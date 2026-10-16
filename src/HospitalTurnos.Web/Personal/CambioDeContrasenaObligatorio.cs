using HospitalTurnos.Aplicacion.Personal;

namespace HospitalTurnos.Web.Personal;

/// <summary>
/// Quien entró con la contraseña temporal no usa el sistema hasta elegir la suya (S-30, story 2.2 AC 20):
/// toda ruta del personal lo manda a la pantalla de cambio, salvo esa, salir, el ingreso y el acceso
/// denegado. Lo de afuera de /personal/ (archivos, /salud, /Error) no se toca.
/// </summary>
public static class CambioDeContrasenaObligatorio
{
    private static readonly HashSet<string> Permitidas = new(StringComparer.OrdinalIgnoreCase)
        { RutasPersonal.CambiarContrasena, RutasPersonal.Salir, RutasPersonal.Ingreso, RutasPersonal.AccesoDenegado };

    /// <summary>Entre UseAuthentication y UseAuthorization: necesita el usuario ya leído de la cookie.</summary>
    public static IApplicationBuilder UseCambioDeContrasenaObligatorio(this IApplicationBuilder app) =>
        app.Use(async (http, siguiente) =>
        {
            var ruta = http.Request.Path.Value ?? "";
            if (http.User.HasClaim(ClaimsPersonal.DebeCambiarContrasena, "si")
                && ruta.StartsWith(RutasPersonal.Prefijo, StringComparison.OrdinalIgnoreCase)
                && !Permitidas.Contains(ruta))
            {
                http.Response.Redirect(RutasPersonal.CambiarContrasena);
                return;
            }
            await siguiente(http);
        });
}
