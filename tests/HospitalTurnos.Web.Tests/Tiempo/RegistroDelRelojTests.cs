using HospitalTurnos.Infraestructura.Tiempo;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalTurnos.Web.Tests.Tiempo;

[Collection("Postgres")]
public class RegistroDelRelojTests(PostgresFixture fixture)
{
    [Fact]
    [Trait("Story", "1.2")]
    [Trait("AC", "5")]
    public async Task TimeProvider_ContenedorDeLaWeb_EsUnSoloRelojHospital()
    {
        await using var fabrica = new HospitalWebApplicationFactory(fixture);

        var primera = fabrica.Services.GetRequiredService<TimeProvider>();
        var segunda = fabrica.Services.GetRequiredService<TimeProvider>();

        Assert.IsType<RelojHospital>(primera);
        Assert.Same(primera, segunda);
    }
}
