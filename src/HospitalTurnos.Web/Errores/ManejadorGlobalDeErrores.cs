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

    /// <summary>
    /// El código HTTP de cada excepción. Un solo mapeo: lo usa este manejador y las páginas SSR que
    /// muestran el cartel de una regla en la propia pantalla (story 2.2; la 3.1 y la 3.2 lo reusan).
    /// </summary>
    public static int EstadoPara(Exception error) => error switch
    {
        // El orden importa: ConflictoException ES una ReglaDeNegocioException.
        ConflictoException => StatusCodes.Status409Conflict,
        ReglaDeNegocioException => StatusCodes.Status400BadRequest,
        NoEncontradoException => StatusCodes.Status404NotFound,
        AccesoDenegadoException => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError
    };

    /// <summary>Las del dominio: su mensaje ya está escrito para el usuario. Lo demás es un error.</summary>
    public static bool EsEsperable(Exception error) => EstadoPara(error) != StatusCodes.Status500InternalServerError;

    /// <summary>
    /// El código de la excepción en la respuesta de una página SSR que muestra el cartel de la regla (story
    /// 2.2; la 3.1 y la 3.2 lo reusan). El 404 se fija al empezar a enviar la respuesta: Blazor (.NET 10)
    /// no escribe el cuerpo de una página que terminó de dibujarse en 404 (lo deja para la página "no
    /// encontrada"), y el cartel no llegaría. Los demás se fijan en el acto.
    /// </summary>
    public static void FijarEstadoEnPagina(HttpContext http, Exception error)
    {
        var estado = EstadoPara(error);
        if (estado != StatusCodes.Status404NotFound)
        {
            http.Response.StatusCode = estado;
            return;
        }
        http.Response.OnStarting(() =>
        {
            http.Response.StatusCode = estado;
            return Task.CompletedTask;
        });
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception error, CancellationToken ct)
    {
        var estado = EstadoPara(error);
        var esperable = EsEsperable(error);

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
