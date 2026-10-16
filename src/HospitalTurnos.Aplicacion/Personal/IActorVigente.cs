using HospitalTurnos.Aplicacion.Auditoria;

namespace HospitalTurnos.Aplicacion.Personal;

/// <summary>
/// El permiso del administrador, confirmado en la base en cada cambio (decisión 16 del docente). Lo crea
/// la story 2.2 y lo reusan con la misma firma la 3.1 y la 3.2.
/// </summary>
public interface IActorVigente
{
    /// <summary>
    /// Confirma en la BASE (no en la sesión, que puede atrasar 5 minutos) que el actor es un usuario
    /// existente, activo y con el único rol Administrador. Si no, AccesoDenegadoException(mensajeSiNoLoEs).
    /// Devuelve el actor con Quien = NombreCompleto leído de la base, para la auditoría.
    /// </summary>
    Task<ActorAuditoria> ExigirAdministradorActivoAsync(ActorAuditoria actor, string mensajeSiNoLoEs, CancellationToken ct);
}
