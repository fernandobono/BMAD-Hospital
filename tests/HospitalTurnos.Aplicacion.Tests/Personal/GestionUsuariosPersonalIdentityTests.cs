using System.Text.Json;
using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Personal;
using HospitalTurnos.Aplicacion.Tests.Ayudas;
using HospitalTurnos.Aplicacion.Tests.Personal.Dobles;
using HospitalTurnos.Dominio.Auditoria;
using HospitalTurnos.Dominio.Excepciones;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Infraestructura;
using HospitalTurnos.Infraestructura.Identidad;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Npgsql;

namespace HospitalTurnos.Aplicacion.Tests.Personal;

// Contra la base real: Identity, la transacción, el candado y la auditoría viven ahí. La base es
// compartida: cada prueba crea sus propios usuarios y filtra la auditoría por su id o su marca.
[Collection("Postgres")]
public class GestionUsuariosPersonalIdentityTests(PostgresFixture fixture)
{
    private const string SinPermiso = "No tenés permiso para administrar usuarios.";

    // Arranca en la hora real: Identity fija LockoutEnd con el reloj del sistema, no con TimeProvider.
    private readonly FakeTimeProvider reloj = RelojDeCordoba();

    private static FakeTimeProvider RelojDeCordoba()
    {
        var reloj = new FakeTimeProvider(DateTimeOffset.UtcNow);
        reloj.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Cordoba"));
        return reloj;
    }

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
        servicios.AddSingleton<TimeProvider>(reloj);
        reemplazos?.Invoke(servicios);   // lo último registrado gana: así entran los dobles
        return servicios.BuildServiceProvider();
    }

    private static string Marca() => Guid.NewGuid().ToString("N")[..8];

    // Un alcance por operación, como un pedido web.
    private static async Task<T> ConGestionAsync<T>(ServiceProvider proveedor, Func<IGestionUsuariosPersonal, Task<T>> operacion)
    {
        using var alcance = proveedor.CreateScope();
        return await operacion(alcance.ServiceProvider.GetRequiredService<IGestionUsuariosPersonal>());
    }

    private static async Task ConGestionAsync(ServiceProvider proveedor, Func<IGestionUsuariosPersonal, Task> operacion)
    {
        using var alcance = proveedor.CreateScope();
        await operacion(alcance.ServiceProvider.GetRequiredService<IGestionUsuariosPersonal>());
    }

    private static async Task<UsuarioPersonal?> RecargarAsync(ServiceProvider proveedor, Guid id)
    {
        using var alcance = proveedor.CreateScope();
        return await alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>().FindByIdAsync(id.ToString());
    }

    private static async Task<UsuarioPersonal?> BuscarPorNombreAsync(ServiceProvider proveedor, string nombre)
    {
        using var alcance = proveedor.CreateScope();
        return await alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>().FindByNameAsync(nombre);
    }

    private static async Task<IList<string>> RolesDeAsync(ServiceProvider proveedor, Guid id)
    {
        using var alcance = proveedor.CreateScope();
        var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>();
        return await usuarios.GetRolesAsync((await usuarios.FindByIdAsync(id.ToString()))!);
    }

    private static async Task BloquearAsync(ServiceProvider proveedor, Guid id)
    {
        using var alcance = proveedor.CreateScope();
        var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>();
        var usuario = (await usuarios.FindByIdAsync(id.ToString()))!;
        for (var i = 0; i < 5; i++)
            Assert.True((await usuarios.AccessFailedAsync(usuario)).Succeeded);   // al 5.º: 15 minutos
    }

    private sealed record Fila(string? UsuarioId, string Quien, string Rol, string Canal, string Accion,
        string EntidadTipo, string EntidadId, string? Detalle);

    private async Task<List<Fila>> FilasDeAsync(Guid id)
    {
        await using var conexion = new NpgsqlConnection(fixture.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(
            """
            SELECT usuario_id, quien, rol, canal, accion, entidad_tipo, entidad_id, detalle::text
            FROM registros_auditoria WHERE entidad_id = @id ORDER BY id
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

    private async Task<long> AltasAuditadasConAsync(string marca)
    {
        await using var conexion = new NpgsqlConnection(fixture.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(
            "SELECT count(*) FROM registros_auditoria WHERE accion = @a AND detalle::text LIKE @m", conexion);
        comando.Parameters.AddWithValue("a", AccionesAuditoria.UsuarioPersonalCreado);
        comando.Parameters.AddWithValue("m", $"%{marca}%");
        return (long)(await comando.ExecuteScalarAsync())!;
    }

    private async Task<long> UsuariosConNombreAsync(string nombre)
    {
        await using var conexion = new NpgsqlConnection(fixture.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(
            "SELECT count(*) FROM asp_net_users WHERE normalized_user_name = @n", conexion);
        comando.Parameters.AddWithValue("n", nombre.ToUpperInvariant());
        return (long)(await comando.ExecuteScalarAsync())!;
    }

    private static Dictionary<string, string?> Detalle(string? detalle) =>
        JsonDocument.Parse(detalle!).RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString());

    private static void ActorAuditado(Fila fila, UsuarioPersonal admin)
    {
        Assert.Equal(admin.Id.ToString(), fila.UsuarioId);
        Assert.Equal(admin.NombreCompleto, fila.Quien);
        Assert.Equal(Roles.Administrador, fila.Rol);
        Assert.Equal(nameof(CanalAuditoria.Personal), fila.Canal);
        Assert.Equal("UsuarioPersonal", fila.EntidadTipo);
    }

    private async Task<(UsuarioPersonal Admin, ActorAuditoria Actor)> AdministradorAsync(ServiceProvider proveedor)
    {
        var admin = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Administrador], nombre: $"Admin {Marca()}");
        return (admin, UsuariosDePrueba.ActorDe(admin, Roles.Administrador));
    }

    // Guarda los mensajes de todos los registros técnicos, para revisar que la temporal no aparezca.
    private sealed class CapturaDeRegistros : ILoggerProvider, ILogger
    {
        private readonly List<string> mensajes = [];

        public IReadOnlyList<string> Mensajes { get { lock (mensajes) return [.. mensajes]; } }

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            lock (mensajes) mensajes.Add(formatter(state, exception) + " " + exception);
        }

        public void Dispose()
        {
        }
    }

    [Fact]
    [Trait("Story", "2.2")]
    [Trait("AC", "3")]
    [Trait("AC", "4")]
    [Trait("AC", "25")]
    public async Task CrearAsync_DatosValidos_CreaUsuarioActivoConContrasenaTemporalYAudita()
    {
        var captura = new CapturaDeRegistros();
        await using var proveedor = Proveedor(s => s.AddSingleton<ILoggerProvider>(captura));
        var (admin, actor) = await AdministradorAsync(proveedor);
        var m = Marca();

        var alta = await ConGestionAsync(proveedor, g => g.CrearAsync(
            new NuevoUsuarioPersonal($"  Ana Ruiz-{m}  ", $"  Ana.Ruiz-{m}  ", Roles.Medico), actor, CancellationToken.None));

        Assert.Equal($"ana.ruiz-{m}", alta.Usuario);
        Assert.Equal(Roles.Medico, alta.Rol);
        var creado = (await RecargarAsync(proveedor, alta.Id))!;
        Assert.Equal($"ana.ruiz-{m}", creado.UserName);
        Assert.Equal($"Ana Ruiz-{m}", creado.NombreCompleto);
        Assert.True(creado.Activo);
        Assert.True(creado.DebeCambiarContrasena);
        Assert.Equal([Roles.Medico], await RolesDeAsync(proveedor, alta.Id));
        Assert.NotEqual(alta.ContrasenaTemporal, creado.PasswordHash);
        var hasher = proveedor.GetRequiredService<IPasswordHasher<UsuarioPersonal>>();
        Assert.Equal(PasswordVerificationResult.Success,
            hasher.VerifyHashedPassword(creado, creado.PasswordHash!, alta.ContrasenaTemporal));

        var fila = Assert.Single(await FilasDeAsync(alta.Id));
        Assert.Equal(AccionesAuditoria.UsuarioPersonalCreado, fila.Accion);
        Assert.Equal(alta.Id.ToString(), fila.EntidadId);
        ActorAuditado(fila, admin);
        Assert.Equal(new Dictionary<string, string?>
        {
            ["usuario"] = $"ana.ruiz-{m}",
            ["nombreCompleto"] = $"Ana Ruiz-{m}",
            ["rol"] = Roles.Medico,
        }, Detalle(fila.Detalle));
        Assert.DoesNotContain(alta.ContrasenaTemporal, fila.Detalle);
        Assert.All(captura.Mensajes, mensaje => Assert.DoesNotContain(alta.ContrasenaTemporal, mensaje));
    }

    [Theory]
    [Trait("Story", "2.2")]
    [Trait("AC", "6")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CrearAsync_UsuarioRepetido_LanzaConflictoSinCrearNiAuditar(bool enMayusculas)
    {
        await using var proveedor = Proveedor();
        var (_, actor) = await AdministradorAsync(proveedor);
        var existente = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Recepcionista]);
        var marca = existente.UserName![2..];
        var tipeado = enMayusculas ? existente.UserName.ToUpperInvariant() : existente.UserName;

        var error = await Assert.ThrowsAsync<ConflictoException>(() => ConGestionAsync(proveedor, g => g.CrearAsync(
            new NuevoUsuarioPersonal("Otra Persona", tipeado, Roles.Medico), actor, CancellationToken.None)));

        Assert.Equal($"Ya existe un usuario con el nombre {existente.UserName}. Elegí otro.", error.Message);
        Assert.Equal(1, await UsuariosConNombreAsync(existente.UserName));
        Assert.Equal(0, await AltasAuditadasConAsync(marca));
    }

    public static TheoryData<string, string, string, string> DatosInvalidos => new()
    {
        // nombre, usuario ({u} = p-<marca>), rol, mensaje
        { "", "{u}", Roles.Medico, "Ingresá el nombre completo." },
        { new string('a', 121), "{u}", Roles.Medico, "El nombre completo tiene como máximo 120 caracteres." },
        { "Ana Ruiz", "a b", Roles.Medico, "El usuario tiene entre 3 y 100 caracteres: letras sin acento, números, punto, guion o guion bajo." },
        { "Ana Ruiz", "ab", Roles.Medico, "El usuario tiene entre 3 y 100 caracteres: letras sin acento, números, punto, guion o guion bajo." },
        { "Ana Ruiz", "{u}", "Superusuario", "Elegí un rol." },
        { "Ana Ruiz", "{u}", "", "Elegí un rol." },
    };

    [Theory]
    [Trait("Story", "2.2")]
    [Trait("AC", "5")]
    [MemberData(nameof(DatosInvalidos))]
    public async Task CrearAsync_DatosInvalidos_LanzaReglaDeNegocioSinCrear(string nombre, string usuario, string rol, string mensaje)
    {
        await using var proveedor = Proveedor();
        var (_, actor) = await AdministradorAsync(proveedor);
        var tipeado = usuario.Replace("{u}", $"p-{Marca()}");

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(() => ConGestionAsync(proveedor, g => g.CrearAsync(
            new NuevoUsuarioPersonal(nombre, tipeado, rol), actor, CancellationToken.None)));

        Assert.Equal(mensaje, error.Message);
        Assert.Equal(0, await UsuariosConNombreAsync(tipeado));
    }

    [Fact]
    [Trait("Story", "2.2")]
    [Trait("AC", "6")]
    public async Task CrearAsync_ChoqueConElIndiceUnico_LanzaConflicto()
    {
        await using var proveedor = Proveedor(s =>
        {
            s.RemoveAll<IUserValidator<UsuarioPersonal>>();
            s.AddScoped<IUserValidator<UsuarioPersonal>, ValidadorUsuarioQueAceptaTodo>();
        });
        var (_, actor) = await AdministradorAsync(proveedor);
        var existente = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Recepcionista]);

        var error = await Assert.ThrowsAsync<ConflictoException>(() => ConGestionAsync(proveedor, g => g.CrearAsync(
            new NuevoUsuarioPersonal("Otra Persona", existente.UserName, Roles.Medico), actor, CancellationToken.None)));

        Assert.Equal($"Ya existe un usuario con el nombre {existente.UserName}. Elegí otro.", error.Message);
        Assert.Equal(1, await UsuariosConNombreAsync(existente.UserName!));
    }

    [Fact]
    [Trait("Story", "2.2")]
    [Trait("AC", "7")]
    public async Task CrearAsync_FallaLaAuditoria_NoQuedaElUsuario()
    {
        await using var proveedor = Proveedor(s => s.AddScoped<IAuditoria, AuditoriaQueFalla>());
        var (_, actor) = await AdministradorAsync(proveedor);
        var usuario = $"p-{Marca()}";

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => ConGestionAsync(proveedor, g => g.CrearAsync(
            new NuevoUsuarioPersonal("Persona Nueva", usuario, Roles.Recepcionista), actor, CancellationToken.None)));

        Assert.Equal("auditoría caída en la prueba", error.Message);
        Assert.Null(await BuscarPorNombreAsync(proveedor, usuario));
    }

    [Fact]
    [Trait("Story", "2.2")]
    [Trait("AC", "8")]
    [Trait("AC", "25")]
    [Trait("AC", "26")]
    public async Task DesactivarAsync_UsuarioActivo_LoDesactivaCambiaSelloYAudita()
    {
        await using var proveedor = Proveedor();
        var (admin, actor) = await AdministradorAsync(proveedor);
        var usuario = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Recepcionista]);
        var selloViejo = (await RecargarAsync(proveedor, usuario.Id))!.SecurityStamp;

        await ConGestionAsync(proveedor, g => g.DesactivarAsync(usuario.Id, actor, CancellationToken.None));

        var despues = (await RecargarAsync(proveedor, usuario.Id))!;
        Assert.False(despues.Activo);
        Assert.NotEqual(selloViejo, despues.SecurityStamp);
        using (var alcance = proveedor.CreateScope())
            Assert.False(await alcance.ServiceProvider.GetRequiredService<IIngresoPersonal>()
                .SesionVigenteAsync(usuario.Id.ToString(), selloViejo, CancellationToken.None));
        var fila = Assert.Single(await FilasDeAsync(usuario.Id));
        Assert.Equal(AccionesAuditoria.UsuarioPersonalDesactivado, fila.Accion);
        ActorAuditado(fila, admin);
        Assert.Equal(new Dictionary<string, string?> { ["usuario"] = usuario.UserName }, Detalle(fila.Detalle));
    }

    [Fact]
    [Trait("Story", "2.2")]
    [Trait("AC", "9")]
    public async Task DesactivarAsync_ASiMismo_LanzaReglaSinCambios()
    {
        await using var proveedor = Proveedor();
        var (admin, actor) = await AdministradorAsync(proveedor);
        var sello = (await RecargarAsync(proveedor, admin.Id))!.SecurityStamp;

        // ThrowsAsync exige el tipo exacto: una ConflictoException (409) no pasa.
        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => ConGestionAsync(proveedor, g => g.DesactivarAsync(admin.Id, actor, CancellationToken.None)));

        Assert.Equal("No podés desactivar tu propio usuario. Pedíselo a otro administrador.", error.Message);
        var despues = (await RecargarAsync(proveedor, admin.Id))!;
        Assert.True(despues.Activo);
        Assert.Equal(sello, despues.SecurityStamp);
        Assert.DoesNotContain(await FilasDeAsync(admin.Id), f => f.Accion == AccionesAuditoria.UsuarioPersonalDesactivado);
    }

    [Fact]
    [Trait("Story", "2.2")]
    [Trait("AC", "10")]
    public async Task DesactivarAsync_DosAdministradoresALaVez_QuedaUnoActivo()
    {
        await using var proveedor = Proveedor();
        var (a, actorA) = await AdministradorAsync(proveedor);
        var (b, actorB) = await AdministradorAsync(proveedor);

        // Cada pedido en su alcance (contexto y conexión propios), lanzados a la vez.
        async Task<Exception?> Intentar(Guid objetivo, ActorAuditoria actor)
        {
            try
            {
                await ConGestionAsync(proveedor, g => g.DesactivarAsync(objetivo, actor, CancellationToken.None));
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }
        var resultados = await Task.WhenAll(
            Task.Run(() => Intentar(b.Id, actorA)),
            Task.Run(() => Intentar(a.Id, actorB)));

        Assert.Single(resultados, r => r is null);
        Assert.Single(resultados, r => r is AccesoDenegadoException { Message: SinPermiso });
        var activos = new[] { (await RecargarAsync(proveedor, a.Id))!.Activo, (await RecargarAsync(proveedor, b.Id))!.Activo };
        Assert.Single(activos, activo => activo);
    }

    [Fact]
    [Trait("Story", "2.2")]
    [Trait("AC", "11")]
    [Trait("AC", "25")]
    public async Task ReactivarAsync_UsuarioDesactivado_VuelveAIngresarConSuContrasena()
    {
        await using var proveedor = Proveedor();
        var (admin, actor) = await AdministradorAsync(proveedor);
        var usuario = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Recepcionista], activo: false);
        var antes = (await RecargarAsync(proveedor, usuario.Id))!;

        await ConGestionAsync(proveedor, g => g.ReactivarAsync(usuario.Id, actor, CancellationToken.None));

        var despues = (await RecargarAsync(proveedor, usuario.Id))!;
        Assert.True(despues.Activo);
        Assert.Equal(antes.DebeCambiarContrasena, despues.DebeCambiarContrasena);
        Assert.Equal(antes.AccessFailedCount, despues.AccessFailedCount);
        var fila = Assert.Single(await FilasDeAsync(usuario.Id));
        Assert.Equal(AccionesAuditoria.UsuarioPersonalReactivado, fila.Accion);
        ActorAuditado(fila, admin);
        Assert.Equal(new Dictionary<string, string?> { ["usuario"] = usuario.UserName }, Detalle(fila.Detalle));
        using var alcance = proveedor.CreateScope();
        var ingreso = await alcance.ServiceProvider.GetRequiredService<IIngresoPersonal>()
            .ValidarAsync(usuario.UserName!, UsuariosDePrueba.Clave, CancellationToken.None);
        Assert.Equal(EstadoIngreso.Exitoso, ingreso.Estado);
    }

    [Theory]
    [Trait("Story", "2.2")]
    [Trait("AC", "12")]
    [InlineData("desactivar", false, "Ese usuario ya estaba desactivado.", AccionesAuditoria.UsuarioPersonalDesactivado)]
    [InlineData("reactivar", true, "Ese usuario ya estaba activo.", AccionesAuditoria.UsuarioPersonalReactivado)]
    public async Task DesactivarOReactivar_EstadoQueYaTenia_LanzaConflictoSinAuditar(
        string operacion, bool activo, string mensaje, string accion)
    {
        await using var proveedor = Proveedor();
        var (_, actor) = await AdministradorAsync(proveedor);
        var usuario = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Recepcionista], activo);

        var error = await Assert.ThrowsAsync<ConflictoException>(() => ConGestionAsync(proveedor, g => operacion == "desactivar"
            ? g.DesactivarAsync(usuario.Id, actor, CancellationToken.None)
            : g.ReactivarAsync(usuario.Id, actor, CancellationToken.None)));

        Assert.Equal(mensaje, error.Message);
        Assert.DoesNotContain(await FilasDeAsync(usuario.Id), f => f.Accion == accion);
    }

    [Fact]
    [Trait("Story", "2.2")]
    [Trait("AC", "13")]
    [Trait("AC", "25")]
    public async Task DesbloquearAsync_UsuarioBloqueado_LoDesbloqueaYAudita()
    {
        await using var proveedor = Proveedor();
        var (admin, actor) = await AdministradorAsync(proveedor);
        var usuario = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Recepcionista]);
        await BloquearAsync(proveedor, usuario.Id);

        await ConGestionAsync(proveedor, g => g.DesbloquearAsync(usuario.Id, actor, CancellationToken.None));

        var despues = (await RecargarAsync(proveedor, usuario.Id))!;
        Assert.Null(despues.LockoutEnd);
        Assert.Equal(0, despues.AccessFailedCount);
        var fila = Assert.Single(await FilasDeAsync(usuario.Id));
        Assert.Equal(AccionesAuditoria.UsuarioPersonalDesbloqueado, fila.Accion);
        ActorAuditado(fila, admin);
        Assert.Equal(new Dictionary<string, string?> { ["usuario"] = usuario.UserName }, Detalle(fila.Detalle));
        using var alcance = proveedor.CreateScope();
        var ingreso = await alcance.ServiceProvider.GetRequiredService<IIngresoPersonal>()
            .ValidarAsync(usuario.UserName!, UsuariosDePrueba.Clave, CancellationToken.None);
        Assert.Equal(EstadoIngreso.Exitoso, ingreso.Estado);
    }

    [Theory]
    [Trait("Story", "2.2")]
    [Trait("AC", "14")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DesbloquearAsync_UsuarioNoBloqueado_LanzaConflicto(bool conBloqueoVencido)
    {
        await using var proveedor = Proveedor();
        var (_, actor) = await AdministradorAsync(proveedor);
        var usuario = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Recepcionista]);
        if (conBloqueoVencido)
        {
            using var alcance = proveedor.CreateScope();
            var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>();
            Assert.True((await usuarios.SetLockoutEndDateAsync(
                (await usuarios.FindByIdAsync(usuario.Id.ToString()))!, reloj.GetUtcNow().AddMinutes(-1))).Succeeded);
        }

        var error = await Assert.ThrowsAsync<ConflictoException>(
            () => ConGestionAsync(proveedor, g => g.DesbloquearAsync(usuario.Id, actor, CancellationToken.None)));

        Assert.Equal("Ese usuario no está bloqueado.", error.Message);
        Assert.DoesNotContain(await FilasDeAsync(usuario.Id), f => f.Accion == AccionesAuditoria.UsuarioPersonalDesbloqueado);
    }

    public static TheoryData<string, string> OperacionesPorActor()
    {
        var datos = new TheoryData<string, string>();
        foreach (var operacion in new[] { "crear", "desactivar", "reactivar", "desbloquear" })
            foreach (var actor in new[] { "recepcionista", "administrador-desactivado", "id-inexistente" })
                datos.Add(operacion, actor);
        return datos;
    }

    [Theory]
    [Trait("Story", "2.2")]
    [Trait("AC", "15")]
    [MemberData(nameof(OperacionesPorActor))]
    public async Task Operaciones_ActorQueNoEsAdministradorActivo_LanzaAccesoDenegado(string operacion, string tipoDeActor)
    {
        await using var proveedor = Proveedor();
        var actor = tipoDeActor switch
        {
            "recepcionista" => UsuariosDePrueba.ActorDe(
                await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Recepcionista]), Roles.Administrador),
            "administrador-desactivado" => UsuariosDePrueba.ActorDe(
                await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Administrador], activo: false), Roles.Administrador),
            _ => new ActorAuditoria(Guid.NewGuid().ToString(), "Nadie", Roles.Administrador, CanalAuditoria.Personal),
        };
        // El objetivo de cada operación, en el estado en que la operación sí haría algo.
        var objetivo = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Recepcionista], activo: operacion != "reactivar");
        if (operacion == "desbloquear")
            await BloquearAsync(proveedor, objetivo.Id);
        var antes = (await RecargarAsync(proveedor, objetivo.Id))!;
        var nuevo = $"p-{Marca()}";

        var error = await Assert.ThrowsAsync<AccesoDenegadoException>(() => ConGestionAsync(proveedor, g => operacion switch
        {
            "crear" => g.CrearAsync(new NuevoUsuarioPersonal("Persona Nueva", nuevo, Roles.Medico), actor, CancellationToken.None),
            "desactivar" => g.DesactivarAsync(objetivo.Id, actor, CancellationToken.None),
            "reactivar" => g.ReactivarAsync(objetivo.Id, actor, CancellationToken.None),
            _ => g.DesbloquearAsync(objetivo.Id, actor, CancellationToken.None),
        }));

        Assert.Equal(SinPermiso, error.Message);
        Assert.Null(await BuscarPorNombreAsync(proveedor, nuevo));
        var despues = (await RecargarAsync(proveedor, objetivo.Id))!;
        Assert.Equal(antes.Activo, despues.Activo);
        Assert.Equal(antes.LockoutEnd, despues.LockoutEnd);
        Assert.Equal(antes.SecurityStamp, despues.SecurityStamp);
        Assert.Empty(await FilasDeAsync(objetivo.Id));
    }

    [Theory]
    [Trait("Story", "2.2")]
    [Trait("AC", "16")]
    [InlineData("desactivar")]
    [InlineData("reactivar")]
    [InlineData("desbloquear")]
    public async Task Operaciones_UsuarioInexistente_LanzaNoEncontrado(string operacion)
    {
        await using var proveedor = Proveedor();
        var (_, actor) = await AdministradorAsync(proveedor);
        var inexistente = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<NoEncontradoException>(() => ConGestionAsync(proveedor, g => operacion switch
        {
            "desactivar" => g.DesactivarAsync(inexistente, actor, CancellationToken.None),
            "reactivar" => g.ReactivarAsync(inexistente, actor, CancellationToken.None),
            _ => g.DesbloquearAsync(inexistente, actor, CancellationToken.None),
        }));

        Assert.Equal("No encontramos ese usuario. Recargá la página.", error.Message);
    }

    [Fact]
    [Trait("Story", "2.2")]
    [Trait("AC", "1")]
    public async Task ListarAsync_UsuariosEnVariosEstados_DevuelveRolEstadoYBloqueo()
    {
        await using var proveedor = Proveedor();
        var m = Marca();
        var uno = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Medico], nombre: $"Zz-{m}-1");
        var dos = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Recepcionista], activo: false, nombre: $"Zz-{m}-2");
        var tres = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Administrador], nombre: $"Zz-{m}-3");
        var cuatro = await UsuariosDePrueba.CrearAsync(proveedor, [], nombre: $"Zz-{m}-4");
        using (var alcance = proveedor.CreateScope())
        {
            var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>();
            var cargado = (await usuarios.FindByIdAsync(tres.Id.ToString()))!;
            cargado.DebeCambiarContrasena = true;
            Assert.True((await usuarios.UpdateAsync(cargado)).Succeeded);
            Assert.True((await usuarios.SetLockoutEndDateAsync(cargado, reloj.GetUtcNow().AddMinutes(15))).Succeeded);
        }

        var lista = await ConGestionAsync(proveedor, g => g.ListarAsync(CancellationToken.None));

        var propias = lista.Where(f => f.NombreCompleto.StartsWith($"Zz-{m}-", StringComparison.Ordinal)).ToList();
        Assert.Equal([uno.Id, dos.Id, tres.Id, cuatro.Id], propias.Select(f => f.Id));
        Assert.Equal([Roles.Medico, Roles.Recepcionista, Roles.Administrador, "(sin rol)"], propias.Select(f => f.Rol));
        Assert.Equal([true, false, true, true], propias.Select(f => f.Activo));
        Assert.Equal([false, false, true, false], propias.Select(f => f.BloqueadoHasta is not null));
        Assert.Equal([false, false, true, false], propias.Select(f => f.DebeCambiarContrasena));
        Assert.Equal(uno.UserName, propias[0].Usuario);
    }
}
