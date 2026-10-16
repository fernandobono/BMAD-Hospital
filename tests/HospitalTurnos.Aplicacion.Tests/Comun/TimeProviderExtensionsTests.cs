using HospitalTurnos.Aplicacion.Comun;
using Microsoft.Extensions.Time.Testing;

namespace HospitalTurnos.Aplicacion.Tests.Comun;

public class TimeProviderExtensionsTests
{
    public static TheoryData<DateTimeOffset, DateTime> Casos => new()
    {
        // instante UTC → hora local del hospital
        { new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero), new DateTime(2026, 10, 9, 9, 0, 0) },
        // borde: el día local es el ANTERIOR al día UTC
        { new DateTimeOffset(2026, 10, 10, 2, 30, 0, TimeSpan.Zero), new DateTime(2026, 10, 9, 23, 30, 0) },
    };

    [Theory]
    [Trait("Story", "1.2")]
    [Trait("AC", "2")]
    [Trait("AC", "3")]
    [MemberData(nameof(Casos))]
    public void AhoraLocal_InstanteUtc_DevuelveHoraLocalDeCordobaSinKind(DateTimeOffset instanteUtc, DateTime esperado)
    {
        var reloj = new FakeTimeProvider(instanteUtc);
        reloj.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Cordoba"));

        var ahora = reloj.AhoraLocal();

        Assert.Equal(esperado, ahora);
        Assert.Equal(DateTimeKind.Unspecified, ahora.Kind);
    }
}
