namespace HospitalTurnos.Infraestructura.Tiempo;

/// <summary>
/// El único lugar del sistema que sabe en qué zona está el hospital (NFR-06).
/// Hereda de TimeProvider: el resto del código pide la hora a TimeProvider y en las
/// pruebas se cambia por un FakeTimeProvider fijo.
/// </summary>
public sealed class RelojHospital : TimeProvider
{
    public const string ZonaCordoba = "America/Argentina/Cordoba";
    private readonly TimeZoneInfo _zona;

    public RelojHospital() : this(ZonaCordoba)
    {
    }

    /// <summary>Con id sólo para probar el modo de falla: la zona no se configura.</summary>
    public RelojHospital(string idZona)
    {
        try
        {
            _zona = TimeZoneInfo.FindSystemTimeZoneById(idZona);
        }
        catch (TimeZoneNotFoundException ex)
        {
            // Por qué fallar al arrancar: una hora corrida 3 h daría turnos en horarios equivocados.
            throw new InvalidOperationException(
                $"No se encontró la zona horaria '{idZona}' en esta máquina. Instalá el paquete tzdata.", ex);
        }
    }

    public override TimeZoneInfo LocalTimeZone => _zona;
}
