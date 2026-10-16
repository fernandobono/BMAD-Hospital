using HospitalTurnos.Infraestructura.Tiempo;

namespace HospitalTurnos.Aplicacion.Tests.Tiempo;

public class RelojHospitalTests
{
    [Fact]
    [Trait("Story", "1.2")]
    [Trait("AC", "1")]
    public void LocalTimeZone_RelojSinArgumentos_EsCordobaMenosTres()
    {
        var reloj = new RelojHospital();

        Assert.Equal("America/Argentina/Cordoba", reloj.LocalTimeZone.Id);
        Assert.Equal(TimeSpan.FromHours(-3), reloj.LocalTimeZone.BaseUtcOffset);
    }

    [Fact]
    [Trait("Story", "1.2")]
    [Trait("AC", "4")]
    public void Constructor_ZonaInexistente_LanzaErrorQueNombraLaZona()
    {
        var error = Assert.Throws<InvalidOperationException>(() => new RelojHospital("America/Inexistente"));

        Assert.Contains("America/Inexistente", error.Message);
        Assert.Contains("tzdata", error.Message);
        Assert.IsType<TimeZoneNotFoundException>(error.InnerException);
    }
}
