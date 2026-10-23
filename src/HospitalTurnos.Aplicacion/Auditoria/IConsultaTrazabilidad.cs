namespace HospitalTurnos.Aplicacion.Auditoria;

/// <summary>
/// "Creado por / Modificado por" de un registro (guía, "Detalle frente a edición"; story 3.3, AC 46): el primero y
/// el último registro de auditoría de la entidad. Sólo lee: la tabla sigue siendo de sólo inserción (ADR-07).
/// </summary>
public interface IConsultaTrazabilidad
{
    /// <summary>null si la entidad no tiene registros de auditoría.</summary>
    Task<Trazabilidad?> ObtenerAsync(string entidadTipo, string entidadId, CancellationToken ct);
}

/// <summary>Modificado: el último registro, sólo si hubo más de uno.</summary>
public sealed record Trazabilidad(MarcaDeTrazabilidad Creado, MarcaDeTrazabilidad? Modificado);

public sealed record MarcaDeTrazabilidad(string Quien, DateTimeOffset Cuando);
