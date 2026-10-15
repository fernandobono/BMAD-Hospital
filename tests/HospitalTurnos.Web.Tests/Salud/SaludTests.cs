using System.Diagnostics;
using System.Net;
using DotNet.Testcontainers.Configurations;

namespace HospitalTurnos.Web.Tests.Salud;

[Collection("Postgres")]
public class SaludTests(PostgresFixture fixture)
{
    [Fact]
    [Trait("Story", "1.1")]
    [Trait("AC", "9")]
    public async Task Salud_BaseDisponible_Devuelve200ConEstadoOk()
    {
        await using var fabrica = new HospitalWebApplicationFactory(fixture);
        var cliente = fabrica.CreateClient();
        var reloj = Stopwatch.StartNew();

        var respuesta = await cliente.GetAsync("/salud");

        reloj.Stop();
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("application/json", respuesta.Content.Headers.ContentType?.MediaType);
        Assert.Equal("""{"estado":"ok"}""", await respuesta.Content.ReadAsStringAsync());
        Assert.True(reloj.Elapsed < TimeSpan.FromSeconds(3), $"Tardó {reloj.Elapsed.TotalSeconds:F1} s");
    }
}

// Fuera de la colección: esta prueba DETIENE su contenedor, no puede compartirlo con nadie.
public class SaludConBaseCaidaTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture = new(Unix.FileMode755, aplicarMigraciones: true);

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    [Trait("Story", "1.1")]
    [Trait("AC", "10")]
    public async Task Salud_BaseCaidaDespuesDeArrancar_Devuelve503SinDetalles()
    {
        await using var fabrica = new HospitalWebApplicationFactory(_fixture);
        var cliente = fabrica.CreateClient();   // arranca la app con la base disponible
        await _fixture.Contenedor.StopAsync();
        var reloj = Stopwatch.StartNew();

        var respuesta = await cliente.GetAsync("/salud");

        reloj.Stop();
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
        Assert.Equal("""{"estado":"sin conexión con la base"}""", cuerpo);
        Assert.DoesNotContain("Npgsql", cuerpo);
        Assert.DoesNotContain("Exception", cuerpo);
        Assert.DoesNotContain("Host=", cuerpo);
        Assert.True(reloj.Elapsed <= TimeSpan.FromSeconds(5), $"Tardó {reloj.Elapsed.TotalSeconds:F1} s");
    }
}
