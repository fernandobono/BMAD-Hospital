using System.Security.Claims;
using System.Text.Json;
using HospitalTurnos.Aplicacion.Personal;
using HospitalTurnos.Aplicacion.Tests.Ayudas;
using HospitalTurnos.Aplicacion.Tests.Personal.Dobles;
using HospitalTurnos.Dominio.Auditoria;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Infraestructura;
using HospitalTurnos.Infraestructura.Identidad;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace HospitalTurnos.Aplicacion.Tests.Personal;

// Contra la base real: Identity, los permisos de turnos_app y la auditoría viven ahí.
// La base es compartida y registros_auditoria no se puede limpiar: cada prueba crea su propio usuario
// y filtra la auditoría por su id.
[Collection("Postgres")]
public class IngresoPersonalIdentityTests(PostgresFixture fixture)
{
    private const string ClaveCorrecta = "Clave-Correcta-1";

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

    private static async Task<UsuarioPersonal> CrearUsuarioAsync(
        ServiceProvider proveedor, string[] roles, bool activo = true, string nombre = "Persona de Prueba")
    {
        using var alcance = proveedor.CreateScope();
        var rolesIdentity = alcance.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var rol in Roles.Todos)
            if (!await rolesIdentity.RoleExistsAsync(rol))
                await rolesIdentity.CreateAsync(new IdentityRole<Guid>(rol));

        var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>();
        var usuario = new UsuarioPersonal
        {
            UserName = $"p-{Guid.NewGuid().ToString("N")[..8]}",
            NombreCompleto = nombre,
            Activo = activo,
        };
        AsegurarExito(await usuarios.CreateAsync(usuario, ClaveCorrecta));
        if (roles.Length > 0)
            AsegurarExito(await usuarios.AddToRolesAsync(usuario, roles));
        return usuario;
    }

    private static void AsegurarExito(IdentityResult resultado) =>
        Assert.True(resultado.Succeeded, string.Join("; ", resultado.Errors.Select(e => e.Description)));

    private static async Task<ResultadoIngreso> IngresarAsync(ServiceProvider proveedor, string usuario, string clave)
    {
        using var alcance = proveedor.CreateScope();   // un alcance por pedido, como en la web
        return await alcance.ServiceProvider.GetRequiredService<IIngresoPersonal>()
            .ValidarAsync(usuario, clave, CancellationToken.None);
    }

    private static async Task<UsuarioPersonal> RecargarAsync(ServiceProvider proveedor, Guid id)
    {
        using var alcance = proveedor.CreateScope();
        return (await alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>()
            .FindByIdAsync(id.ToString()))!;
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

    // jsonb normaliza los espacios: se compara el valor, no el texto.
    private static string? Motivo(string? detalle) =>
        detalle is null ? null : JsonDocument.Parse(detalle).RootElement.GetProperty("motivo").GetString();

    private static void CamposObligatorios(Fila fila, UsuarioPersonal usuario, string rol)
    {
        Assert.Equal(usuario.Id.ToString(), fila.UsuarioId);
        Assert.Equal(usuario.Id.ToString(), fila.EntidadId);
        Assert.Equal(usuario.NombreCompleto, fila.Quien);
        Assert.Equal(rol, fila.Rol);
        Assert.Equal(nameof(CanalAuditoria.Personal), fila.Canal);
        Assert.Equal("UsuarioPersonal", fila.EntidadTipo);
    }

    [Fact]
    [Trait("Story", "2.1")]
    [Trait("AC", "1")]
    [Trait("AC", "22")]
    [Trait("AC", "23")]
    public async Task ValidarAsync_CredencialesCorrectas_DevuelveExitosoYRegistraIngreso()
    {
        await using var proveedor = Proveedor();
        var usuario = await CrearUsuarioAsync(proveedor, [Roles.Recepcionista], nombre: "Carla Gómez");
        using (var alcance = proveedor.CreateScope())
        {
            var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>();
            var cargado = (await usuarios.FindByIdAsync(usuario.Id.ToString()))!;
            await usuarios.AccessFailedAsync(cargado);
            await usuarios.AccessFailedAsync(cargado);
        }

        var resultado = await IngresarAsync(proveedor, usuario.UserName!, ClaveCorrecta);

        Assert.Equal(EstadoIngreso.Exitoso, resultado.Estado);
        Assert.Equal(Roles.Recepcionista, resultado.Rol);
        var principal = Assert.IsType<ClaimsPrincipal>(resultado.Principal);
        Assert.Equal(Roles.Recepcionista, principal.FindFirstValue(ClaimTypes.Role));
        Assert.Equal("Carla Gómez", principal.FindFirstValue(ClaimsPersonal.NombreCompleto));
        var selloClaim = proveedor.GetRequiredService<IOptions<IdentityOptions>>().Value.ClaimsIdentity.SecurityStampClaimType;
        Assert.False(string.IsNullOrEmpty(principal.FindFirstValue(selloClaim)));
        Assert.Equal(0, (await RecargarAsync(proveedor, usuario.Id)).AccessFailedCount);

        var fila = Assert.Single(await FilasDeAsync(usuario.Id));
        Assert.Equal(AccionesAuditoria.IngresoPersonal, fila.Accion);
        CamposObligatorios(fila, usuario, Roles.Recepcionista);
        Assert.Null(fila.Detalle);
    }

    [Fact]
    [Trait("Story", "2.1")]
    [Trait("AC", "2")]
    [Trait("AC", "22")]
    public async Task ValidarAsync_ContrasenaIncorrecta_RegistraFalloConCamposObligatorios()
    {
        await using var proveedor = Proveedor();
        var usuario = await CrearUsuarioAsync(proveedor, [Roles.Recepcionista]);

        var resultado = await IngresarAsync(proveedor, usuario.UserName!, "Mala-Clave-1");

        Assert.Equal(EstadoIngreso.CredencialesInvalidas, resultado.Estado);
        Assert.Null(resultado.Principal);
        var fila = Assert.Single(await FilasDeAsync(usuario.Id));
        Assert.Equal(AccionesAuditoria.IngresoPersonalFallido, fila.Accion);
        CamposObligatorios(fila, usuario, Roles.Recepcionista);
        Assert.Equal("contrasena-incorrecta", Motivo(fila.Detalle));
        Assert.DoesNotContain("Mala-Clave-1", fila.Detalle);
        Assert.Equal(1, (await RecargarAsync(proveedor, usuario.Id)).AccessFailedCount);
    }

    [Fact]
    [Trait("Story", "2.1")]
    [Trait("AC", "3")]
    [Trait("AC", "21")]
    [Trait("AC", "22")]
    public async Task ValidarAsync_QuintoIntentoFallido_BloqueaQuinceMinutosYRegistraBloqueo()
    {
        await using var proveedor = Proveedor();
        var usuario = await CrearUsuarioAsync(proveedor, [Roles.Recepcionista]);

        for (var i = 1; i <= 4; i++)
            Assert.Equal(EstadoIngreso.CredencialesInvalidas,
                (await IngresarAsync(proveedor, usuario.UserName!, "Mala-Clave-1")).Estado);
        var quinto = await IngresarAsync(proveedor, usuario.UserName!, "Mala-Clave-1");
        // Identity calcula el bloqueo con el reloj del sistema (no usa TimeProvider): se compara igual.
        var ahora = DateTimeOffset.UtcNow;
        var conLaCorrecta = await IngresarAsync(proveedor, usuario.UserName!, ClaveCorrecta);

        Assert.Equal(EstadoIngreso.Bloqueado, quinto.Estado);
        Assert.Equal(EstadoIngreso.Bloqueado, conLaCorrecta.Estado);
        Assert.Null(conLaCorrecta.Principal);
        var fin = (await RecargarAsync(proveedor, usuario.Id)).LockoutEnd;
        Assert.NotNull(fin);
        Assert.InRange(fin.Value, ahora.AddMinutes(14), ahora.AddMinutes(16));

        var bloqueos = (await FilasDeAsync(usuario.Id))
            .Where(f => f.Accion == AccionesAuditoria.IngresoPersonalBloqueado).ToList();
        Assert.Equal(2, bloqueos.Count);
        Assert.All(bloqueos, f => CamposObligatorios(f, usuario, Roles.Recepcionista));
        Assert.Equal(["bloqueo", "intento-durante-bloqueo"], bloqueos.Select(f => Motivo(f.Detalle)));
    }

    [Fact]
    [Trait("Story", "2.1")]
    [Trait("AC", "4")]
    public async Task ValidarAsync_BloqueoVencido_PermiteIngresarYReiniciaContador()
    {
        await using var proveedor = Proveedor();
        var usuario = await CrearUsuarioAsync(proveedor, [Roles.Medico]);
        using (var alcance = proveedor.CreateScope())
        {
            var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>();
            var cargado = (await usuarios.FindByIdAsync(usuario.Id.ToString()))!;
            cargado.AccessFailedCount = 5;
            AsegurarExito(await usuarios.UpdateAsync(cargado));
            // Identity no usa TimeProvider: el bloqueo vencido se escribe, no se adelanta un reloj.
            AsegurarExito(await usuarios.SetLockoutEndDateAsync(cargado, DateTimeOffset.UtcNow.AddMinutes(-1)));
        }

        var resultado = await IngresarAsync(proveedor, usuario.UserName!, ClaveCorrecta);

        Assert.Equal(EstadoIngreso.Exitoso, resultado.Estado);
        Assert.Equal(0, (await RecargarAsync(proveedor, usuario.Id)).AccessFailedCount);
    }

    public static TheoryData<string, string[], bool, EstadoIngreso, string, string> CasosSinIngreso => new()
    {
        // caso, roles, activo, estado esperado, rol auditado, motivo
        { "desactivado", [Roles.Medico], false, EstadoIngreso.Desactivado, Roles.Medico, "desactivado" },
        { "sin rol", [], true, EstadoIngreso.SinRol, "(sin rol)", "sin-rol" },
        { "varios roles", [Roles.Recepcionista, Roles.Administrador], true, EstadoIngreso.VariosRoles, "(varios roles)", "varios-roles" },
    };

    [Theory]
    [Trait("Story", "2.1")]
    [Trait("AC", "5")]
    [Trait("AC", "24")]
    [MemberData(nameof(CasosSinIngreso))]
    public async Task ValidarAsync_UsuarioDesactivadoSinRolOConVariosRoles_NoIngresaYRegistraMotivo(
        string caso, string[] roles, bool activo, EstadoIngreso esperado, string rolAuditado, string motivo)
    {
        await using var proveedor = Proveedor();
        var usuario = await CrearUsuarioAsync(proveedor, roles, activo);

        var resultado = await IngresarAsync(proveedor, usuario.UserName!, ClaveCorrecta);

        Assert.True(esperado == resultado.Estado, $"{caso}: {resultado.Estado}");
        Assert.Null(resultado.Principal);
        var fila = Assert.Single(await FilasDeAsync(usuario.Id));
        Assert.Equal(AccionesAuditoria.IngresoPersonalFallido, fila.Accion);
        CamposObligatorios(fila, usuario, rolAuditado);
        Assert.Equal(motivo, Motivo(fila.Detalle));
    }

    // Doble de UserManager: todo real salvo guardar el intento fallido, que falla como un conflicto de
    // concurrencia. Es la única forma de llegar a esa rama: con la base real no se puede provocar a pedido.
    private sealed class UsuariosQueNoGuardanIntentos(
        IUserStore<UsuarioPersonal> store, IOptions<IdentityOptions> opciones, IPasswordHasher<UsuarioPersonal> hasher,
        IEnumerable<IUserValidator<UsuarioPersonal>> validadoresDeUsuario,
        IEnumerable<IPasswordValidator<UsuarioPersonal>> validadoresDeContrasena, ILookupNormalizer normalizador,
        IdentityErrorDescriber errores, IServiceProvider servicios, ILogger<UserManager<UsuarioPersonal>> registro)
        : UserManager<UsuarioPersonal>(store, opciones, hasher, validadoresDeUsuario, validadoresDeContrasena,
            normalizador, errores, servicios, registro)
    {
        public override Task<IdentityResult> AccessFailedAsync(UsuarioPersonal user) =>
            Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "ConcurrencyFailure" }));
    }

    [Fact]
    [Trait("Story", "2.1")]
    [Trait("AC", "3")]
    [Trait("AC", "9")]
    public async Task ValidarAsync_NoSePuedeGuardarElIntentoFallido_LanzaSinIngresar()
    {
        await using var proveedor = Proveedor(s => s.AddScoped<UserManager<UsuarioPersonal>, UsuariosQueNoGuardanIntentos>());
        var usuario = await CrearUsuarioAsync(proveedor, [Roles.Recepcionista]);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => IngresarAsync(proveedor, usuario.UserName!, "Mala-Clave-1"));

        Assert.Contains("ConcurrencyFailure", error.Message);
        Assert.Empty(await FilasDeAsync(usuario.Id));
    }

    [Fact]
    [Trait("Story", "2.1")]
    [Trait("AC", "2")]
    [Trait("AC", "21")]
    public async Task ValidarAsync_UsuarioInexistente_NoAuditaYVerificaHashFicticio()
    {
        var marcador = Guid.NewGuid().ToString("N");
        var usuarioTipeado = $"noexiste-{marcador}";
        var claveTipeada = $"Clave-Tipeada-{marcador}";
        var hasher = new HasherQueCuenta();
        var registro = new RegistroEnMemoria<IngresoPersonalIdentity>();
        await using var proveedor = Proveedor(s =>
        {
            s.AddSingleton<IPasswordHasher<UsuarioPersonal>>(hasher);
            s.AddSingleton<ILogger<IngresoPersonalIdentity>>(registro);
        });

        var resultado = await IngresarAsync(proveedor, usuarioTipeado, claveTipeada);

        Assert.Equal(EstadoIngreso.CredencialesInvalidas, resultado.Estado);
        Assert.Equal(1, hasher.Verificaciones);
        var aviso = Assert.Single(registro.Entradas, e => e.Nivel == LogLevel.Warning);
        Assert.Equal("Ingreso del personal fallido: el usuario no existe.", aviso.Mensaje);
        Assert.All(registro.Entradas, e =>
        {
            Assert.DoesNotContain(marcador, e.Mensaje);
            Assert.DoesNotContain(marcador, e.Excepcion?.ToString() ?? "");
        });

        await using var conexion = new NpgsqlConnection(fixture.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(
            """
            SELECT count(*) FROM registros_auditoria
            WHERE quien LIKE @m OR entidad_id LIKE @m OR coalesce(detalle::text, '') LIKE @m
            """, conexion);
        comando.Parameters.AddWithValue("m", $"%{marcador}%");
        Assert.Equal(0L, (long)(await comando.ExecuteScalarAsync())!);
    }
}
