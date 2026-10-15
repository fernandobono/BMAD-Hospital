using System.Net;
using HospitalTurnos.Web.Tests.Ayudas;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalTurnos.Web.Tests.Arranque;

[Collection("Postgres")]
public class HttpsDetrasDelProxyTests(PostgresFixture fixture)
{
    // HSTS no se envía a "localhost" (está en HstsOptions.ExcludedHosts): se usa el host real.
    private const string Host = "turnos.lasacacias.example";

    public static TheoryData<string, string, bool, string?, HttpStatusCode, bool> Casos => new()
    {
        // entorno, IP remota, X-Forwarded-Proto: https, red confiable configurada, código, HSTS
        { "Testing", "127.0.0.1", true, null, HttpStatusCode.OK, true },
        { "Testing", "127.0.0.1", false, null, HttpStatusCode.TemporaryRedirect, false },
        { "Testing", "203.0.113.5", true, null, HttpStatusCode.TemporaryRedirect, false },
        { "Testing", "203.0.113.5", true, "203.0.113.0/24", HttpStatusCode.OK, true },
        { "Development", "127.0.0.1", false, null, HttpStatusCode.OK, false },
    };

    [Theory]
    [Trait("Story", "1.1")]
    [Trait("AC", "17")]
    [MemberData(nameof(Casos))]
    public async Task Salud_DetrasDelProxy_RespetaHttpsSoloDeRedesConfiables(
        string entorno, string ipRemota, bool conHttpsReenviado, string? redConfiable,
        HttpStatusCode codigoEsperado, bool conHsts)
    {
        await using var fabrica = new HospitalWebApplicationFactory(fixture);
        await using var app = fabrica.WithWebHostBuilder(b =>
        {
            b.UseEnvironment(entorno);
            b.UseSetting("https_port", "443");
            if (redConfiable is not null)
                b.ConfigureAppConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ProxyInverso:RedesConfiables:0"] = redConfiable,
                }));
            b.ConfigureTestServices(s => s.AddTransient<IStartupFilter>(_ => new FiltroIpRemota(IPAddress.Parse(ipRemota))));
        });
        var cliente = app.CreateClient(new() { AllowAutoRedirect = false });
        var pedido = new HttpRequestMessage(HttpMethod.Get, "/salud");
        pedido.Headers.Host = Host;
        if (conHttpsReenviado)
            pedido.Headers.Add("X-Forwarded-Proto", "https");

        var respuesta = await cliente.SendAsync(pedido);

        Assert.Equal(codigoEsperado, respuesta.StatusCode);
        Assert.Equal(conHsts, respuesta.Headers.Contains("Strict-Transport-Security"));
        if (codigoEsperado == HttpStatusCode.TemporaryRedirect)
            Assert.Equal($"https://{Host}/salud", respuesta.Headers.Location?.ToString());
    }
}
