using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Dominio.Agendas;

namespace HospitalTurnos.Aplicacion.Agendas;

/// <summary>
/// M2 (story 4.2): la propuesta de agenda del médico de la SESIÓN (Arq. §7.7). Nunca recibe un id de médico ni
/// de agenda: los busca por el usuario del actor. Leer no consulta el rol en la base; cada escritura, sí (AC 21).
/// </summary>
public interface IPropuestaAgenda
{
    Task<VistaPropuesta> ObtenerAsync(ActorAuditoria actor, CancellationToken ct);

    /// <summary>Crea la propuesta o le aplica sólo las diferencias. No se audita (sólo el envío).</summary>
    Task<ResultadoGuardado> GuardarBorradorAsync(DatosAgenda datos, ActorAuditoria actor, CancellationToken ct);

    /// <summary>Aplica los datos y envía, en un paso (decisión 6). Audita con PropuestaAgendaEnviada.</summary>
    Task EnviarAsync(DatosAgenda datos, ActorAuditoria actor, CancellationToken ct);
}

public enum ResultadoGuardado { Creada, Guardada, SinCambios }

/// <summary>
/// Lo que M2 muestra. Las agendas, con sus franjas y sin seguimiento. La 4.3 agrega string? RechazadaPor = null
/// AL FINAL (contrato, AC 24; decisión 7).
/// </summary>
public sealed record VistaPropuesta(
    bool SinVinculo,
    AgendaMedico? Vigente,          // la que rige hoy (QueRigeEn)
    AgendaMedico? ProximaAprobada,  // la aprobada más próxima que todavía no rige
    AgendaMedico? ParaCopiar,       // la última aprobada (decisión 4)
    AgendaMedico? EnCurso,          // Enviada, o si no la editable (Borrador o Rechazada)
    DateOnly MinimoVigenteDesde);   // hoy + 7 (S-31)
