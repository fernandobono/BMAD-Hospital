using System.Security.Claims;
using System.Text.Json;
using HospitalTurnos.Aplicacion.Personal;
using HospitalTurnos.Aplicacion.Tests.Ayudas;
using HospitalTurnos.Dominio.Auditoria;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Infraestructura;
using HospitalTurnos.Infraestructura.Identidad;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Npgsql;

namespace HospitalTurnos.Aplicacion.Tests.Personal;

[Collection("Postgres")]
public class CambioContrasenaPersonalIdentityTests(PostgresFixture fixture)
{
    private const string Nueva = "Nueva-Clave-2026";

    private ServiceProvider Proveedor()
    {
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:App"] = fixture.CadenaApp,
            ["ConnectionStrings:Migraciones"] = fixture.CadenaDuenio,
        }).Build();
        var reloj = new FakeTimeProvider(DateTimeOffset.UtcNow);
        reloj.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Cordoba"));
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AgregarInfraestructura(configuracion);
        servicios.AddIdentidadPersonal(configuracion);
        servicios.AddSingleton<TimeProvider>(reloj);
        return servicios.BuildServiceProvider();
    }

    private static async Task<ResultadoCambioContrasena> CambiarAsync(ServiceProvider proveedor, Guid id, string actual, string nueva)
    {
        using var alcance = proveedor.CreateScope();
        return await alcance.ServiceProvider.GetRequiredService<ICambioContrasenaPersonal>()
            .CambiarAsync(id.ToString(), actual, nueva, CancellationToken.None);
    }

    private static async Task<ResultadoIngreso> IngresarAsync(ServiceProvider proveedor, string usuario, string clave)
    {
        using var alcance = proveedor.CreateScope();
        return await alcance.ServiceProvider.GetRequiredService<IIngresoPersonal>()
            .ValidarAsync(usuario, clave, CancellationToken.None);
    }

    private static async Task<UsuarioPersonal> RecargarAsync(ServiceProvider proveedor, Guid id)
    {
        using var alcance = proveedor.CreateScope();
        return (await alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>()
            .FindByIdAsync(id.ToString()))!;
    }

    private static async Task MarcarAsync(ServiceProvider proveedor, Guid id)
    {
        using var alcance = proveedor.CreateScope();
        var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>();
        var usuario = (await usuarios.FindByIdAsync(id.ToString()))!;
        usuario.DebeCambiarContrasena = true;
        Assert.True((await usuarios.UpdateAsync(usuario)).Succeeded);
    }

    private sealed record Fila(string? UsuarioId, string EntidadId, string Accion, string? Detalle);

    private async Task<List<Fila>> CambiosAuditadosDeAsync(Guid id)
    {
        await using var conexion = new NpgsqlConnection(fixture.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(
            """
            SELECT usuario_id, entidad_id, accion, detalle::text FROM registros_auditoria
            WHERE entidad_id = @id AND accion = @a ORDER BY id
            """, conexion);
        comando.Parameters.AddWithValue("id", id.ToString());
        comando.Parameters.AddWithValue("a", AccionesAuditoria.ContrasenaPersonalCambiada);
        var filas = new List<Fila>();
        await using var lector = await comando.ExecuteReaderAsync();
        while (await lector.ReadAsync())
            filas.Add(new Fila(lector.IsDBNull(0) ? null : lector.GetString(0), lector.GetString(1),
                lector.GetString(2), lector.IsDBNull(3) ? null : lector.GetString(3)));
        return filas;
    }

    private static string? Motivo(string? detalle) =>
        JsonDocument.Parse(detalle!).RootElement.GetProperty("motivo").GetString();

    [Fact]
    [Trait("Story", "2.2")]
    [Trait("AC", "19")]
    [Trait("AC", "21")]
    [Trait("AC", "25")]
    public async Task CambiarAsync_ContrasenaTemporal_CambiaHashSelloYBanderaYAudita()
    {
        await using var proveedor = Proveedor();
        var admin = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Administrador]);
        AltaUsuarioPersonal alta;
        using (var alcance = proveedor.CreateScope())
            alta = await alcance.ServiceProvider.GetRequiredService<IGestionUsuariosPersonal>().CrearAsync(
                new NuevoUsuarioPersonal("Persona Temporal", $"p-{Guid.NewGuid().ToString("N")[..8]}", Roles.Recepcionista),
                UsuariosDePrueba.ActorDe(admin, Roles.Administrador), CancellationToken.None);
        var selloDelAlta = (await RecargarAsync(proveedor, alta.Id)).SecurityStamp;
        var selloClaim = proveedor.GetRequiredService<IOptions<IdentityOptions>>().Value.ClaimsIdentity.SecurityStampClaimType;

        var conTemporal = await IngresarAsync(proveedor, alta.Usuario, alta.ContrasenaTemporal);
        Assert.Equal(EstadoIngreso.Exitoso, conTemporal.Estado);
        Assert.True(conTemporal.DebeCambiarContrasena);
        Assert.Equal("si", conTemporal.Principal!.FindFirstValue(ClaimsPersonal.DebeCambiarContrasena));

        var cambio = await CambiarAsync(proveedor, alta.Id, alta.ContrasenaTemporal, Nueva);

        Assert.True(cambio.Exitoso, string.Join(" ", cambio.Errores));
        Assert.Equal(Roles.Recepcionista, cambio.Rol);
        var principal = Assert.IsType<ClaimsPrincipal>(cambio.Principal);
        Assert.Null(principal.FindFirstValue(ClaimsPersonal.DebeCambiarContrasena));
        var despues = await RecargarAsync(proveedor, alta.Id);
        Assert.False(despues.DebeCambiarContrasena);
        Assert.NotEqual(selloDelAlta, despues.SecurityStamp);
        Assert.Equal(despues.SecurityStamp, principal.FindFirstValue(selloClaim));
        Assert.Equal(EstadoIngreso.CredencialesInvalidas,
            (await IngresarAsync(proveedor, alta.Usuario, alta.ContrasenaTemporal)).Estado);
        var conNueva = await IngresarAsync(proveedor, alta.Usuario, Nueva);
        Assert.Equal(EstadoIngreso.Exitoso, conNueva.Estado);
        Assert.False(conNueva.DebeCambiarContrasena);

        var fila = Assert.Single(await CambiosAuditadosDeAsync(alta.Id));
        Assert.Equal(alta.Id.ToString(), fila.UsuarioId);
        Assert.Equal(alta.Id.ToString(), fila.EntidadId);
        Assert.Equal("primer-ingreso", Motivo(fila.Detalle));
    }

    public static TheoryData<string, string, string[]> DatosQueNoSirven => new()
    {
        // actual, nueva, mensajes esperados
        { "Mala-Clave-99", Nueva, ["La contraseña actual no es correcta."] },
        { UsuariosDePrueba.Clave, UsuariosDePrueba.Clave, ["La nueva contraseña tiene que ser distinta de la actual."] },
        { UsuariosDePrueba.Clave, "abc",
            [
                "La contraseña tiene que tener al menos 10 caracteres.",
                "La contraseña tiene que tener al menos una mayúscula.",
                "La contraseña tiene que tener al menos un número.",
                "La contraseña tiene que tener al menos un símbolo, como - o #.",
            ] },
    };

    [Theory]
    [Trait("Story", "2.2")]
    [Trait("AC", "22")]
    [Trait("AC", "24")]
    [MemberData(nameof(DatosQueNoSirven))]
    public async Task CambiarAsync_DatosInvalidos_DevuelveMensajesEnCastellanoSinCambiar(string actual, string nueva, string[] mensajes)
    {
        await using var proveedor = Proveedor();
        var usuario = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Recepcionista]);
        await MarcarAsync(proveedor, usuario.Id);
        var antes = await RecargarAsync(proveedor, usuario.Id);

        var cambio = await CambiarAsync(proveedor, usuario.Id, actual, nueva);

        Assert.False(cambio.Exitoso);
        Assert.Null(cambio.Principal);
        Assert.Equal(mensajes.Order(), cambio.Errores.Order());
        var despues = await RecargarAsync(proveedor, usuario.Id);
        Assert.Equal(antes.PasswordHash, despues.PasswordHash);
        Assert.Equal(antes.SecurityStamp, despues.SecurityStamp);
        Assert.True(despues.DebeCambiarContrasena);
        Assert.Equal(0, despues.AccessFailedCount);
        Assert.Empty(await CambiosAuditadosDeAsync(usuario.Id));
    }

    [Fact]
    [Trait("Story", "2.2")]
    [Trait("AC", "23")]
    public async Task CambiarAsync_UsuarioSinBandera_CambiaYAuditaComoVoluntario()
    {
        await using var proveedor = Proveedor();
        var usuario = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Medico]);

        var cambio = await CambiarAsync(proveedor, usuario.Id, UsuariosDePrueba.Clave, Nueva);

        Assert.True(cambio.Exitoso, string.Join(" ", cambio.Errores));
        Assert.Equal(Roles.Medico, cambio.Rol);
        var fila = Assert.Single(await CambiosAuditadosDeAsync(usuario.Id));
        Assert.Equal("voluntario", Motivo(fila.Detalle));
    }
}
