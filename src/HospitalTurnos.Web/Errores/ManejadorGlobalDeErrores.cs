using HospitalTurnos.Dominio.Excepciones;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HospitalTurnos.Web.Errores;

/// <summary>
/// Traduce cualquier excepción a una respuesta sin detalles técnicos (NFR-09). Las del dominio son
/// esperables y su mensaje ya está escrito para el usuario; lo inesperado recibe un mensaje genérico
/// y el detalle completo va sólo al registro técnico. Es el ÚNICO que registra la excepción.
/// </summary>
public sealed class ManejadorGlobalDeErrores(
    IProblemDetailsService problemas, ILogger<ManejadorGlobalDeErrores> registro) : IExceptionHandler
{
    public const string MensajeGenerico = "Algo falló. Probá de nuevo en unos minutos.";

    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception error, CancellationToken ct)
    {
        // El orden importa: ConflictoException ES una ReglaDeNegocioException.
        var estado = error switch
        {
            ConflictoException => StatusCodes.Status409Conflict,
            ReglaDeNegocioException => StatusCodes.Status400BadRequest,
            NoEncontradoException => StatusCodes.Status404NotFound,
            AccesoDenegadoException => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };
        var esperable = estado != StatusCodes.Status500InternalServerError;

        if (esperable)
            registro.LogInformation("Regla de negocio: {Mensaje}", error.Message);
        else
            registro.LogError(error, "Error no previsto en {Ruta}", http.Request.Path);

        // Un navegador pide text/html explícito: que lo atienda la página /Error.
        // (Un navegador también manda */*, y el escritor de ProblemDetails lo aceptaría como JSON.)
        if (http.Request.GetTypedHeaders().Accept.Any(a => a.MediaType == "text/html"))
            return false;

        http.Response.StatusCode = estado;
        return await problemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = null, // nunca pasar la excepción: algunos escritores la vuelcan en Development
            ProblemDetails = new ProblemDetails { Status = estado, Title = esperable ? error.Message : MensajeGenerico }
        });
    }
}
