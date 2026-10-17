using HospitalTurnos.Dominio.Agendas;

namespace HospitalTurnos.Aplicacion.Agendas;

/// <summary>
/// Lectura de agendas (story 4.1, AC 29). La usan: el listado y la ficha de médicos (3.2, estado de agenda en
/// A1), y después la propuesta (4.2), la aprobación (4.3) y la disponibilidad (5.2). La regla de qué agenda
/// rige está una sola vez, en el dominio (AgendaMedico.QueRigeEn): esta consulta sólo trae las candidatas.
/// </summary>
public interface IConsultaAgendas
{
    /// <summary>La agenda que rige en la fecha, con sus franjas y sin seguimiento; null si no rige ninguna.</summary>
    Task<AgendaMedico?> AgendaQueRigeAsync(long medicoId, DateOnly fecha, CancellationToken ct);

    /// <summary>Una situación por CADA id pedido (sin agendas: SinAgenda), con "hoy" del hospital y una sola consulta.</summary>
    Task<IReadOnlyDictionary<long, SituacionDeAgenda>> EstadosDeAgendaAsync(IReadOnlyCollection<long> medicoIds,
        CancellationToken ct);
}
