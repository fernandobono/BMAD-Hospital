using System.Net;

namespace HospitalTurnos.Web.Tests.Inicio;

[Collection("Postgres")]
public class InicioTests(PostgresFixture fixture)
{
    [Fact]
    [Trait("Story", "1.1")]
    [Trait("AC", "11")]
    public async Task Inicio_Get_MuestraNombreDelHospitalYNoLasPaginasDeEjemplo()
    {
        await using var fabrica = new HospitalWebApplicationFactory(fixture);
        var cliente = fabrica.CreateClient();

        var inicio = await cliente.GetAsync("/");
        var contador = await cliente.GetAsync("/counter");
        var clima = await cliente.GetAsync("/weather");

        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);
        // Se decodifica por si el HTML escribe "ó" como entidad.
        var html = WebUtility.HtmlDecode(await inicio.Content.ReadAsStringAsync());
        Assert.Contains("""<html lang="es">""", html);
        Assert.Matches(@"<h1[^>]*>\s*Hospital Municipal Las Acacias\s*</h1>", html);
        Assert.Contains("Gestión de turnos", html);
        Assert.Equal(HttpStatusCode.NotFound, contador.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, clima.StatusCode);
    }
}
