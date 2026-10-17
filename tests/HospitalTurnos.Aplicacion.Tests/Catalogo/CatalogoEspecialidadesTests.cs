using System.Text.Json;
using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Catalogo;
using HospitalTurnos.Aplicacion.Tests.Ayudas;
using HospitalTurnos.Dominio.Auditoria;
using HospitalTurnos.Dominio.Catalogo;
using HospitalTurnos.Dominio.Excepciones;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Infraestructura;
using HospitalTurnos.Infraestructura.Identidad;
using HospitalTurnos.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace HospitalTurnos.Aplicacion.Tests.Catalogo;

// Contra la base real: el índice único, la transacción y la auditoría viven ahí. La base es compartida y
// las especialidades no se borran: cada prueba usa su propia marca en los nombres y filtra por ella.
[Collection("Postgres")]
public class CatalogoEspecialidadesTests(PostgresFixture fixture)
{
    private const string SinPermiso = "Sólo el administrador puede modificar las especialidades.";
    private const string YaNoExiste = "La especialidad ya no existe. Actualizá la lista.";

    private ServiceProvider Proveedor(Action<IServiceCollection>? reemplazos = null)
    {
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:App"] = fixture.CadenaApp,
            ["ConnectionStrings:Migraciones"] = fixture.CadenaDuenio,
        }).Build();
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AgregarInfraestructura(configuracion);
        servicios.AddIdentidadPersonal(configuracion);
        reemplazos?.Invoke(servicios);   // lo último registrado gana: así entran los dobles
        return servicios.BuildServiceProvider();
    }

    private static string Marca() => Guid.NewGuid().ToString("N")[..8];

    // Un alcance por operación, como un pedido web.
    private static async Task<T> ConCatalogoAsync<T>(ServiceProvider proveedor, Func<ICatalogoEspecialidades, Task<T>> operacion)
    {
        using var alcance = proveedor.CreateScope();
        return await operacion(alcance.ServiceProvider.GetRequiredService<ICatalogoEspecialidades>());
    }

    private static async Task ConCatalogoAsync(ServiceProvider proveedor, Func<ICatalogoEspecialidades, Task> operacion)
    {
        using var alcance = proveedor.CreateScope();
        await operacion(alcance.ServiceProvider.GetRequiredService<ICatalogoEspecialidades>());
    }

    private static async Task<(UsuarioPersonal Admin, ActorAuditoria Actor)> AdministradorAsync(ServiceProvider proveedor,
        bool activo = true)
    {
        var admin = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Administrador], activo, $"Admin {Marca()}");
        return (admin, UsuariosDePrueba.ActorDe(admin, Roles.Administrador));
    }

    private static Task<long> CrearAsync(ServiceProvider proveedor, ActorAuditoria actor, string nombre) =>
        ConCatalogoAsync(proveedor, c => c.CrearAsync(actor, nombre, CancellationToken.None));

    private static Task<EspecialidadResumen?> BuscarAsync(ServiceProvider proveedor, long id) =>
        ConCatalogoAsync(proveedor, c => c.BuscarAsync(id, CancellationToken.None));

    private sealed record Fila(string? UsuarioId, string Quien, string Rol, string Canal, string Accion,
        string EntidadTipo, string EntidadId, string? Detalle);

    private async Task<List<Fila>> FilasDeAsync(long id)
    {
        await using var conexion = new NpgsqlConnection(fixture.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(
            """
            SELECT usuario_id, quien, rol, canal, accion, entidad_tipo, entidad_id, detalle::text
            FROM registros_auditoria WHERE entidad_tipo = 'Especialidad' AND entidad_id = @id ORDER BY id
            """, conexion);
        comando.Parameters.AddWithValue("id", id.ToString());
        var filas = new List<Fila>();
        await using var lector = await comando.ExecuteReaderAsync();
        while (await lector.ReadAsync())
            filas.Add(new Fila(lector.IsDBNull(0) ? null : lector.GetString(0), lector.GetString(1),
                lector.GetString(2), lector.GetString(3), lector.GetString(4), lector.GetString(5),
                lector.GetString(6), lector.IsDBNull(7) ? null : lector.GetString(7)));
        return filas;
    }

    private async Task<long> ContarAsync(string sql, params (string Nombre, object Valor)[] parametros)
    {
        await using var conexion = new NpgsqlConnection(fixture.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        foreach (var (nombre, valor) in parametros)
            comando.Parameters.AddWithValue(nombre, valor);
        return (long)(await comando.ExecuteScalarAsync())!;
    }

    private Task<long> FilasConClaveAsync(string nombre) =>
        ContarAsync("SELECT count(*) FROM especialidades WHERE nombre_normalizado = @c",
            ("c", Especialidad.Normalizar(nombre)));

    private Task<long> AltasAuditadasConAsync(string marca) =>
        ContarAsync("SELECT count(*) FROM registros_auditoria WHERE accion = @a AND detalle::text LIKE @m",
            ("a", AccionesAuditoria.EspecialidadCreada), ("m", $"%{marca}%"));

    private Task<long> AuditadasPorAsync(string? usuarioId) =>
        ContarAsync("SELECT count(*) FROM registros_auditoria WHERE usuario_id = @u", ("u", usuarioId ?? ""));

    private static string? NombreDelDetalle(string? detalle) =>
        JsonDocument.Parse(detalle!).RootElement.GetProperty("nombre").GetString();

    private static void ActorAuditado(Fila fila, UsuarioPersonal admin, string accion, string nombre)
    {
        Assert.Equal(accion, fila.Accion);
        Assert.Equal(admin.Id.ToString(), fila.UsuarioId);
        Assert.Equal(admin.NombreCompleto, fila.Quien);
        Assert.Equal(Roles.Administrador, fila.Rol);
        Assert.Equal(nameof(CanalAuditoria.Personal), fila.Canal);
        Assert.Equal("Especialidad", fila.EntidadTipo);
        Assert.Equal(nombre, NombreDelDetalle(fila.Detalle));
    }

    [Fact]
    [Trait("Story", "3.1")]
    [Trait("AC", "5")]
    [Trait("AC", "16")]
    public async Task CrearAsync_Administrador_GuardaActivaYRegistraAuditoria()
    {
        await using var proveedor = Proveedor();
        var (admin, actor) = await AdministradorAsync(proveedor);
        var nombre = $"Esp {Marca()}";

        var id = await CrearAsync(proveedor, actor, nombre);

        Assert.Equal(1, await ContarAsync(
            "SELECT count(*) FROM especialidades WHERE id = @id AND activa AND nombre_normalizado = @c",
            ("id", id), ("c", Especialidad.Normalizar(nombre))));
        Assert.Equal(new EspecialidadResumen(id, nombre, true), await BuscarAsync(proveedor, id));
        var fila = Assert.Single(await FilasDeAsync(id));
        Assert.Equal(id.ToString(), fila.EntidadId);
        ActorAuditado(fila, admin, AccionesAuditoria.EspecialidadCreada, nombre);
    }

    [Theory]
    [Trait("Story", "3.1")]
    [Trait("AC", "6")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CrearAsync_NombreRepetido_LanzaConflictoSinGuardarNiAuditar(bool existenteActiva)
    {
        await using var proveedor = Proveedor();
        var (_, actor) = await AdministradorAsync(proveedor);
        var m = Marca();
        var guardado = $"Neumonología {m}";
        var id = await CrearAsync(proveedor, actor, guardado);
        if (!existenteActiva)
            await ConCatalogoAsync(proveedor, c => c.DesactivarAsync(actor, id, CancellationToken.None));
        var tipeado = existenteActiva ? $"neumonologia {m}" : $"NEUMONOLOGÍA {m}";

        var error = await Assert.ThrowsAsync<ConflictoException>(() => CrearAsync(proveedor, actor, tipeado));

        Assert.Equal(existenteActiva
            ? $"Ya existe la especialidad {guardado}."
            : $"Ya existe la especialidad {guardado}, desactivada. Reactivala desde la lista.", error.Message);
        Assert.Equal(1, await FilasConClaveAsync(guardado));
        Assert.Equal(1, await AltasAuditadasConAsync(m));   // sólo el alta original
    }

    [Fact]
    [Trait("Story", "3.1")]
    [Trait("AC", "5")]
    [Trait("AC", "7")]
    public async Task CrearAsync_OtraAltaGanaLaCarrera_TraduceElIndiceUnicoAConflicto()
    {
        var m = Marca();
        var nombre = $"Carrera {m}";
        var interceptor = new InterceptorQueSeAdelanta(fixture.CadenaApp,
            $"INSERT INTO especialidades (nombre, nombre_normalizado, activa) VALUES ('{nombre}', '{Especialidad.Normalizar(nombre)}', true)");
        await using var proveedor = Proveedor(s =>
            s.ConfigureDbContext<HospitalDbContext>(o => o.AddInterceptors(interceptor)));
        var (_, actor) = await AdministradorAsync(proveedor);

        var error = await Assert.ThrowsAsync<ConflictoException>(() => CrearAsync(proveedor, actor, $"carrera {m}"));

        Assert.True(interceptor.SeAdelanto, "La otra alta no llegó a insertar: la prueba no demostraría nada.");
        Assert.Equal($"Ya existe la especialidad {nombre}.", error.Message);
        Assert.Equal(1, await FilasConClaveAsync(nombre));
        Assert.Equal(0, await AltasAuditadasConAsync(m));   // la ganadora entró por SQL, sin auditar
    }

    [Fact]
    [Trait("Story", "3.1")]
    [Trait("AC", "9")]
    [Trait("AC", "10")]
    [Trait("AC", "16")]
    public async Task DesactivarAsync_Activa_LaDesactivaAuditaYSaleDeLasActivas()
    {
        await using var proveedor = Proveedor();
        var (admin, actor) = await AdministradorAsync(proveedor);
        var nombre = $"Desactivar {Marca()}";
        var id = await CrearAsync(proveedor, actor, nombre);

        await ConCatalogoAsync(proveedor, c => c.DesactivarAsync(actor, id, CancellationToken.None));

        Assert.Equal(new EspecialidadResumen(id, nombre, false), await BuscarAsync(proveedor, id));
        var activas = await ConCatalogoAsync(proveedor, c => c.ListarActivasAsync(CancellationToken.None));
        Assert.DoesNotContain(activas, e => e.Id == id);
        var filas = await FilasDeAsync(id);
        Assert.Equal(2, filas.Count);
        ActorAuditado(filas[1], admin, AccionesAuditoria.EspecialidadDesactivada, nombre);
    }

    [Fact]
    [Trait("Story", "3.1")]
    [Trait("AC", "11")]
    [Trait("AC", "16")]
    public async Task ReactivarAsync_Desactivada_LaReactivaAuditaYVuelveALasActivas()
    {
        await using var proveedor = Proveedor();
        var (admin, actor) = await AdministradorAsync(proveedor);
        var nombre = $"Reactivar {Marca()}";
        var id = await CrearAsync(proveedor, actor, nombre);
        await ConCatalogoAsync(proveedor, c => c.DesactivarAsync(actor, id, CancellationToken.None));

        await ConCatalogoAsync(proveedor, c => c.ReactivarAsync(actor, id, CancellationToken.None));

        Assert.Equal(new EspecialidadResumen(id, nombre, true), await BuscarAsync(proveedor, id));
        var activas = await ConCatalogoAsync(proveedor, c => c.ListarActivasAsync(CancellationToken.None));
        Assert.Contains(activas, e => e.Id == id);
        var filas = await FilasDeAsync(id);
        Assert.Equal(3, filas.Count);
        ActorAuditado(filas[2], admin, AccionesAuditoria.EspecialidadReactivada, nombre);
    }

    [Theory]
    [Trait("Story", "3.1")]
    [Trait("AC", "3")]
    [Trait("AC", "4")]
    [Trait("AC", "12")]
    [InlineData("desactivar-inexistente")]
    [InlineData("reactivar-inexistente")]
    [InlineData("desactivar-desactivada")]
    [InlineData("reactivar-activa")]
    public async Task CambiarEstadoAsync_IdInexistenteOEstadoRepetido_LanzaSinAuditar(string caso)
    {
        await using var proveedor = Proveedor();
        var (_, actor) = await AdministradorAsync(proveedor);
        var nombre = $"Estado {Marca()}";
        var id = await CrearAsync(proveedor, actor, nombre);
        if (caso == "desactivar-desactivada")
            await ConCatalogoAsync(proveedor, c => c.DesactivarAsync(actor, id, CancellationToken.None));
        var antes = await FilasDeAsync(id);
        var estadoAntes = (await BuscarAsync(proveedor, id))!.Activa;

        Task Accion(ICatalogoEspecialidades c) => caso switch
        {
            "desactivar-inexistente" => c.DesactivarAsync(actor, long.MaxValue, CancellationToken.None),
            "reactivar-inexistente" => c.ReactivarAsync(actor, long.MaxValue, CancellationToken.None),
            "desactivar-desactivada" => c.DesactivarAsync(actor, id, CancellationToken.None),
            _ => c.ReactivarAsync(actor, id, CancellationToken.None),
        };

        if (caso.EndsWith("inexistente"))
        {
            var error = await Assert.ThrowsAsync<NoEncontradoException>(() => ConCatalogoAsync(proveedor, Accion));
            Assert.Equal(YaNoExiste, error.Message);
            Assert.Equal(0, await ContarAsync(
                "SELECT count(*) FROM registros_auditoria WHERE entidad_tipo = 'Especialidad' AND entidad_id = @id",
                ("id", long.MaxValue.ToString())));
        }
        else
        {
            var error = await Assert.ThrowsAsync<ConflictoException>(() => ConCatalogoAsync(proveedor, Accion));
            Assert.Equal(caso == "desactivar-desactivada"
                ? $"La especialidad {nombre} ya está desactivada."
                : $"La especialidad {nombre} ya está activa.", error.Message);
            Assert.Equal(antes.Count, (await FilasDeAsync(id)).Count);
            Assert.Equal(estadoAntes, (await BuscarAsync(proveedor, id))!.Activa);
        }
    }

    [Theory]
    [Trait("Story", "3.1")]
    [Trait("AC", "13")]
    [InlineData("recepcionista", "crear")]
    [InlineData("recepcionista", "desactivar")]
    [InlineData("recepcionista", "reactivar")]
    [InlineData("medico", "crear")]
    [InlineData("medico", "desactivar")]
    [InlineData("medico", "reactivar")]
    [InlineData("admin-desactivado", "crear")]
    [InlineData("admin-desactivado", "desactivar")]
    [InlineData("admin-desactivado", "reactivar")]
    [InlineData("inexistente", "crear")]
    [InlineData("inexistente", "desactivar")]
    [InlineData("inexistente", "reactivar")]
    public async Task Escrituras_ActorQueNoEsAdministradorActivo_LanzaAccesoDenegadoSinCambios(string quien, string operacion)
    {
        await using var proveedor = Proveedor();
        var (_, admin) = await AdministradorAsync(proveedor);
        var m = Marca();
        var activa = await CrearAsync(proveedor, admin, $"Activa {m}");
        var desactivada = await CrearAsync(proveedor, admin, $"Desactivada {m}");
        await ConCatalogoAsync(proveedor, c => c.DesactivarAsync(admin, desactivada, CancellationToken.None));
        var actor = quien switch
        {
            "recepcionista" => UsuariosDePrueba.ActorDe(
                await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Recepcionista]), Roles.Recepcionista),
            "medico" => UsuariosDePrueba.ActorDe(await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Medico]), Roles.Medico),
            // Su sesión todavía dice Administrador, como una cookie atrasada: la base dice que no.
            "admin-desactivado" => (await AdministradorAsync(proveedor, activo: false)).Actor,
            _ => new ActorAuditoria(Guid.NewGuid().ToString(), "Nadie", Roles.Administrador, CanalAuditoria.Personal),
        };
        var nueva = $"Nueva {m}";

        var error = await Assert.ThrowsAsync<AccesoDenegadoException>(() => ConCatalogoAsync(proveedor, c => operacion switch
        {
            "crear" => (Task)c.CrearAsync(actor, nueva, CancellationToken.None),
            "desactivar" => c.DesactivarAsync(actor, activa, CancellationToken.None),
            _ => c.ReactivarAsync(actor, desactivada, CancellationToken.None),
        }));

        Assert.Equal(SinPermiso, error.Message);
        Assert.Equal(0, await FilasConClaveAsync(nueva));
        Assert.True((await BuscarAsync(proveedor, activa))!.Activa);
        Assert.False((await BuscarAsync(proveedor, desactivada))!.Activa);
        Assert.Equal(0, await AuditadasPorAsync(actor.UsuarioId));
        Assert.Equal(0, await AltasAuditadasConAsync($"Nueva {m}"));
    }

    [Fact]
    [Trait("Story", "3.1")]
    [Trait("AC", "16")]
    [Trait("AC", "18")]
    public async Task ListarActivasAsync_ConActivasYDesactivadas_DevuelveSoloActivasOrdenadas()
    {
        await using var proveedor = Proveedor();
        var (_, actor) = await AdministradorAsync(proveedor);
        var m = Marca();
        await CrearAsync(proveedor, actor, $"{m} Ortopedia");
        await CrearAsync(proveedor, actor, $"{m} Clínica");
        var alergia = await CrearAsync(proveedor, actor, $"{m} Alergia");
        await ConCatalogoAsync(proveedor, c => c.DesactivarAsync(actor, alergia, CancellationToken.None));

        var activas = (await ConCatalogoAsync(proveedor, c => c.ListarActivasAsync(CancellationToken.None)))
            .Where(e => e.Nombre.StartsWith(m, StringComparison.Ordinal))
            .ToList();

        Assert.Equal([$"{m} Clínica", $"{m} Ortopedia"], activas.Select(e => e.Nombre));
        Assert.All(activas, e => Assert.True(e.Activa));
    }

    [Fact]
    [Trait("Story", "3.1")]
    [Trait("AC", "9")]
    [Trait("AC", "19")]
    public async Task PermisosDeLaBase_TurnosApp_NoPuedeBorrarEspecialidades()
    {
        await using var conexion = new NpgsqlConnection(fixture.CadenaDuenio);
        await conexion.OpenAsync();

        async Task<bool> PuedeAsync(string tabla, string privilegio)
        {
            await using var comando = new NpgsqlCommand($"SELECT has_table_privilege('turnos_app', '{tabla}', '{privilegio}')", conexion);
            return (bool)(await comando.ExecuteScalarAsync())!;
        }

        Assert.True(await PuedeAsync("especialidades", "SELECT"));
        Assert.True(await PuedeAsync("especialidades", "INSERT"));
        Assert.True(await PuedeAsync("especialidades", "UPDATE"));
        Assert.False(await PuedeAsync("especialidades", "DELETE"));
        Assert.False(await PuedeAsync("registros_auditoria", "UPDATE"));
    }
}
