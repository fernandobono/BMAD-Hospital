using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace HospitalTurnos.Infraestructura.Auditoria;

/// <summary>
/// IConsultaTrazabilidad sobre registros_auditoria (story 3.3, AC 46): el primero y el último registro de la
/// entidad, por id (el orden en que se insertaron). Sólo SELECT: la app ya lo tiene desde la 1.3 (ADR-07).
/// </summary>
public sealed class ConsultaTrazabilidadEnBase(HospitalDbContext contexto) : IConsultaTrazabilidad
{
    public async Task<Trazabilidad?> ObtenerAsync(string entidadTipo, string entidadId, CancellationToken ct)
    {
        var registros = contexto.RegistrosAuditoria.AsNoTracking()
            .Where(r => r.EntidadTipo == entidadTipo && r.EntidadId == entidadId);
        var primero = await registros.OrderBy(r => r.Id)
            .Select(r => new { r.Id, Marca = new MarcaDeTrazabilidad(r.Quien, r.Cuando) }).FirstOrDefaultAsync(ct);
        if (primero is null)
            return null;
        var ultimo = await registros.OrderByDescending(r => r.Id)
            .Select(r => new { r.Id, Marca = new MarcaDeTrazabilidad(r.Quien, r.Cuando) }).FirstAsync(ct);
        return new Trazabilidad(primero.Marca, ultimo.Id == primero.Id ? null : ultimo.Marca);
    }
}
