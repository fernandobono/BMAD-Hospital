using HospitalTurnos.Aplicacion.Email;
using MailKit.Net.Smtp;
using MimeKit;

namespace HospitalTurnos.Aplicacion.Tests.Email.Dobles;

/// <summary>
/// Un "fake" del servidor de correo, escrito a mano (mismo estilo que el proyecto guía): cuenta las
/// llamadas y guarda lo que recibió. El modo decide cómo responde.
/// </summary>
public sealed class EnviadorEmailFalso(EnviadorEmailFalso.Modo modo = EnviadorEmailFalso.Modo.RespondeBien) : IEnviadorEmail
{
    public enum Modo
    {
        RespondeBien,
        SiempreLanza,       // SMTP caído
        RechazaDestinatario, // el servidor responde 550 (casilla inexistente), como lo haría MailKit
        Compuerta,          // se queda adentro de EnviarAsync hasta que la prueba llame Liberar()
        EsperaCancelacion,  // se queda adentro hasta que cancelen el token (apagado del host)
    }

    private readonly TaskCompletionSource _compuerta = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly List<MensajeEmail> _recibidos = [];
    private int _llamadas;

    public int Llamadas => Volatile.Read(ref _llamadas);

    public IReadOnlyList<MensajeEmail> Recibidos
    {
        get { lock (_recibidos) return [.. _recibidos]; }
    }

    public void Liberar() => _compuerta.TrySetResult();

    public async Task EnviarAsync(MensajeEmail mensaje, CancellationToken ct)
    {
        Interlocked.Increment(ref _llamadas);
        lock (_recibidos) _recibidos.Add(mensaje);
        switch (modo)
        {
            case Modo.SiempreLanza:
                // Como un servidor real (p. ej. Postfix): el texto del rechazo repite la dirección.
                throw new IOException($"<{mensaje.Para}>: Recipient address rejected");
            case Modo.RechazaDestinatario:
                throw new SmtpCommandException(SmtpErrorCode.RecipientNotAccepted, SmtpStatusCode.MailboxUnavailable,
                    MailboxAddress.Parse(mensaje.Para), $"<{mensaje.Para}>: Recipient address rejected: User unknown");
            case Modo.Compuerta:
                await _compuerta.Task.WaitAsync(ct);
                break;
            case Modo.EsperaCancelacion:
                await Task.Delay(Timeout.Infinite, ct);
                break;
        }
    }
}
