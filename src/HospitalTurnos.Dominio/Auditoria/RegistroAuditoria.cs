namespace HospitalTurnos.Dominio.Auditoria;

/// <summary>
/// Una entrada del registro de auditoría (FR-33, FR-35, DOM-03). Nace completa y no cambia nunca: sin setters
/// públicos en el código, y en la base sólo se puede insertar (permisos + disparador, ADR-07).
/// </summary>
public sealed class RegistroAuditoria
{
    public long Id { get; private set; }
    public DateTimeOffset Cuando { get; private set; }
    public string? UsuarioId { get; private set; }
    public string Quien { get; private set; } = null!;
    public string Rol { get; private set; } = null!;
    public CanalAuditoria Canal { get; private set; }
    public string Accion { get; private set; } = null!;
    public string EntidadTipo { get; private set; } = null!;
    public string EntidadId { get; private set; } = null!;
    public string? Detalle { get; private set; }   // JSON ya serializado → columna jsonb

    private RegistroAuditoria() { } // para EF

    public RegistroAuditoria(DateTimeOffset cuando, string? usuarioId, string quien, string rol,
        CanalAuditoria canal, string accion, string entidadTipo, string entidadId, string? detalle)
    {
        // Por qué validar acá: Npgsql rechaza un timestamptz con offset ≠ 0 recién al guardar, lejos del error real.
        if (cuando.Offset != TimeSpan.Zero)
            throw new ArgumentException("La marca de auditoría tiene que estar en UTC.", nameof(cuando));
        ArgumentException.ThrowIfNullOrWhiteSpace(quien);
        ArgumentException.ThrowIfNullOrWhiteSpace(rol);
        ArgumentException.ThrowIfNullOrWhiteSpace(accion);
        ArgumentException.ThrowIfNullOrWhiteSpace(entidadTipo);
        ArgumentException.ThrowIfNullOrWhiteSpace(entidadId);

        Cuando = cuando;
        UsuarioId = usuarioId;
        Quien = quien;
        Rol = rol;
        Canal = canal;
        Accion = accion;
        EntidadTipo = entidadTipo;
        EntidadId = entidadId;
        Detalle = detalle;
    }
}
