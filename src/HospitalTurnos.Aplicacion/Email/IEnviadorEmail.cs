namespace HospitalTurnos.Aplicacion.Email;

/// <summary>Entrega un email al servidor de correo. Sólo lo usa el proceso de fondo de la bandeja.</summary>
public interface IEnviadorEmail
{
    /// Lanza excepción si no pudo entregar al servidor SMTP. No atrapa nada.
    Task EnviarAsync(MensajeEmail mensaje, CancellationToken ct);
}
