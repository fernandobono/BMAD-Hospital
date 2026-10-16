using System.Security.Claims;
using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Personal;
using HospitalTurnos.Dominio.Auditoria;

namespace HospitalTurnos.Web.Personal;

/// <summary>
/// Quién actúa, según la cookie del personal (story 2.2; la 3.1 y la 3.2 lo reusan). El caso de uso lo
/// vuelve a confirmar en la base: la sesión puede atrasar hasta 5 minutos.
/// </summary>
public static class ActorDeSesion
{
    public static ActorAuditoria Desde(ClaimsPrincipal usuario) =>
        new(Requerido(usuario, ClaimTypes.NameIdentifier), Requerido(usuario, ClaimsPersonal.NombreCompleto),
            Requerido(usuario, ClaimTypes.Role), CanalAuditoria.Personal);

    // Con [Authorize] siempre están: si falta uno es un error de programación y va al 500.
    private static string Requerido(ClaimsPrincipal usuario, string tipo) =>
        usuario.FindFirstValue(tipo)
        ?? throw new InvalidOperationException($"La sesión del personal no trae el dato {tipo}.");
}
