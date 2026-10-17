using Microsoft.Extensions.Time.Testing;

namespace HospitalTurnos.Dominio.Tests.Ayudas;

/// <summary>
/// Un reloj falso en la zona del hospital (Córdoba, UTC−3 sin horario de verano). Las pruebas nunca esperan
/// tiempo real: fijan la hora y, si hace falta, la mueven a mano (retrospectiva del sprint 1, acción A-4).
/// </summary>
public static class RelojDePrueba
{
    public static readonly TimeZoneInfo Cordoba = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Cordoba");

    /// <summary>La hora pedida es LOCAL del hospital; el reloj guarda su equivalente UTC (+3 horas).</summary>
    public static FakeTimeProvider En(int anio, int mes, int dia, int hora = 12, int minuto = 0) =>
        EnUtc(new DateTimeOffset(anio, mes, dia, hora, minuto, 0, TimeSpan.FromHours(-3)).ToUniversalTime());

    public static FakeTimeProvider EnUtc(DateTimeOffset instante)
    {
        var reloj = new FakeTimeProvider(instante);
        reloj.SetLocalTimeZone(Cordoba);
        return reloj;
    }
}
