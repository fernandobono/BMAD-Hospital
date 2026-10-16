using System.Security.Claims;
using HospitalTurnos.Aplicacion.Personal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HospitalTurnos.Web.Personal;

/// <summary>
/// Revalida la sesión contra la base cada 5 minutos (AC 25): si el usuario se desactivó, se borró o
/// cambió su sello de seguridad (story 2.2), la sesión se corta. Es la misma idea que el
/// SecurityStampValidator de Identity, que acá no sirve: necesita SignInManager y el esquema
/// Identity.Application.
/// </summary>
public sealed class ValidadorSesionPersonal(IIngresoPersonal ingreso, IOptions<IdentityOptions> opcionesIdentity)
    : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext contexto)
    {
        var ahora = contexto.Options.TimeProvider!.GetUtcNow();
        var emitida = contexto.Properties.IssuedUtc;
        // Dentro del intervalo no se consulta la base: un pedido normal no paga la consulta.
        if (emitida is not null && ahora - emitida.Value < SesionPersonal.IntervaloRevalidacion)
            return;

        var id = contexto.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var sello = contexto.Principal?.FindFirstValue(opcionesIdentity.Value.ClaimsIdentity.SecurityStampClaimType);
        if (id is null || !await ingreso.SesionVigenteAsync(id, sello, contexto.HttpContext.RequestAborted))
        {
            contexto.RejectPrincipal();                                         // sigue como anónimo → 302 al ingreso
            await contexto.HttpContext.SignOutAsync(SesionPersonal.Esquema);    // y la cookie se vence en la respuesta
            return;
        }
        contexto.ShouldRenew = true;   // nueva fecha de emisión: la próxima revisión, en 5 minutos
    }
}
