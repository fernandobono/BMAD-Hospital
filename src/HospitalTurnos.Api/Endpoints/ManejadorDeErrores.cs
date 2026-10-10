using HospitalTurnos.Api.Dominio;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HospitalTurnos.Api.Endpoints;

/// <summary>Traduce las excepciones del dominio a respuestas HTTP con un mensaje legible.</summary>
public class ManejadorDeErrores(IProblemDetailsService problemas) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception error, CancellationToken ct)
    {
        var estado = error switch
        {
            NoEncontradoException => StatusCodes.Status404NotFound,
            ConflictoException => StatusCodes.Status409Conflict,
            ReglaDeNegocioException => StatusCodes.Status400BadRequest,
            _ => 0
        };
        if (estado == 0) return false; // lo inesperado sigue al manejador por defecto, sin exponer detalles

        http.Response.StatusCode = estado;
        return await problemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails = new ProblemDetails { Status = estado, Title = error.Message }
        });
    }
}
