namespace HospitalTurnos.Dominio.Auditoria;

/// <summary>Por dónde llegó el cambio. Se guarda como texto: el registro legal lo lee una persona.</summary>
public enum CanalAuditoria
{
    Web,        // el paciente, por internet
    Recepcion,  // cambios de turnos hechos por la recepción (CE-01 compara Web contra éste)
    Personal,   // acciones del personal fuera de los turnos (p. ej. ingreso al sistema)
    Sistema     // procesos automáticos (p. ej. la bandeja de salida)
}
