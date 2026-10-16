using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace HospitalTurnos.Web.Tests;

/// <summary>
/// La aplicación web completa en memoria, apuntada al PostgreSQL de la fixture.
/// Por qué AddInMemoryCollection y no UseSetting (revisión I-15): en Development se cargan
/// appsettings.Development.json y user-secrets DESPUÉS de UseSetting y le pueden ganar. La fuente en
/// memoria que agrega la fábrica es la última y gana siempre. Lo vigila T11.
/// </summary>
public class HospitalWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string? _cadenaApp;
    private readonly string? _cadenaMigraciones;

    public HospitalWebApplicationFactory(PostgresFixture fixture)
        : this(fixture.CadenaApp, fixture.CadenaDuenio)
    {
    }

    // Una cadena en null la "quita": la fuente en memoria le gana a cualquier otra (T4).
    public HospitalWebApplicationFactory(string? cadenaApp, string? cadenaMigraciones)
    {
        _cadenaApp = cadenaApp;
        _cadenaMigraciones = cadenaMigraciones;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Testing, salvo que la prueba pida otro entorno con WithWebHostBuilder.
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:App"] = _cadenaApp,
            ["ConnectionStrings:Migraciones"] = _cadenaMigraciones,
            // Story 1.4: sin configuración SMTP la app no arranca (AC 16). Puerto 1: nadie escucha, así que
            // un email encolado en una prueba de Web sólo suma reintentos y nunca sale.
            ["Email:Smtp:Host"] = "127.0.0.1",
            ["Email:Smtp:Puerto"] = "1",
            ["Email:Smtp:Seguridad"] = "None",
            ["Email:Smtp:RemitenteNombre"] = "Hospital Las Acacias",
            ["Email:Smtp:RemitenteDireccion"] = "turnos@lasacacias.example",
        }));
    }
}
