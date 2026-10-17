namespace HospitalTurnos.Dominio.Agendas;

/// <summary>
/// Lo que A1 muestra de la agenda de UN médico (AC 22): la regla de la decisión 8 escrita sobre QueRigeEn, así
/// la regla de vigencia sigue estando una sola vez. Devuelve la SituacionDeAgenda de la 3.2 (AC 23 de la 3.2).
/// </summary>
public static class EstadoDeAgenda
{
    // La pendiente gana: pide una acción del administrador. Una aprobada que todavía no rige no ofrece turnos
    // hoy (RU-07), pero se muestra con su fecha: "Sin agenda (rige desde 01/12)". Borradores y rechazadas no cuentan.
    public static SituacionDeAgenda Calcular(IEnumerable<AgendaMedico> agendasDelMedico, DateOnly hoy)
    {
        var agendas = agendasDelMedico.ToList();
        if (agendas.Any(a => a.Estado == EstadoPropuesta.Enviada))
            return new(EstadoAgendaMedico.PropuestaPendiente);
        var proxima = agendas.Where(a => a.Estado == EstadoPropuesta.Aprobada && a.VigenteDesde > hoy)
                             .Select(a => (DateOnly?)a.VigenteDesde)
                             .Min();                         // null si no hay ninguna
        // Story 3.3 (AC 61): con una vigente, la próxima aprobada es el día en que "Cambia el" horario.
        if (AgendaMedico.QueRigeEn(agendas, hoy) is not null)
            return new(EstadoAgendaMedico.Vigente, CambiaEl: proxima);
        return new(EstadoAgendaMedico.SinAgenda, proxima);
    }
}
