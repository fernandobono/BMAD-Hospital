using System.Globalization;
using HospitalTurnos.Dominio.Agendas;
using HospitalTurnos.Dominio.Tests.Ayudas;

namespace HospitalTurnos.Dominio.Tests.Agendas;

public class EstadoDeAgendaTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 16);

    // Aprobada desde el 12/10 (rige hoy): se envía y aprueba el 01/10, con 7 días o más de anticipación.
    private static AgendaMedico QueRigeHoy() =>
        AgendaBuilder.UnaAgenda().Desde(new DateOnly(2026, 10, 12)).Aprobada(RelojDePrueba.En(2026, 10, 1));

    private static AgendaMedico AprobadaDesde(int mes, int dia) =>
        AgendaBuilder.UnaAgenda().Desde(new DateOnly(2026, mes, dia)).Aprobada();

    [Theory]
    [Trait("Story", "4.1")]
    [Trait("AC", "22")]
    [InlineData("a", EstadoAgendaMedico.SinAgenda, null)]
    [InlineData("b", EstadoAgendaMedico.SinAgenda, null)]
    [InlineData("c", EstadoAgendaMedico.SinAgenda, null)]
    [InlineData("d", EstadoAgendaMedico.Vigente, null)]
    [InlineData("e", EstadoAgendaMedico.SinAgenda, "02/11/2026")]
    [InlineData("f", EstadoAgendaMedico.PropuestaPendiente, null)]
    [InlineData("g", EstadoAgendaMedico.PropuestaPendiente, null)]
    [InlineData("h", EstadoAgendaMedico.SinAgenda, "02/11/2026")]
    public void Calcular_SegunLasAgendasDelMedico_DevuelveSuSituacion(string caso, EstadoAgendaMedico estado, string? rigeDesde)
    {
        AgendaMedico[] agendas = caso switch
        {
            "a" => [],
            "b" => [AgendaBuilder.UnaAgenda().Borrador()],
            "c" => [AgendaBuilder.UnaAgenda().Rechazada()],
            "d" => [QueRigeHoy()],
            "e" => [AprobadaDesde(11, 2)],
            "f" => [QueRigeHoy(), AgendaBuilder.UnaAgenda().Enviada()],
            "g" => [AgendaBuilder.UnaAgenda().Enviada()],
            "h" => [AprobadaDesde(11, 16), AprobadaDesde(11, 2)],
            _ => throw new ArgumentOutOfRangeException(nameof(caso)),
        };

        var situacion = EstadoDeAgenda.Calcular(agendas, Hoy);

        Assert.Equal(estado, situacion.Estado);
        Assert.Equal(rigeDesde, situacion.RigeDesde?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
    }
}
