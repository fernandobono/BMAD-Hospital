using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Comun;

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

    /// <summary>
    /// A2 paginada en la base (story 3.3, AC 43): busca sin tildes ni mayúsculas, filtra por "estado" (activos,
    /// inactivos, todos) y ordena por "nombre" o "medicos" (cantidad de médicos asociados).
    /// </summary>
    Task<PaginaDeResultados<EspecialidadFila>> ListarPaginadoAsync(ConsultaDeListado consulta, CancellationToken ct);

    /// <summary>Cuántos médicos la tienen asignada: la consecuencia que muestra la confirmación de baja (AC 52).</summary>
    Task<int> ContarMedicosAsync(long id, CancellationToken ct);

    Task<long> CrearAsync(ActorAuditoria actor, string nombre, CancellationToken ct);

    Task DesactivarAsync(ActorAuditoria actor, long id, CancellationToken ct);

    Task ReactivarAsync(ActorAuditoria actor, long id, CancellationToken ct);
}

public sealed record EspecialidadResumen(long Id, string Nombre, bool Activa);

/// <summary>Una fila de A2 con la columna derivada "Médicos asociados" (story 3.3, AC 43 y 50).</summary>
public sealed record EspecialidadFila(long Id, string Nombre, bool Activa, int MedicosAsociados);
