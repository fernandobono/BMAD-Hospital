using System.Text.Json;
using HospitalTurnos.Dominio.Excepciones;
using HospitalTurnos.Web.Errores;
using HospitalTurnos.Web.Tests.Ayudas;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HospitalTurnos.Web.Tests.Errores;

public class ManejadorGlobalDeErroresTests
{
    private readonly RegistroEnMemoria<ManejadorGlobalDeErrores> _registro = new();

    private ManejadorGlobalDeErrores CrearManejador()
    {
        var problemas = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider()
            .GetRequiredService<IProblemDetailsService>();
        return new ManejadorGlobalDeErrores(problemas, _registro);
    }

    private static DefaultHttpContext CrearPedidoJson()
    {
        var http = new DefaultHttpContext();
        http.Response.Body = new MemoryStream();
        http.Request.Headers.Accept = "application/json";
        return http;
    }

    // System.Text.Json escapa la "ó": se compara el JSON parseado, nunca el texto crudo.
    private static async Task<(JsonElement Json, string Crudo)> LeerCuerpo(HttpContext http)
    {
        http.Response.Body.Position = 0;
        var crudo = await new StreamReader(http.Response.Body).ReadToEndAsync();
        return (JsonDocument.Parse(crudo).RootElement, crudo);
    }

    public static TheoryData<Exception, int> ExcepcionesDelDominio => new()
    {
        { new ConflictoException("Ese horario ya está ocupado."), StatusCodes.Status409Conflict },
        { new ReglaDeNegocioException("No hay turnos ese día."), StatusCodes.Status400BadRequest },
        { new NoEncontradoException("No encontramos ese paciente."), StatusCodes.Status404NotFound },
        { new AccesoDenegadoException("No tenés permiso para ver esta agenda."), StatusCodes.Status403Forbidden },
    };

    [Theory]
    [Trait("Story", "1.2")]
    [Trait("AC", "7")]
    [Trait("AC", "8")]
    [MemberData(nameof(ExcepcionesDelDominio))]
    public async Task TryHandleAsync_ExcepcionDelDominio_DevuelveCodigoYMensajeDeLaRegla(Exception excepcion, int codigo)
    {
        var http = CrearPedidoJson();

        var manejada = await CrearManejador().TryHandleAsync(http, excepcion, CancellationToken.None);

        Assert.True(manejada);
        Assert.Equal(codigo, http.Response.StatusCode);
        var (json, _) = await LeerCuerpo(http);
        Assert.Equal(excepcion.Message, json.GetProperty("title").GetString());
        Assert.DoesNotContain(_registro.Entradas, e => e.Nivel == LogLevel.Error);
        Assert.Contains(_registro.Entradas, e => e.Nivel == LogLevel.Information);
    }

    [Fact]
    [Trait("Story", "1.2")]
    [Trait("AC", "9")]
    public async Task TryHandleAsync_ExcepcionInesperada_Devuelve500GenericoYRegistraDetalle()
    {
        var http = CrearPedidoJson();
        var excepcion = new InvalidOperationException("detalle secreto");

        var manejada = await CrearManejador().TryHandleAsync(http, excepcion, CancellationToken.None);

        Assert.True(manejada);
        Assert.Equal(StatusCodes.Status500InternalServerError, http.Response.StatusCode);
        var (json, crudo) = await LeerCuerpo(http);
        Assert.Equal(ManejadorGlobalDeErrores.MensajeGenerico, json.GetProperty("title").GetString());
        Assert.False(json.TryGetProperty("detail", out _), "El cuerpo no debe traer 'detail'");
        Assert.DoesNotContain("detalle secreto", crudo);
        Assert.DoesNotContain("InvalidOperationException", crudo);
        var error = Assert.Single(_registro.Entradas, e => e.Nivel == LogLevel.Error);
        Assert.Same(excepcion, error.Excepcion);
    }
}
