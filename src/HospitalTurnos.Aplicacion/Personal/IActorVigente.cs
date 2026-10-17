using HospitalTurnos.Aplicacion.Auditoria;

namespace HospitalTurnos.Aplicacion.Personal;

/// <summary>
/// El permiso por rol, confirmado en la base en cada cambio (decisión 16 del docente). Lo crea la story 2.2
/// para el administrador (lo reusan con la misma firma la 2.3, la 3.1 y la 3.2); la 4.2 suma el método general,
/// que reusa la 5.1 para la recepción.
/// </summary>
public interface IActorVigente
{
    /// <summary>
    /// Confirma en la BASE que el actor existe, está activo y su ÚNICO rol es <paramref name="rol"/>. Si no,
    /// AccesoDenegadoException(mensajeSiNoLoEs). Devuelve el actor con el nombre de la base, ese rol y canal Personal.
    /// </summary>
    Task<ActorAuditoria> ExigirActivoConRolAsync(ActorAuditoria actor, string rol, string mensajeSiNoLoEs, CancellationToken ct);

    /// <summary>
    /// Confirma en la BASE (no en la sesión, que puede atrasar 5 minutos) que el actor es un usuario
    /// existente, activo y con el único rol Administrador. Si no, AccesoDenegadoException(mensajeSiNoLoEs).
    /// Devuelve el actor con Quien = NombreCompleto leído de la base, para la auditoría.
    /// </summary>
    Task<ActorAuditoria> ExigirAdministradorActivoAsync(ActorAuditoria actor, string mensajeSiNoLoEs, CancellationToken ct);
}
