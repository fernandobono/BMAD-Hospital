using System.Net;
using System.Text.Json;
using HospitalTurnos.Web.Errores;
using HospitalTurnos.Web.Tests.Ayudas;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HospitalTurnos.Web.Tests.Errores;

[Collection("Postgres")]
public class ErroresDeExtremoAExtremoTests(PostgresFixture fixture)
{
    private const string AcceptNavegador = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";

    private static WebApplicationFactory<Program> ConRutasQueFallan(
        HospitalWebApplicationFactory fabrica, string entorno, ProveedorDeRegistroEnMemoria? registro = null) =>
        fabrica.WithWebHostBuilder(b =>
        {
            b.UseEnvironment(entorno);
            b.ConfigureTestServices(s =>
            {
                s.AddTransient<IStartupFilter, FiltroRutaQueFalla>();
                if (registro is not null)
                    s.AddSingleton<ILoggerProvider>(registro);
            });
        });

    private static HttpRequestMessage Pedido(string ruta, string accept)
    {
        var pedido = new HttpRequestMessage(HttpMethod.Get, ruta);
        pedido.Headers.Accept.ParseAdd(accept);
        return pedido;
    }

    public static TheoryData<string, string> CasosDeLayout => new()
    {
        // Accept, entorno
        { "application/json", "Testing" },
        { AcceptNavegador, "Testing" },
        { AcceptNavegador, "Development" },
    };

    [Theory]
    [Trait("Story", "1.2")]
    [Trait("AC", "10")]
    [Trait("AC", "11")]
    [Trait("AC", "12")]
    [MemberData(nameof(CasosDeLayout))]
    public async Task Get_RutaQueFalla_MuestraMensajeGenericoSinDetalles(string accept, string entorno)
    {
        await using var fabrica = new HospitalWebApplicationFactory(fixture);
        await using var app = ConRutasQueFallan(fabrica, entorno);
        var cliente = app.CreateClient();

        var respuesta = await cliente.SendAsync(Pedido(FiltroRutaQueFalla.RutaInesperada, accept));

        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        if (accept == "application/json")
        {
            Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
            var json = JsonDocument.Parse(cuerpo).RootElement;
            Assert.Equal(ManejadorGlobalDeErrores.MensajeGenerico, json.GetProperty("title").GetString());
        }
        else
        {
            Assert.Equal("text/html", respuesta.Content.Headers.ContentType?.MediaType);
            // En HTML la "ó" puede salir literal o como &#xF3;: se decodifica antes de buscar.
            Assert.Contains(ManejadorGlobalDeErrores.MensajeGenerico, WebUtility.HtmlDecode(cuerpo));
        }
        Assert.DoesNotContain("detalle secreto", cuerpo);
        Assert.DoesNotContain("Request ID", cuerpo);
        Assert.DoesNotContain("Development Mode", cuerpo);
    }

    public static TheoryData<string, string> CasosDeRegistro => new()
    {
        // ruta, Accept
        { FiltroRutaQueFalla.RutaRegla, "application/json" },
        { FiltroRutaQueFalla.RutaRegla, AcceptNavegador },
        { FiltroRutaQueFalla.RutaInesperada, "application/json" },
        { FiltroRutaQueFalla.RutaInesperada, AcceptNavegador },
    };

    [Theory]
    [Trait("Story", "1.2")]
    [Trait("AC", "11")]
    [Trait("AC", "14")]
    [MemberData(nameof(CasosDeRegistro))]
    public async Task Get_RutaQueFalla_RegistraLaExcepcionUnaSolaVez(string ruta, string accept)
    {
        var registro = new ProveedorDeRegistroEnMemoria();   // uno nuevo por caso: no cuenta entradas ajenas
        await using var fabrica = new HospitalWebApplicationFactory(fixture);
        await using var app = ConRutasQueFallan(fabrica, "Testing", registro);
        var cliente = app.CreateClient();

        var respuesta = await cliente.SendAsync(Pedido(ruta, accept));
        await respuesta.Content.ReadAsStringAsync();   // se lee el registro con la respuesta ya completa

        var categoria = typeof(ManejadorGlobalDeErrores).FullName;
        var entradas = registro.Entradas.ToList();
        var errores = entradas.Where(e => e.Nivel >= LogLevel.Error).ToList();
        if (ruta == FiltroRutaQueFalla.RutaRegla)
        {
            var regla = Assert.Single(entradas, e => e.Categoria == categoria);
            Assert.Equal(LogLevel.Information, regla.Nivel);
            Assert.Empty(errores);
        }
        else
        {
            var error = Assert.Single(errores);
            Assert.Equal(categoria, error.Categoria);
            Assert.IsType<InvalidOperationException>(error.Excepcion);
            Assert.Equal("detalle secreto", error.Excepcion!.Message);
        }
    }
}
