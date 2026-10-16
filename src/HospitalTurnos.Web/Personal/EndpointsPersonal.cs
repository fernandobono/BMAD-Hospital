using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;

namespace HospitalTurnos.Web.Personal;

public static class EndpointsPersonal
{
    public static IEndpointRouteBuilder MapEndpointsPersonal(this IEndpointRouteBuilder app)
    {
        // POST con token antifalsificación (AC 15). Un GET no podría cerrar la sesión: cualquier página
        // ajena con un <img> lo dispararía. El token se valida a mano y no con un parámetro [FromForm]:
        // con [FromForm] y un token inválido, leer el formulario lanza y el pedido termina en 500.
        app.MapPost(RutasPersonal.Salir, async (HttpContext http, IAntiforgery antiforgery) =>
        {
            if (!await antiforgery.IsRequestValidAsync(http))
                // Con cuerpo, para que la página de estado de 1.2 no lo reemplace re-ejecutando la ruta.
                return Results.Text("Falta el token del formulario. Volvé a la página e intentá de nuevo.",
                    statusCode: StatusCodes.Status400BadRequest);
            await http.SignOutAsync(SesionPersonal.Esquema);
            return Results.Redirect(RutasPersonal.Ingreso);
        }).DisableAntiforgery();   // la validación la hace el propio endpoint, arriba
        return app;
    }
}
