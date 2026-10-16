using HospitalTurnos.Aplicacion.Email;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace HospitalTurnos.Infraestructura.Email;

/// <summary>
/// Entrega el email con MailKit. No atrapa nada: tragarse el error sería el fallo silencioso. Quien decide
/// qué hacer con él (reintentar) es el procesador.
/// </summary>
public sealed class EnviadorEmailSmtp(IOptions<OpcionesSmtp> opciones) : IEnviadorEmail
{
    /// Tope para todo el envío (conectar + autenticar + enviar). El Timeout de MailKit vale por operación:
    /// sin este tope, la fila del procesador podía quedar bloqueada 90 s o más (revisión de resiliencia).
    public static readonly TimeSpan TiempoMaximoPorEmail = TimeSpan.FromSeconds(60);

    public async Task EnviarAsync(MensajeEmail mensaje, CancellationToken ct)
    {
        var op = opciones.Value;
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(op.RemitenteNombre, op.RemitenteDireccion));
        mime.To.Add(MailboxAddress.Parse(mensaje.Para));
        mime.Subject = mensaje.Asunto;
        mime.Body = new BodyBuilder { TextBody = mensaje.CuerpoTexto, HtmlBody = mensaje.CuerpoHtml }.ToMessageBody();

        // Si vence el tope, la cancelación llega como excepción con el token del host sin cancelar:
        // el procesador la cuenta como un intento fallido y reintenta.
        using var tope = CancellationTokenSource.CreateLinkedTokenSource(ct);
        tope.CancelAfter(TiempoMaximoPorEmail);
        using var smtp = new SmtpClient { Timeout = 30_000 };   // acota cada operación por separado
        await smtp.ConnectAsync(op.Host, op.Puerto,
            op.Seguridad ?? throw new InvalidOperationException("Falta Email:Smtp:Seguridad"), tope.Token);
        if (!string.IsNullOrEmpty(op.Usuario)) await smtp.AuthenticateAsync(op.Usuario, op.Clave ?? "", tope.Token);
        await smtp.SendAsync(mime, tope.Token);
        // Sin DisconnectAsync: con el email ya aceptado, un error al despedirse (QUIT) se contaba como fallo de
        // envío y el paciente lo recibía dos veces. El using cierra la conexión igual.
    }
}
