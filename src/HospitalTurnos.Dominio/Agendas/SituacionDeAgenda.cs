namespace HospitalTurnos.Dominio.Agendas;

/// <summary>
/// Lo que A1 muestra de la agenda de un médico (story 3.2, contrato con la 4.1 en su AC 23). RigeDesde sólo
/// con SinAgenda, cuando hay una aprobada que todavía no rige (decisión 8: "Sin agenda (rige desde 01/12)").
/// </summary>
public sealed record SituacionDeAgenda(EstadoAgendaMedico Estado, DateOnly? RigeDesde = null)
{
    public static readonly SituacionDeAgenda SinAgenda = new(EstadoAgendaMedico.SinAgenda);
}
