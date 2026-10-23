using System.Security.Claims;
using HospitalTurnos.Aplicacion.Personal;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HospitalTurnos.Web.Personal;

/// <summary>
/// En una página interactiva la cookie no se vuelve a leer: el circuito vive lo que dure la pestaña. Este
/// proveedor repite ahí lo que ValidadorSesionPersonal hace en cada pedido HTTP (story 2.1, AC 25): cada
/// IntervaloRevalidacion pregunta a la base si la sesión sigue valiendo. Si no, el estado pasa a anónimo y la
/// página va al ingreso (story 3.3, AC 5). Mismo patrón que la plantilla oficial de Blazor con Identity.
/// </summary>
public class RevalidacionDeCircuito(
    ILoggerFactory registros, IServiceScopeFactory alcances, IOptions<IdentityOptions> opcionesIdentity)
    : RevalidatingServerAuthenticationStateProvider(registros)
{
    protected override TimeSpan RevalidationInterval => SesionPersonal.IntervaloRevalidacion;

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState estado, CancellationToken ct)
    {
        var usuario = estado.User;
        var id = usuario.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id is null)
            return false;
        var sello = usuario.FindFirstValue(opcionesIdentity.Value.ClaimsIdentity.SecurityStampClaimType);
        // El proveedor dura lo que el circuito: cada revisión pide el caso de uso en un alcance nuevo, así el
        // contexto de la base no queda vivo entre revisiones.
        await using var alcance = alcances.CreateAsyncScope();
        return await alcance.ServiceProvider.GetRequiredService<IIngresoPersonal>().SesionVigenteAsync(id, sello, ct);
    }
}
