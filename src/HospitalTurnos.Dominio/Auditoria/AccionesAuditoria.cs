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
}
