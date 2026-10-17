using HospitalTurnos.Aplicacion.Auditoria;

namespace HospitalTurnos.Aplicacion.Catalogo;

/// <summary>
/// El catálogo de especialidades (FR-12, story 3.1). Lo administra A2; la 3.2 (médicos) y las de turnos
/// (5.2, 5.5, 6.4) lo leen. Las escrituras confirman en la base que el actor es un administrador activo.
/// </summary>
public interface ICatalogoEspecialidades
{
    /// <summary>Todas, activas y desactivadas, ordenadas por su clave de comparación (A2).</summary>
    Task<IReadOnlyList<EspecialidadResumen>> ListarAsync(CancellationToken ct);

    /// <summary>Sólo las activas, ordenadas por su clave: las que se ofrecen en los selectores (3.2, 5.2, 6.4).</summary>
    Task<IReadOnlyList<EspecialidadResumen>> ListarActivasAsync(CancellationToken ct);

    /// <summary>La especialidad o null. La 3.2 la usa para validar en el servidor el id que recibe.</summary>
    Task<EspecialidadResumen?> BuscarAsync(long id, CancellationToken ct);

    Task<long> CrearAsync(ActorAuditoria actor, string nombre, CancellationToken ct);

    Task DesactivarAsync(ActorAuditoria actor, long id, CancellationToken ct);

    Task ReactivarAsync(ActorAuditoria actor, long id, CancellationToken ct);
}

public sealed record EspecialidadResumen(long Id, string Nombre, bool Activa);
