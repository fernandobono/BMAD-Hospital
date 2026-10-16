namespace HospitalTurnos.Aplicacion.Comun;

public static class TimeProviderExtensions
{
    /// <summary>
    /// Hora local del hospital, sin zona (Kind=Unspecified): así se guardan los turnos
    /// (contrato técnico, "Fechas de negocio").
    /// </summary>
    public static DateTime AhoraLocal(this TimeProvider reloj)
        => DateTime.SpecifyKind(reloj.GetLocalNow().DateTime, DateTimeKind.Unspecified);
}
