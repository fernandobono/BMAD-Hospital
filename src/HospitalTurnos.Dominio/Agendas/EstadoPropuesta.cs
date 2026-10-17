namespace HospitalTurnos.Dominio.Agendas;

/// <summary>
/// El ciclo de una agenda (Arq. §6, FR-14b): el diagrama de estados, ahora ejecutable. Lo que no está acá
/// (por ejemplo, aprobar un borrador) lo frena AgendaMedico con un 409.
/// </summary>
public enum EstadoPropuesta
{
    Borrador,   // el médico la arma: se edita libremente; se puede enviar
    Enviada,    // espera al administrador: no se edita; se aprueba o se rechaza (una sola por médico)
    Aprobada,   // rige desde su VigenteDesde: no se modifica nunca (se arma otra propuesta)
    Rechazada,  // volvió con un motivo: editarla la pasa a Borrador, conservando el motivo hasta reenviar
}
