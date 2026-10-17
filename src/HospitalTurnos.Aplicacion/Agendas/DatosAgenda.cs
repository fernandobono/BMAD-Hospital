namespace HospitalTurnos.Aplicacion.Agendas;

/// <summary>Una franja tal como la escribió el médico (story 4.2). Hasta = 00:00 es la medianoche al final del día.</summary>
public sealed record DatosFranja(DayOfWeek Dia, TimeOnly Desde, TimeOnly Hasta);

/// <summary>Lo que manda una pantalla de agenda: M2 (4.2) y la corrección de urgencia (4.4).</summary>
public sealed record DatosAgenda(int DuracionTurnoMinutos, DateOnly VigenteDesde, IReadOnlyList<DatosFranja> Franjas);
