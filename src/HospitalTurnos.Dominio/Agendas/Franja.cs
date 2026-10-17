using System.Globalization;
using HospitalTurnos.Dominio.Excepciones;

namespace HospitalTurnos.Dominio.Agendas;

/// <summary>
/// Un tramo semanal de atención: día, hora desde y hora hasta (FR-14, R-02). No cruza la medianoche: una
/// franja de 22 a 02 se carga como dos. Hasta = 00:00 significa la medianoche AL FINAL del día, porque
/// TimeOnly no puede valer 24:00; por eso las cuentas se hacen en minutos, con la medianoche = 1440.
/// </summary>
public sealed class Franja
{
    private const int MinutosDelDia = 24 * 60;

    // Para los mensajes: un arreglo propio y no CultureInfo("es-AR"), que la imagen de Docker puede no traer.
    private static readonly string[] NombresDeDias = ["domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado"];

    public long Id { get; private set; }                  // para EF; el dominio no lo usa
    public DayOfWeek Dia { get; private set; }
    public TimeOnly Desde { get; private set; }
    public TimeOnly Hasta { get; private set; }           // 00:00 = medianoche, al final del día

    private Franja() { }                                  // para EF

    public Franja(DayOfWeek dia, TimeOnly desde, TimeOnly hasta)
    {
        if (desde.Ticks % TimeSpan.TicksPerMinute != 0 || hasta.Ticks % TimeSpan.TicksPerMinute != 0)
            throw new ReglaDeNegocioException("Las horas de la franja van en horas y minutos, sin segundos.");
        if (FinEnMinutos(hasta) <= InicioEnMinutos(desde))
            throw new ReglaDeNegocioException("La franja tiene que terminar después de empezar.");
        (Dia, Desde, Hasta) = (dia, desde, hasta);
    }

    public bool TerminaAMedianoche => Hasta == TimeOnly.MinValue;
    public int DuracionMinutos => Fin - Inicio;
    internal int Inicio => InicioEnMinutos(Desde);
    internal int Fin => FinEnMinutos(Hasta);

    public bool SeSuperponeCon(Franja otra) =>
        Dia == otra.Dia && Inicio < otra.Fin && otra.Inicio < Fin;   // tocarse no es pisarse

    internal string NombreDelDia => NombresDeDias[(int)Dia];

    /// <summary>"08:00 a 12:00"; la medianoche se muestra 24:00.</summary>
    internal string Tramo => $"{Desde.ToString("HH:mm", CultureInfo.InvariantCulture)} a " +
        (TerminaAMedianoche ? "24:00" : Hasta.ToString("HH:mm", CultureInfo.InvariantCulture));

    private static int InicioEnMinutos(TimeOnly t) => t.Hour * 60 + t.Minute;
    private static int FinEnMinutos(TimeOnly t) => t == TimeOnly.MinValue ? MinutosDelDia : InicioEnMinutos(t);
}
