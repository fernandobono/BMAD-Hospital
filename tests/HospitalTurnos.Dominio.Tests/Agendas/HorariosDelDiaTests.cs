using HospitalTurnos.Dominio.Tests.Ayudas;

namespace HospitalTurnos.Dominio.Tests.Agendas;

public class HorariosDelDiaTests
{
    private static readonly DateOnly Lunes9 = new(2026, 11, 9);
    private static readonly DateOnly Martes10 = new(2026, 11, 10);

    private static string[] Horas(IEnumerable<DateTime> horarios) => horarios.Select(h => h.ToString("HH:mm")).ToArray();

    [Fact]
    [Trait("Story", "4.1")]
    [Trait("AC", "14")]
    public void HorariosDelDia_FranjaMultiploDeLaDuracion_DevuelveCadaInicio()
    {
        var agenda = AgendaBuilder.UnaAgenda().DeMinutos(20).ConFranja(DayOfWeek.Monday, "08:00", "12:00").Borrador();

        var horarios = agenda.HorariosDelDia(Lunes9);

        Assert.Equal(12, horarios.Count);
        Assert.Equal(new DateTime(2026, 11, 9, 8, 0, 0), horarios[0]);
        Assert.Equal(new DateTime(2026, 11, 9, 8, 20, 0), horarios[1]);
        Assert.Equal(new DateTime(2026, 11, 9, 11, 40, 0), horarios[^1]);
        Assert.All(horarios, h => Assert.Equal(DateTimeKind.Unspecified, h.Kind));
    }

    [Fact]
    [Trait("Story", "4.1")]
    [Trait("AC", "15")]
    public void HorariosDelDia_FranjaNoMultiplo_NoOfreceElTurnoQueNoEntra()
    {
        var agenda = AgendaBuilder.UnaAgenda().DeMinutos(20).ConFranja(DayOfWeek.Monday, "08:00", "10:50").Borrador();

        var horarios = agenda.HorariosDelDia(Lunes9);

        Assert.Equal(8, horarios.Count);
        Assert.Equal("10:20", horarios[^1].ToString("HH:mm"));
    }

    [Fact]
    [Trait("Story", "4.1")]
    [Trait("AC", "16")]
    public void HorariosDelDia_FranjaHastaMedianoche_ElUltimoTerminaALas24()
    {
        var agenda = AgendaBuilder.UnaAgenda().DeMinutos(30).ConFranja(DayOfWeek.Monday, "20:00", "00:00").Borrador();

        var lunes = agenda.HorariosDelDia(Lunes9);
        var martes = agenda.HorariosDelDia(Martes10);

        Assert.Equal(8, lunes.Count);
        Assert.Equal(new DateTime(2026, 11, 9, 23, 30, 0), lunes[^1]);
        Assert.Empty(martes);
    }

    [Fact]
    [Trait("Story", "4.1")]
    [Trait("AC", "17")]
    public void HorariosDelDia_DiaSinFranja_DevuelveVacio()
    {
        var agenda = AgendaBuilder.UnaAgenda().ConFranja(DayOfWeek.Monday, "08:00", "12:00").Borrador();

        Assert.Empty(agenda.HorariosDelDia(Martes10));
    }

    [Fact]
    [Trait("Story", "4.1")]
    [Trait("AC", "14")]
    public void HorariosDelDia_FranjasCargadasDesordenadas_DevuelveEnOrden()
    {
        var agenda = AgendaBuilder.UnaAgenda().DeMinutos(30)
            .ConFranja(DayOfWeek.Monday, "14:00", "15:00").ConFranja(DayOfWeek.Monday, "08:00", "09:00").Borrador();

        Assert.Equal(["08:00", "08:30", "14:00", "14:30"], Horas(agenda.HorariosDelDia(Lunes9)));
    }

    [Theory]
    [Trait("Story", "4.1")]
    [Trait("AC", "18")]
    [InlineData(9, 8, 20, true)]
    [InlineData(9, 8, 10, false)]
    [InlineData(9, 11, 40, true)]
    [InlineData(9, 12, 0, false)]
    [InlineData(10, 8, 20, false)]
    public void AtiendeEn_SegunLaHora_DevuelveSiEsUnInicioDeTurno(int dia, int hora, int minuto, bool esperado)
    {
        var agenda = AgendaBuilder.UnaAgenda().DeMinutos(20).ConFranja(DayOfWeek.Monday, "08:00", "12:00").Borrador();

        Assert.Equal(esperado, agenda.AtiendeEn(new DateTime(2026, 11, dia, hora, minuto, 0)));
    }

    [Fact]
    [Trait("Story", "4.1")]
    [Trait("AC", "19")]
    public void TurnosPorSemana_VariasFranjas_SumaLosQueEntranCompletos()
    {
        var agenda = AgendaBuilder.UnaAgenda().DeMinutos(20)
            .ConFranja(DayOfWeek.Monday, "08:00", "12:00").ConFranja(DayOfWeek.Wednesday, "08:00", "10:50").Borrador();

        Assert.Equal(20, agenda.TurnosPorSemana());
    }
}
