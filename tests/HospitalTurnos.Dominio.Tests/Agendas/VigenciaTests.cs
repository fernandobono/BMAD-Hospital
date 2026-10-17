using HospitalTurnos.Dominio.Agendas;
using HospitalTurnos.Dominio.Tests.Ayudas;

namespace HospitalTurnos.Dominio.Tests.Agendas;

public class VigenciaTests
{
    [Theory]
    [Trait("Story", "4.1")]
    [Trait("AC", "20")]
    [Trait("AC", "21")]
    [InlineData(10, 26, null)]
    [InlineData(11, 1, null)]
    [InlineData(11, 2, "A")]
    [InlineData(11, 9, "A")]
    [InlineData(11, 15, "A")]
    [InlineData(11, 16, "B")]
    [InlineData(11, 30, "B")]
    public void QueRigeEn_VariasAgendas_EligeLaAprobadaMasRecienteNoPosterior(int mes, int dia, string? esperada)
    {
        var agendas = new Dictionary<string, AgendaMedico>
        {
            ["A"] = AgendaBuilder.UnaAgenda().Desde(new DateOnly(2026, 11, 2)).Aprobada(),
            ["B"] = AgendaBuilder.UnaAgenda().Desde(new DateOnly(2026, 11, 16)).Aprobada(),
            ["C"] = AgendaBuilder.UnaAgenda().Desde(new DateOnly(2026, 11, 9)).Enviada(),
            ["D"] = AgendaBuilder.UnaAgenda().Desde(new DateOnly(2026, 10, 26)).Rechazada(),
        };

        var rige = AgendaMedico.QueRigeEn(agendas.Values, new DateOnly(2026, mes, dia));

        Assert.Equal(esperada, agendas.SingleOrDefault(par => ReferenceEquals(par.Value, rige)).Key);
    }

    [Fact]
    [Trait("Story", "4.1")]
    [Trait("AC", "20")]
    public void QueRigeEn_MismaVigencia_GanaLaAprobadaDespues()
    {
        var desde = new DateOnly(2026, 11, 2);
        var a = AgendaBuilder.UnaAgenda().Desde(desde).Aprobada(RelojDePrueba.En(2026, 10, 20, 10));
        var b = AgendaBuilder.UnaAgenda().Desde(desde).Aprobada(RelojDePrueba.En(2026, 10, 20, 11));

        Assert.Same(b, AgendaMedico.QueRigeEn([b, a], desde));
        Assert.Same(b, AgendaMedico.QueRigeEn([a, b], desde));
    }

    [Fact]
    [Trait("Story", "4.1")]
    [Trait("AC", "21")]
    public void CambioDeAgenda_AntesYDesdeLaFecha_UsaLasFranjasDeCadaAgenda()
    {
        var a = AgendaBuilder.UnaAgenda().DeMinutos(20).Desde(new DateOnly(2026, 11, 2))
            .ConFranja(DayOfWeek.Monday, "08:00", "12:00").Aprobada();
        var b = AgendaBuilder.UnaAgenda().DeMinutos(30).Desde(new DateOnly(2026, 11, 16))
            .ConFranja(DayOfWeek.Monday, "14:00", "16:00").Aprobada();
        AgendaMedico[] agendas = [a, b];
        var lunes9 = new DateOnly(2026, 11, 9);
        var lunes16 = new DateOnly(2026, 11, 16);

        var horarios9 = AgendaMedico.QueRigeEn(agendas, lunes9)!.HorariosDelDia(lunes9);
        var horarios16 = AgendaMedico.QueRigeEn(agendas, lunes16)!.HorariosDelDia(lunes16);

        Assert.Equal(12, horarios9.Count);
        Assert.Equal("08:00", horarios9[0].ToString("HH:mm"));
        Assert.Equal("11:40", horarios9[^1].ToString("HH:mm"));
        Assert.Equal(["14:00", "14:30", "15:00", "15:30"], horarios16.Select(h => h.ToString("HH:mm")));
    }
}
