namespace HospitalTurnos.Aplicacion.Email;

/// <summary>
/// Donde los casos de uso dejan los emails (ADR-05): nunca los envían ellos, así un servidor de correo
/// caído no hace fallar una reserva ni un bloqueo.
/// </summary>
public interface IBandejaDeSalida
{
    /// Agrega el mensaje SIN guardar: lo persiste el SaveChangesAsync del caso de uso (arquitectura §7.4).
    void Encolar(MensajeEmail mensaje);
}
