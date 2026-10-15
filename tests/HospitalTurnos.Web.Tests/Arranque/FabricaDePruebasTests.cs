using HospitalTurnos.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace HospitalTurnos.Web.Tests.Arranque;

[Collection("Postgres")]
public class FabricaDePruebasTests(PostgresFixture fixture)
{
    // Vigila la revisión I-15: en Development se cargan appsettings.Development.json y user-secrets,
    // y aun así la cadena tiene que ser la del contenedor de la prueba.
    [Fact]
    [Trait("Story", "1.1")]
    [Trait("AC", "18")]
    public async Task Fabrica_EntornoDevelopment_UsaLasCadenasDeLaFixture()
    {
        await using var fabrica = new HospitalWebApplicationFactory(fixture);
        await using var app = fabrica.WithWebHostBuilder(b => b.UseEnvironment("Development"));
        using var alcance = app.Services.CreateScope();

        var cadena = alcance.ServiceProvider.GetRequiredService<HospitalDbContext>().Database.GetConnectionString();

        var real = new NpgsqlConnectionStringBuilder(cadena);
        var esperada = new NpgsqlConnectionStringBuilder(fixture.CadenaApp);
        Assert.Equal(esperada.Host, real.Host);
        Assert.Equal(esperada.Port, real.Port);
        Assert.Equal("turnos_app", real.Username);
    }
}
