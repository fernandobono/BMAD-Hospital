using System.Text.Json;
using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Catalogo;
using HospitalTurnos.Aplicacion.Tests.Ayudas;
using HospitalTurnos.Dominio.Agendas;
using HospitalTurnos.Dominio.Auditoria;
using HospitalTurnos.Dominio.Excepciones;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Infraestructura;
using HospitalTurnos.Infraestructura.Identidad;
using HospitalTurnos.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Npgsql;

namespace HospitalTurnos.Aplicacion.Tests.Catalogo;

// Contra la base real: los índices únicos, la transacción y la auditoría viven ahí. La base es compartida y
// los médicos no se borran: cada prueba usa su propia marca (apellido Prueba<marca>, matrícula T-<MARCA>) y
// filtra por ella o por el id propio. GestionMedicosEnBase SIEMPRE sale del contenedor (la 4.1 le agrega
// un parámetro sin tocar estas pruebas).
[Collection("Postgres")]
public class GestionMedicosEnBaseTests(PostgresFixture fixture)
{
    private const string SinPermiso = "Sólo un administrador puede dar de alta o modificar médicos.";
    private const string EspecialidadInvalida = "La especialidad elegida no existe o está desactivada. Elegí otra.";
    private const string UsuarioNoVinculable =
        "El usuario elegido no se puede vincular: tiene que estar activo y tener el rol Médico.";
    private const string VinculadoAOtro = "El usuario elegido ya está vinculado a otro médico.";
    private const string Inexistente = "No existe el médico pedido.";

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
    private static async Task<T> ConGestionAsync<T>(ServiceProvider proveedor, Func<IGestionMedicos, Task<T>> operacion)
    {
        using var alcance = proveedor.CreateScope();
        return await operacion(alcance.ServiceProvider.GetRequiredService<IGestionMedicos>());
    }

    private static Task<long> CrearAsync(ServiceProvider proveedor, ActorAuditoria actor, DatosMedico datos) =>
        ConGestionAsync(proveedor, g => g.CrearAsync(datos, actor, CancellationToken.None));

    private static Task<bool> EditarAsync(ServiceProvider proveedor, ActorAuditoria actor, long id, DatosMedico datos) =>
        ConGestionAsync(proveedor, g => g.EditarAsync(id, datos, actor, CancellationToken.None));

    private static Task<DatosFichaMedico?> ObtenerAsync(ServiceProvider proveedor, long id) =>
        ConGestionAsync(proveedor, g => g.ObtenerAsync(id, CancellationToken.None));

    private static async Task<(UsuarioPersonal Admin, ActorAuditoria Actor)> AdministradorAsync(ServiceProvider proveedor,
        bool activo = true)
    {
        var admin = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Administrador], activo, $"Admin {Marca()}");
        return (admin, UsuariosDePrueba.ActorDe(admin, Roles.Administrador));
    }

    private static async Task DesactivarUsuarioAsync(ServiceProvider proveedor, Guid id)
    {
        using var alcance = proveedor.CreateScope();
        var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>();
        var usuario = (await usuarios.FindByIdAsync(id.ToString()))!;
        usuario.Activo = false;
        Assert.True((await usuarios.UpdateAsync(usuario)).Succeeded);
    }

    private sealed record Fila(string? UsuarioId, string Quien, string Rol, string Canal, string Accion,
        string EntidadTipo, string EntidadId, DateTimeOffset Cuando, string? Detalle);

    private async Task<List<Fila>> FilasDeAsync(long id)
    {
        await using var conexion = new NpgsqlConnection(fixture.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(
            """
            SELECT usuario_id, quien, rol, canal, accion, entidad_tipo, entidad_id, cuando, detalle::text
            FROM registros_auditoria WHERE entidad_tipo = 'Medico' AND entidad_id = @id ORDER BY id
            """, conexion);
        comando.Parameters.AddWithValue("id", id.ToString());
        var filas = new List<Fila>();
        await using var lector = await comando.ExecuteReaderAsync();
        while (await lector.ReadAsync())
            filas.Add(new Fila(lector.IsDBNull(0) ? null : lector.GetString(0), lector.GetString(1),
                lector.GetString(2), lector.GetString(3), lector.GetString(4), lector.GetString(5),
                lector.GetString(6), lector.GetFieldValue<DateTimeOffset>(7), lector.IsDBNull(8) ? null : lector.GetString(8)));
        return filas;
    }

    private async Task<long> ContarAsync(string sql, params (string Nombre, object Valor)[] parametros)
    {
        await using var conexion = new NpgsqlConnection(fixture.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        foreach (var (nombre, valor) in parametros)
            comando.Parameters.AddWithValue(nombre, valor);
        return Convert.ToInt64(await comando.ExecuteScalarAsync());
    }

    private Task<long> MedicosConMatriculaAsync(string matricula) =>
        ContarAsync("SELECT count(*) FROM medicos WHERE matricula = @m", ("m", matricula));

    // Las altas auditadas cuya matrícula lleva la marca (el detalle es jsonb).
    private Task<long> AltasAuditadasConAsync(string marca) =>
        ContarAsync("SELECT count(*) FROM registros_auditoria WHERE accion = 'medico.creado' AND detalle->>'matricula' LIKE @p",
            ("p", $"%{marca.ToUpperInvariant()}%"));

    private static DatosMedico Datos(string m, long especialidad, Guid? usuario = null, string nombre = "Ana",
        string? matricula = null) =>
        new(nombre, $"Prueba{m}", matricula ?? $"T-{m.ToUpperInvariant()}", especialidad, usuario);

    [Fact]
    [Trait("Story", "3.2")]
    [Trait("AC", "6")]
    [Trait("AC", "22")]
    public async Task CrearAsync_DatosValidos_GuardaElMedicoYAuditaElAlta()
    {
        var instante = new DateTimeOffset(2026, 11, 2, 13, 0, 0, TimeSpan.Zero);
        var reloj = new FakeTimeProvider(instante);
        reloj.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Cordoba"));
        await using var proveedor = Proveedor(s => s.AddSingleton<TimeProvider>(reloj));
        var (admin, actor) = await AdministradorAsync(proveedor);
        var especialidad = await proveedor.CrearEspecialidadAsync(actor);
        var usuario = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Medico]);
        var m = Marca();

        var id = await CrearAsync(proveedor, actor, Datos(m, especialidad, usuario.Id, matricula: $" t-{m}  x "));

        Assert.True(id > 0);
        Assert.Equal(1, await ContarAsync(
            "SELECT count(*) FROM medicos WHERE id = @id AND matricula = @mat AND usuario_personal_id = @u",
            ("id", id), ("mat", $"T-{m.ToUpperInvariant()} X"), ("u", usuario.Id)));
        var fila = Assert.Single(await FilasDeAsync(id));
        Assert.Equal(AccionesAuditoria.MedicoCreado, fila.Accion);
        Assert.Equal("Medico", fila.EntidadTipo);
        Assert.Equal(id.ToString(), fila.EntidadId);
        Assert.Equal(nameof(CanalAuditoria.Personal), fila.Canal);
        Assert.Equal(Roles.Administrador, fila.Rol);
        Assert.Equal(admin.Id.ToString(), fila.UsuarioId);
        Assert.Equal(admin.NombreCompleto, fila.Quien);
        Assert.Equal(instante, fila.Cuando);
        using var detalle = JsonDocument.Parse(fila.Detalle!);
        var raiz = detalle.RootElement;
        Assert.Equal(["nombre", "apellido", "matricula", "especialidadId", "usuarioPersonalId"],
            raiz.EnumerateObject().Select(p => p.Name));
        Assert.Equal("Ana", raiz.GetProperty("nombre").GetString());
        Assert.Equal($"Prueba{m}", raiz.GetProperty("apellido").GetString());
        Assert.Equal($"T-{m.ToUpperInvariant()} X", raiz.GetProperty("matricula").GetString());
        Assert.Equal(especialidad, raiz.GetProperty("especialidadId").GetInt64());
        Assert.Equal(usuario.Id, raiz.GetProperty("usuarioPersonalId").GetGuid());
    }

    [Fact]
    [Trait("Story", "3.2")]
    [Trait("AC", "8")]
    public async Task CrearAsync_MatriculaRepetidaConOtroFormato_LanzaConflictoSinGuardar()
    {
        await using var proveedor = Proveedor();
        var (_, actor) = await AdministradorAsync(proveedor);
        var especialidad = await proveedor.CrearEspecialidadAsync(actor);
        var m = Marca();
        var matricula = $"T-{m.ToUpperInvariant()}";
        await proveedor.CrearMedicoAsync("Beto", $"Prueba{m}", matricula, especialidad);

        var error = await Assert.ThrowsAsync<ConflictoException>(() =>
            CrearAsync(proveedor, actor, Datos(m, especialidad, matricula: $" t-{m} ")));

        Assert.Equal($"Ya hay un médico con la matrícula {matricula}.", error.Message);
        Assert.Equal(1, await MedicosConMatriculaAsync(matricula));
        Assert.Equal(0, await AltasAuditadasConAsync(m));
    }

    [Theory]
    [Trait("Story", "3.2")]
    [Trait("AC", "8")]
    [Trait("AC", "12")]
    [InlineData("matricula")]
    [InlineData("usuario")]
    public async Task GuardarAsync_OtroPedidoGanaLaCarrera_TraduceElIndiceUnicoAConflicto(string carrera)
    {
        var m = Marca();
        var matricula = $"T-{m.ToUpperInvariant()}";
        // Los datos se arman con un proveedor sin doble; el doble va sólo en el del alta que pierde.
        await using var preparacion = Proveedor();
        var (_, actor) = await AdministradorAsync(preparacion);
        var especialidad = await preparacion.CrearEspecialidadAsync(actor);
        var usuario = await UsuariosDePrueba.CrearAsync(preparacion, [Roles.Medico]);
        var (matriculaGanadora, mensaje) = carrera == "matricula"
            ? (matricula, $"Ya hay un médico con la matrícula {matricula}.")
            : ($"{matricula}-G", VinculadoAOtro);
        var interceptor = new InterceptorQueSeAdelanta(fixture.CadenaApp,
            $"""
            INSERT INTO medicos (nombre, apellido, matricula, especialidad_id, usuario_personal_id)
            VALUES ('Ganador', 'Prueba{m}', '{matriculaGanadora}', {especialidad}, '{usuario.Id}')
            """);
        await using var proveedor = Proveedor(s =>
            s.ConfigureDbContext<HospitalDbContext>(o => o.AddInterceptors(interceptor)));

        var error = await Assert.ThrowsAsync<ConflictoException>(() =>
            CrearAsync(proveedor, actor, Datos(m, especialidad, usuario.Id)));

        Assert.True(interceptor.SeAdelanto, "El otro pedido no llegó a insertar: la prueba no demostraría nada.");
        Assert.Equal(mensaje, error.Message);
        Assert.Equal(1, await ContarAsync("SELECT count(*) FROM medicos WHERE apellido = @a", ("a", $"Prueba{m}")));
        Assert.Equal(0, await AltasAuditadasConAsync(m));   // el ganador entró por SQL, sin auditar
    }

    [Theory]
    [Trait("Story", "3.2")]
    [Trait("AC", "9")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CrearAsync_EspecialidadInexistenteOInactiva_LanzaReglaDeNegocio(bool existeDesactivada)
    {
        await using var proveedor = Proveedor();
        var (_, actor) = await AdministradorAsync(proveedor);
        var especialidad = existeDesactivada
            ? await proveedor.CrearEspecialidadAsync(actor, activa: false)
            : long.MaxValue;
        var m = Marca();

        // Assert.ThrowsAsync exige el tipo exacto: una ConflictoException (que hereda) no pasaría.
        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
            CrearAsync(proveedor, actor, Datos(m, especialidad)));

        Assert.Equal(EspecialidadInvalida, error.Message);
        Assert.Equal(0, await MedicosConMatriculaAsync($"T-{m.ToUpperInvariant()}"));
    }

    [Theory]
    [Trait("Story", "3.2")]
    [Trait("AC", "11")]
    [InlineData("inexistente")]
    [InlineData("recepcionista")]
    [InlineData("medico-desactivado")]
    [InlineData("medico-y-recepcionista")]
    public async Task CrearAsync_UsuarioNoVinculable_LanzaReglaDeNegocio(string caso)
    {
        await using var proveedor = Proveedor();
        var (_, actor) = await AdministradorAsync(proveedor);
        var especialidad = await proveedor.CrearEspecialidadAsync(actor);
        var usuario = caso switch
        {
            "recepcionista" => (await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Recepcionista])).Id,
            "medico-desactivado" => (await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Medico], activo: false)).Id,
            "medico-y-recepcionista" => (await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Medico, Roles.Recepcionista])).Id,
            _ => Guid.NewGuid(),
        };
        var m = Marca();

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
            CrearAsync(proveedor, actor, Datos(m, especialidad, usuario)));

        Assert.Equal(UsuarioNoVinculable, error.Message);
        Assert.Equal(0, await MedicosConMatriculaAsync($"T-{m.ToUpperInvariant()}"));
    }

    [Fact]
    [Trait("Story", "3.2")]
    [Trait("AC", "12")]
    public async Task CrearAsync_UsuarioYaVinculado_LanzaConflictoNombrandoAlMedico()
    {
        await using var proveedor = Proveedor();
        var (_, actor) = await AdministradorAsync(proveedor);
        var especialidad = await proveedor.CrearEspecialidadAsync(actor);
        var usuario = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Medico]);
        var m = Marca();
        await proveedor.CrearMedicoAsync("Ana", $"Prueba{m}", $"T-{m.ToUpperInvariant()}-1", especialidad, usuario.Id);

        var error = await Assert.ThrowsAsync<ConflictoException>(() =>
            CrearAsync(proveedor, actor, Datos(m, especialidad, usuario.Id, "Beto", $"T-{m}-2")));

        Assert.Equal($"El usuario elegido ya está vinculado al médico Prueba{m}, Ana.", error.Message);
        Assert.Equal(0, await MedicosConMatriculaAsync($"T-{m.ToUpperInvariant()}-2"));
    }

    [Fact]
    [Trait("Story", "3.2")]
    [Trait("AC", "16")]
    [Trait("AC", "22")]
    public async Task EditarAsync_CambiaDatos_GuardaYAuditaSoloLosCambios()
    {
        await using var proveedor = Proveedor();
        var (admin, actor) = await AdministradorAsync(proveedor);
        var especialidad = await proveedor.CrearEspecialidadAsync(actor);
        var otra = await proveedor.CrearEspecialidadAsync(actor);
        var m = Marca();
        var id = await proveedor.CrearMedicoAsync("Ana", $"Prueba{m}", $"T-{m}", especialidad);

        var guardo = await EditarAsync(proveedor, actor, id, new DatosMedico("Ana", $"Otra{m}", $"T-{m}", otra, null));

        Assert.True(guardo);
        var ficha = (await ObtenerAsync(proveedor, id))!;
        Assert.Equal($"Otra{m}", ficha.Datos.Apellido);
        Assert.Equal(otra, ficha.Datos.EspecialidadId);
        var fila = Assert.Single(await FilasDeAsync(id));
        Assert.Equal(AccionesAuditoria.MedicoEditado, fila.Accion);
        Assert.Equal(admin.Id.ToString(), fila.UsuarioId);
        using var detalle = JsonDocument.Parse(fila.Detalle!);
        var cambios = detalle.RootElement.GetProperty("cambios");
        Assert.Equal(["apellido", "especialidadId"], cambios.EnumerateObject().Select(p => p.Name).Order());
        Assert.Equal($"Prueba{m}", cambios.GetProperty("apellido").GetProperty("antes").GetString());
        Assert.Equal($"Otra{m}", cambios.GetProperty("apellido").GetProperty("despues").GetString());
        Assert.Equal(especialidad.ToString(), cambios.GetProperty("especialidadId").GetProperty("antes").GetString());
        Assert.Equal(otra.ToString(), cambios.GetProperty("especialidadId").GetProperty("despues").GetString());
    }

    [Fact]
    [Trait("Story", "3.2")]
    [Trait("AC", "16")]
    public async Task EditarAsync_SinCambios_NoGuardaNiAudita()
    {
        await using var proveedor = Proveedor();
        var (_, actor) = await AdministradorAsync(proveedor);
        var especialidad = await proveedor.CrearEspecialidadAsync(actor);
        var m = Marca();
        var id = await proveedor.CrearMedicoAsync("Ana", $"Prueba{m}", $"T-{m}", especialidad);

        var guardo = await EditarAsync(proveedor, actor, id, Datos(m, especialidad, matricula: $"t-{m}"));

        Assert.False(guardo);
        Assert.Empty(await FilasDeAsync(id));
    }

    [Fact]
    [Trait("Story", "3.2")]
    [Trait("AC", "17")]
    public async Task EditarAsync_LoQueNoCambia_NoSeRevalida()
    {
        await using var proveedor = Proveedor();
        var (_, actor) = await AdministradorAsync(proveedor);
        var especialidad = await proveedor.CrearEspecialidadAsync(actor);
        var usuario = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Medico]);
        var m = Marca();
        var id = await proveedor.CrearMedicoAsync("Ana", $"Prueba{m}", $"T-{m}", especialidad, usuario.Id);
        using (var alcance = proveedor.CreateScope())
            await alcance.ServiceProvider.GetRequiredService<ICatalogoEspecialidades>()
                .DesactivarAsync(actor, especialidad, CancellationToken.None);
        await DesactivarUsuarioAsync(proveedor, usuario.Id);

        var guardo = await EditarAsync(proveedor, actor, id, Datos(m, especialidad, usuario.Id, "Anabel"));

        Assert.True(guardo);
        var ficha = (await ObtenerAsync(proveedor, id))!;
        Assert.Equal("Anabel", ficha.Datos.Nombre);
        Assert.Equal(especialidad, ficha.Datos.EspecialidadId);
        Assert.Equal(usuario.Id, ficha.Datos.UsuarioPersonalId);
    }

    [Fact]
    [Trait("Story", "3.2")]
    [Trait("AC", "13")]
    public async Task EditarAsync_Desvincular_LiberaAlUsuarioParaOtroMedico()
    {
        await using var proveedor = Proveedor();
        var (_, actor) = await AdministradorAsync(proveedor);
        var especialidad = await proveedor.CrearEspecialidadAsync(actor);
        var usuario = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Medico]);
        var m = Marca();
        var a = await proveedor.CrearMedicoAsync("Ana", $"Prueba{m}", $"T-{m}-1", especialidad, usuario.Id);

        Assert.True(await EditarAsync(proveedor, actor, a, Datos(m, especialidad, null, matricula: $"T-{m}-1")));
        var b = await CrearAsync(proveedor, actor, Datos(m, especialidad, usuario.Id, "Beto", $"T-{m}-2"));

        Assert.Null((await ObtenerAsync(proveedor, a))!.Datos.UsuarioPersonalId);
        Assert.Equal(usuario.Id, (await ObtenerAsync(proveedor, b))!.Datos.UsuarioPersonalId);
        var paraB = await ConGestionAsync(proveedor, g => g.UsuariosParaVincularAsync(b, CancellationToken.None));
        var paraNuevo = await ConGestionAsync(proveedor, g => g.UsuariosParaVincularAsync(null, CancellationToken.None));
        Assert.Contains(paraB, o => o.Id == usuario.Id);
        Assert.DoesNotContain(paraNuevo, o => o.Id == usuario.Id);
    }

    [Fact]
    [Trait("Story", "3.2")]
    [Trait("AC", "18")]
    public async Task EditarAsync_MedicoInexistente_LanzaNoEncontrado()
    {
        await using var proveedor = Proveedor();
        var (_, actor) = await AdministradorAsync(proveedor);
        var especialidad = await proveedor.CrearEspecialidadAsync(actor);

        var error = await Assert.ThrowsAsync<NoEncontradoException>(() =>
            EditarAsync(proveedor, actor, long.MaxValue, Datos(Marca(), especialidad)));

        Assert.Equal(Inexistente, error.Message);
        Assert.Empty(await FilasDeAsync(long.MaxValue));
    }

    [Theory]
    [Trait("Story", "3.2")]
    [Trait("AC", "20")]
    [InlineData("recepcionista", "crear")]
    [InlineData("recepcionista", "editar")]
    [InlineData("medico", "crear")]
    [InlineData("medico", "editar")]
    [InlineData("admin-desactivado", "crear")]
    [InlineData("admin-desactivado", "editar")]
    public async Task EscriturasDelCatalogo_ActorQueNoEsAdministradorActivo_LanzaAccesoDenegadoSinGuardar(string quien,
        string operacion)
    {
        await using var proveedor = Proveedor();
        var (_, admin) = await AdministradorAsync(proveedor);
        var especialidad = await proveedor.CrearEspecialidadAsync(admin);
        var m = Marca();
        var existente = await proveedor.CrearMedicoAsync("Ana", $"Prueba{m}", $"T-{m}-1", especialidad);
        var actor = quien switch
        {
            "recepcionista" => UsuariosDePrueba.ActorDe(
                await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Recepcionista]), Roles.Recepcionista),
            "medico" => UsuariosDePrueba.ActorDe(await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Medico]), Roles.Medico),
            // Su sesión todavía dice Administrador, como una cookie atrasada: la base dice que no.
            _ => (await AdministradorAsync(proveedor, activo: false)).Actor,
        };

        var error = await Assert.ThrowsAsync<AccesoDenegadoException>(() => operacion == "crear"
            ? CrearAsync(proveedor, actor, Datos(m, especialidad, matricula: $"T-{m}-2"))
            : EditarAsync(proveedor, actor, existente, Datos(m, especialidad, nombre: "Cambiada", matricula: $"T-{m}-1")));

        Assert.Equal(SinPermiso, error.Message);
        Assert.Equal(0, await MedicosConMatriculaAsync($"T-{m.ToUpperInvariant()}-2"));
        Assert.Equal("Ana", (await ObtenerAsync(proveedor, existente))!.Datos.Nombre);
        Assert.Empty(await FilasDeAsync(existente));
        Assert.Equal(0, await AltasAuditadasConAsync(m));
    }

    [Theory]
    [Trait("Story", "3.2")]
    [Trait("AC", "2")]
    // {m} se reemplaza por la marca de la prueba; LARGO son 101 caracteres (más de 100: no filtra, la pantalla avisa).
    [InlineData("prueba{m}", "Ana,Beto")]
    [InlineData("beto", "Beto")]
    [InlineData("t-{m}-2", "Beto")]
    [InlineData("%", "")]
    [InlineData("_", "")]
    [InlineData("   ", "Ana,Beto")]
    [InlineData("LARGO", "Ana,Beto")]
    public async Task ListarAsync_Buscar_FiltraSinTratarComodines(string patron, string nombres)
    {
        await using var proveedor = Proveedor();
        var (_, actor) = await AdministradorAsync(proveedor);
        var especialidad = await proveedor.CrearEspecialidadAsync(actor);
        var m = Marca();
        // Beto antes que Ana: el orden lo pone la consulta, no la carga.
        await proveedor.CrearMedicoAsync("Beto", $"Prueba{m}", $"T-{m}-2", especialidad);
        await proveedor.CrearMedicoAsync("Ana", $"Prueba{m}", $"T-{m}-1", especialidad);
        var buscar = patron == "LARGO" ? new string('x', 101) : patron.Replace("{m}", m);
        var esperados = nombres.Split(',', StringSplitOptions.RemoveEmptyEntries);

        var listado = await ConGestionAsync(proveedor, g => g.ListarAsync(buscar, CancellationToken.None));

        var propios = listado.Where(x => x.Apellido == $"Prueba{m}").ToList();
        Assert.Equal(esperados, propios.Select(x => x.Nombre));
        Assert.All(propios, x => Assert.Equal(SituacionDeAgenda.SinAgenda, x.Agenda));
    }

    [Fact]
    [Trait("Story", "3.2")]
    [Trait("AC", "5")]
    [Trait("AC", "13")]
    public async Task UsuariosParaVincularAsync_SoloMedicosActivosLibresMasElPropio()
    {
        await using var proveedor = Proveedor();
        var (_, actor) = await AdministradorAsync(proveedor);
        var especialidad = await proveedor.CrearEspecialidadAsync(actor);
        var libre = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Medico], nombre: "Libre");
        var desactivado = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Medico], activo: false);
        var recepcionista = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Recepcionista]);
        var vinculado = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Medico]);
        var m = Marca();
        var medico = await proveedor.CrearMedicoAsync("Ana", $"Prueba{m}", $"T-{m}", especialidad, vinculado.Id);
        Guid[] propios = [libre.Id, desactivado.Id, recepcionista.Id, vinculado.Id];

        var paraNuevo = await ConGestionAsync(proveedor, g => g.UsuariosParaVincularAsync(null, CancellationToken.None));
        var paraMedico = await ConGestionAsync(proveedor, g => g.UsuariosParaVincularAsync(medico, CancellationToken.None));

        Assert.Equal([libre.Id], paraNuevo.Where(o => propios.Contains(o.Id)).Select(o => o.Id));
        Assert.Equal(new OpcionUsuarioMedico(libre.Id, libre.UserName!, "Libre"), paraNuevo.Single(o => o.Id == libre.Id));
        Assert.Equal(new[] { libre.Id, vinculado.Id }.Order(),
            paraMedico.Where(o => propios.Contains(o.Id)).Select(o => o.Id).Order());
    }

    [Fact]
    [Trait("Story", "3.2")]
    [Trait("AC", "15")]
    [Trait("AC", "17")]
    [Trait("AC", "23")]
    public async Task ObtenerAsync_EspecialidadDesactivadaDespues_DevuelveLaFichaConLaEspecialidadInactiva()
    {
        await using var proveedor = Proveedor();
        var (_, actor) = await AdministradorAsync(proveedor);
        var m = Marca();
        var especialidad = await proveedor.CrearEspecialidadAsync(actor, nombre: $"Esp{m}");
        var id = await proveedor.CrearMedicoAsync("Ana", $"Prueba{m}", $"T-{m}", especialidad);
        using (var alcance = proveedor.CreateScope())
            await alcance.ServiceProvider.GetRequiredService<ICatalogoEspecialidades>()
                .DesactivarAsync(actor, especialidad, CancellationToken.None);

        var ficha = await ObtenerAsync(proveedor, id);

        Assert.NotNull(ficha);
        Assert.Equal(id, ficha.Id);
        Assert.Equal($"Esp{m}", ficha.Especialidad);
        Assert.False(ficha.EspecialidadActiva);
        Assert.Equal(SituacionDeAgenda.SinAgenda, ficha.Agenda);
        Assert.Equal(new DatosMedico("Ana", $"Prueba{m}", $"T-{m.ToUpperInvariant()}", especialidad, null), ficha.Datos);
        using var alcanceCatalogo = proveedor.CreateScope();
        var activas = await alcanceCatalogo.ServiceProvider.GetRequiredService<ICatalogoEspecialidades>()
            .ListarActivasAsync(CancellationToken.None);
        Assert.DoesNotContain(activas, e => e.Id == especialidad);   // por eso la ficha la agrega marcada (AC 15)
    }

    [Fact]
    [Trait("Story", "3.2")]
    [Trait("AC", "12")]
    [Trait("AC", "24")]
    public async Task MigracionMedicos_PermisosEIndices_ComoLosDeclaraLaStory()
    {
        await using var conexion = new NpgsqlConnection(fixture.CadenaDuenio);
        await conexion.OpenAsync();

        async Task<object?> EscalarAsync(string sql)
        {
            await using var comando = new NpgsqlCommand(sql, conexion);
            return await comando.ExecuteScalarAsync();
        }

        foreach (var indice in new[] { "ix_medicos_matricula", "ix_medicos_usuario_personal_id" })
            Assert.Equal(1L, await EscalarAsync(
                $"SELECT count(*) FROM pg_indexes WHERE tablename = 'medicos' AND indexname = '{indice}' AND indexdef LIKE 'CREATE UNIQUE INDEX%'"));
        foreach (var privilegio in new[] { "SELECT", "INSERT", "UPDATE" })
            Assert.Equal(true, await EscalarAsync($"SELECT has_table_privilege('turnos_app', 'medicos', '{privilegio}')"));
        Assert.Equal(false, await EscalarAsync("SELECT has_table_privilege('turnos_app', 'medicos', 'DELETE')"));
        Assert.Equal(false, await EscalarAsync("SELECT has_table_privilege('turnos_app', 'registros_auditoria', 'UPDATE')"));
    }
}
