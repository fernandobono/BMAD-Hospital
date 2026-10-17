using HospitalTurnos.Dominio.Agendas;
using HospitalTurnos.Dominio.Excepciones;

namespace HospitalTurnos.Dominio.Tests.Agendas;

public class FranjaTests
{
    private static Franja F(DayOfWeek dia, string desde, string hasta) =>
        new(dia, TimeOnly.Parse(desde), TimeOnly.Parse(hasta));

    [Theory]
    [Trait("Story", "4.1")]
    [Trait("AC", "1")]
    [InlineData(DayOfWeek.Monday, "08:00", "12:00", 240, false)]
    [InlineData(DayOfWeek.Monday, "20:00", "00:00", 240, true)]
    [InlineData(DayOfWeek.Sunday, "00:00", "00:00", 1440, true)]
    public void Franja_HorasValidas_ExponeDuracionYMedianoche(DayOfWeek dia, string desde, string hasta,
        int duracion, bool terminaAMedianoche)
    {
        var franja = F(dia, desde, hasta);

        Assert.Equal(duracion, franja.DuracionMinutos);
        Assert.Equal(terminaAMedianoche, franja.TerminaAMedianoche);
        Assert.Equal(dia, franja.Dia);
        Assert.Equal(TimeOnly.Parse(desde), franja.Desde);
        Assert.Equal(TimeOnly.Parse(hasta), franja.Hasta);
    }

    [Theory]
    [Trait("Story", "4.1")]
    [Trait("AC", "2")]
    [InlineData("10:00", "10:00", "La franja tiene que terminar después de empezar.")]
    [InlineData("12:00", "08:00", "La franja tiene que terminar después de empezar.")]
    [InlineData("22:00", "02:00", "La franja tiene que terminar después de empezar.")]
    [InlineData("08:00:30", "12:00", "Las horas de la franja van en horas y minutos, sin segundos.")]
    [InlineData("08:00", "12:00:15", "Las horas de la franja van en horas y minutos, sin segundos.")]
    public void Franja_HorasInvalidas_LanzaReglaDeNegocio(string desde, string hasta, string mensaje)
    {
        var error = Assert.Throws<ReglaDeNegocioException>(() => F(DayOfWeek.Monday, desde, hasta));

        Assert.Equal(mensaje, error.Message);
    }

    [Theory]
    [Trait("Story", "4.1")]
    [Trait("AC", "3")]
    [InlineData(DayOfWeek.Monday, "08:00", "12:00", DayOfWeek.Monday, "10:00", "14:00", true)]
    [InlineData(DayOfWeek.Monday, "08:00", "12:00", DayOfWeek.Monday, "12:00", "16:00", false)]
    [InlineData(DayOfWeek.Monday, "08:00", "12:00", DayOfWeek.Monday, "06:00", "08:00", false)]
    [InlineData(DayOfWeek.Monday, "08:00", "12:00", DayOfWeek.Monday, "09:00", "10:00", true)]
    [InlineData(DayOfWeek.Monday, "08:00", "12:00", DayOfWeek.Tuesday, "08:00", "12:00", false)]
    [InlineData(DayOfWeek.Monday, "22:00", "00:00", DayOfWeek.Monday, "23:00", "23:30", true)]
    [InlineData(DayOfWeek.Monday, "22:00", "00:00", DayOfWeek.Monday, "00:00", "02:00", false)]
    public void SeSuperponeCon_CasosDeBorde_DevuelveSiSePisan(DayOfWeek diaA, string desdeA, string hastaA,
        DayOfWeek diaB, string desdeB, string hastaB, bool esperado)
    {
        var a = F(diaA, desdeA, hastaA);
        var b = F(diaB, desdeB, hastaB);

        Assert.Equal(esperado, a.SeSuperponeCon(b));
        Assert.Equal(esperado, b.SeSuperponeCon(a));
    }
}
