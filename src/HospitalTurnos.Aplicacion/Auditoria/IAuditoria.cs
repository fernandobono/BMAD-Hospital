namespace HospitalTurnos.Aplicacion.Auditoria;

public interface IAuditoria
{
    /// <summary>
    /// Agrega el registro al contexto SIN guardar: lo persiste el SaveChangesAsync del caso de uso,
    /// así el cambio y su registro se guardan juntos o no se guarda ninguno (arquitectura §7.5).
    /// </summary>
    void Registrar(ActorAuditoria actor, string accion, string entidadTipo, string entidadId, object? detalle = null);
}
