using HospitalTurnos.Aplicacion.Auditoria;
using Microsoft.AspNetCore.Components.Authorization;

namespace HospitalTurnos.Web.Personal;

/// <summary>
/// Quién actúa en una página interactiva (story 3.3, AC 2): ahí no hay HttpContext, así que el usuario sale del
/// AuthenticationState en cascada. Los mismos claims que la cookie: la auditoría queda igual que en SSR.
/// </summary>
public static class ActorDelCircuito
{
    public static async Task<ActorAuditoria> DesdeAsync(Task<AuthenticationState>? estado)
    {
        if (estado is null)
            throw new InvalidOperationException("La página no recibió el estado de autenticación en cascada.");
        return ActorDeSesion.Desde((await estado).User);
    }
}
