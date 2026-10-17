using System.Globalization;
using HospitalTurnos.Dominio.Agendas;
using HospitalTurnos.Dominio.Tests.Ayudas;

namespace HospitalTurnos.Dominio.Tests.Agendas;

// Story 3.3, AC 61 (decisión del docente, sesión 17): con una agenda vigente y otra aprobada a futuro, A1 muestra
// "Vigente" y debajo "Cambia el dd/mm/aaaa". La fecha la da el mismo cálculo; el estado no cambia en ningún caso.
public class CambioDeAgendaTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 16);

    private static AgendaMedico QueRigeHoy() =>
        AgendaBuilder.UnaAgenda().Desde(new DateOnly(2026, 10, 12)).Aprobada(RelojDePrueba.En(2026, 10, 1));

    private static AgendaMedico AprobadaDesde(int mes, int dia) =>
        AgendaBuilder.UnaAgenda().Desde(new DateOnly(2026, mes, dia)).Aprobada();

    [Theory]
    [Trait("Story", "3.3")]
    [Trait("AC", "61")]
    [InlineData("vigente-sola", EstadoAgendaMedico.Vigente, null, null)]
    [InlineData("vigente-con-dos-futuras", EstadoAgendaMedico.Vigente, null, "02/11/2026")]
    [InlineData("solo-futuras", EstadoAgendaMedico.SinAgenda, "02/11/2026", null)]
    [InlineData("pendiente-con-futura", EstadoAgendaMedico.PropuestaPendiente, null, null)]
    [InlineData("ninguna", EstadoAgendaMedico.SinAgenda, null, null)]
    public void Calcular_VigenteConOtraAprobadaFutura_DevuelveCambiaEl(string caso, EstadoAgendaMedico estado,
        string? rigeDesde, string? cambiaEl)
    {
        AgendaMedico[] agendas = caso switch
        {
            "vigente-sola" => [QueRigeHoy()],
            "vigente-con-dos-futuras" => [QueRigeHoy(), AprobadaDesde(11, 16), AprobadaDesde(11, 2)],
            "solo-futuras" => [AprobadaDesde(11, 16), AprobadaDesde(11, 2)],
            "pendiente-con-futura" => [QueRigeHoy(), AprobadaDesde(11, 2), AgendaBuilder.UnaAgenda().Enviada()],
            "ninguna" => [],
            _ => throw new ArgumentOutOfRangeException(nameof(caso)),
        };

        var situacion = EstadoDeAgenda.Calcular(agendas, Hoy);

        Assert.Equal(estado, situacion.Estado);
        Assert.Equal(rigeDesde, situacion.RigeDesde?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
        Assert.Equal(cambiaEl, situacion.CambiaEl?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
    }
}
