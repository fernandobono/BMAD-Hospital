using System.Text.Json;
using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Dominio.Auditoria;
using HospitalTurnos.Infraestructura.Persistencia;

namespace HospitalTurnos.Infraestructura.Auditoria;

/// <summary>
/// Escribe el registro en el mismo contexto que el cambio. Nunca llama SaveChanges: un registro guardado
/// aparte podría quedar aunque el cambio falle (o al revés).
/// </summary>
public sealed class AuditoriaEnBase(HospitalDbContext contexto, TimeProvider reloj) : IAuditoria
{
    public void Registrar(ActorAuditoria actor, string accion, string entidadTipo, string entidadId, object? detalle = null)
    {
        var detalleJson = detalle is null ? null : JsonSerializer.Serialize(detalle, JsonSerializerOptions.Web);
        contexto.RegistrosAuditoria.Add(new RegistroAuditoria(reloj.GetUtcNow(), actor.UsuarioId, actor.Quien,
            actor.Rol, actor.Canal, accion, entidadTipo, entidadId, detalleJson));
    }
}
