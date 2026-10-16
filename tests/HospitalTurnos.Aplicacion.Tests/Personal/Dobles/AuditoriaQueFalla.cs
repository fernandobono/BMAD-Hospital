using HospitalTurnos.Aplicacion.Auditoria;

namespace HospitalTurnos.Aplicacion.Tests.Personal.Dobles;

/// <summary>La auditoría que no puede registrar (T5): el alta tiene que deshacerse entera.</summary>
public sealed class AuditoriaQueFalla : IAuditoria
{
    public void Registrar(ActorAuditoria actor, string accion, string entidadTipo, string entidadId, object? detalle = null) =>
        throw new InvalidOperationException("auditoría caída en la prueba");
}
