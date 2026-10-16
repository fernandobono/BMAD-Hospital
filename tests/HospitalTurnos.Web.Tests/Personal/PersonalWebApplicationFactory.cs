using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Infraestructura.Identidad;
using HospitalTurnos.Web.Tests.Ayudas;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace HospitalTurnos.Web.Tests.Personal;

/// <summary>
/// La fábrica de 1.1 más lo que necesita el ingreso del personal: un reloj de prueba (la cookie y su
/// revalidación lo leen), el registro técnico en memoria, el administrador inicial vacío (como Compose
/// sin .env) y los tres usuarios fijos, sembrados de forma idempotente.
/// </summary>
public class PersonalWebApplicationFactory(PostgresFixture fixture) : HospitalWebApplicationFactory(fixture)
{
    // Usuarios fijos: sólo para flujos que no cambian su estado (ingresar, ver secciones, salir).
    public static readonly UsuarioFijo Recepcion = new("recepcion1", "Recepcion-2026", Roles.Recepcionista, "Carla Gómez");
    public static readonly UsuarioFijo Medico = new("medico1", "Medico-2026x", Roles.Medico, "Diego Fernández");
    public static readonly UsuarioFijo Admin = new("admin1", "Admin-2026xx", Roles.Administrador, "Lucía Pérez");
    public static readonly UsuarioFijo[] Fijos = [Recepcion, Medico, Admin];

    public FakeTimeProvider Reloj { get; } = CrearReloj();

    public ProveedorDeRegistroEnMemoria Registro { get; } = new();

    private static FakeTimeProvider CrearReloj()
    {
        // Arranca en la hora real: Identity calcula el bloqueo con el reloj del sistema y la cookie con éste.
        var reloj = new FakeTimeProvider(DateTimeOffset.UtcNow);
        reloj.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Cordoba"));
        return reloj;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Personal:AdministradorInicial:Usuario"] = "",
            ["Personal:AdministradorInicial:Contrasena"] = "",
        }));
        builder.ConfigureTestServices(s =>
        {
            s.AddSingleton<TimeProvider>(Reloj);
            s.AddSingleton<ILoggerProvider>(Registro);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var alcance = host.Services.CreateScope();
        var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>();
        foreach (var fijo in Fijos)
        {
            if (usuarios.FindByNameAsync(fijo.Usuario).GetAwaiter().GetResult() is not null)
                continue;
            var usuario = new UsuarioPersonal { UserName = fijo.Usuario, NombreCompleto = fijo.Nombre };
            Exito(usuarios.CreateAsync(usuario, fijo.Contrasena).GetAwaiter().GetResult());
            Exito(usuarios.AddToRoleAsync(usuario, fijo.Rol).GetAwaiter().GetResult());
        }
        return host;
    }

    private static void Exito(IdentityResult resultado)
    {
        if (!resultado.Succeeded)
            throw new InvalidOperationException(string.Join("; ", resultado.Errors.Select(e => e.Description)));
    }
}

public sealed record UsuarioFijo(string Usuario, string Contrasena, string Rol, string Nombre);
