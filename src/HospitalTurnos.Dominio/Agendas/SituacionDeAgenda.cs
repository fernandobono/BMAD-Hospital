namespace HospitalTurnos.Dominio.Agendas;

/// <summary>
/// Lo que A1 muestra de la agenda de un médico (story 3.2, contrato con la 4.1 en su AC 23). RigeDesde sólo
/// con SinAgenda, cuando hay una aprobada que todavía no rige (A1 lo muestra como "Programada · desde …").
/// CambiaEl sólo con Vigente, cuando ya hay otra aprobada que la reemplaza más adelante (story 3.3, AC 61).
/// </summary>
public sealed record SituacionDeAgenda(EstadoAgendaMedico Estado, DateOnly? RigeDesde = null, DateOnly? CambiaEl = null)
{
    public static readonly SituacionDeAgenda SinAgenda = new(EstadoAgendaMedico.SinAgenda);
}
