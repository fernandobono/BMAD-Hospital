using System.Globalization;

namespace HospitalTurnos.Web.Components.Comunes;

/// <summary>
/// Los formatos de la guía (Microcopy, "Formatos"; story 3.3, AC 41): fechas dd/mm/aaaa siempre con año, horas
/// HH:mm en la zona del hospital y personas "Apellido, Nombre" en listados. Un solo lugar para todas las pantallas.
/// </summary>
public static class Formatos
{
    private static readonly CultureInfo Invariante = CultureInfo.InvariantCulture;

    public static string Fecha(DateOnly fecha) => fecha.ToString("dd/MM/yyyy", Invariante);

    public static string Fecha(DateTimeOffset instante, TimeZoneInfo zona) =>
        TimeZoneInfo.ConvertTime(instante, zona).ToString("dd/MM/yyyy", Invariante);

    public static string Hora(DateTimeOffset instante, TimeZoneInfo zona) =>
        TimeZoneInfo.ConvertTime(instante, zona).ToString("HH:mm", Invariante);

    public static string FechaHora(DateTimeOffset instante, TimeZoneInfo zona) => $"{Fecha(instante, zona)} {Hora(instante, zona)}";

    public static string Persona(string apellido, string nombre) => $"{apellido}, {nombre}";
}
