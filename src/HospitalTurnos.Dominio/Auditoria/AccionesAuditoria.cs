namespace HospitalTurnos.Dominio.Auditoria;

/// <summary>Las acciones que se auditan (FR-33). Un solo lugar para el texto que queda en cada registro.</summary>
public static class AccionesAuditoria
{
    public const string TurnoReservado = "turno.reservado";
    public const string TurnoCancelado = "turno.cancelado";
    public const string AgendaBloqueada = "agenda.bloqueada";
    public const string BloqueoLevantado = "bloqueo.levantado";
    public const string PacienteMarcadoAvisado = "paciente.marcado-avisado";
    public const string PropuestaAgendaEnviada = "propuesta-agenda.enviada";
    public const string PropuestaAgendaAprobada = "propuesta-agenda.aprobada";
    public const string PropuestaAgendaRechazada = "propuesta-agenda.rechazada";
    public const string AgendaCorregida = "agenda.corregida";

    // Story 2.1: el ingreso del personal. Como el bloqueo no se muestra en pantalla, la auditoría (A5)
    // es la única forma de que el administrador se entere de bloqueos e intentos.
    public const string IngresoPersonal = "ingreso-personal";
    public const string IngresoPersonalFallido = "ingreso-personal-fallido";
    public const string IngresoPersonalBloqueado = "ingreso-personal-bloqueado";

    // Story 2.2: la administración de usuarios (A4) y el cambio de contraseña. Estilo <entidad>.<acción>,
    // como las de turnos y agenda (contrato técnico §Auditoría).
    public const string UsuarioPersonalCreado = "usuario-personal.creado";
    public const string UsuarioPersonalDesactivado = "usuario-personal.desactivado";
    public const string UsuarioPersonalReactivado = "usuario-personal.reactivado";
    public const string UsuarioPersonalDesbloqueado = "usuario-personal.desbloqueado";
    public const string ContrasenaPersonalCambiada = "usuario-personal.contrasena-cambiada";

    // Story 3.1: el catálogo de especialidades (A2, canal Personal).
    public const string EspecialidadCreada = "especialidad.creada";
    public const string EspecialidadDesactivada = "especialidad.desactivada";
    public const string EspecialidadReactivada = "especialidad.reactivada";

    // Story 3.2: los médicos (A1, canal Personal).
    public const string MedicoCreado = "medico.creado";
    public const string MedicoEditado = "medico.editado";
}
