using System.ComponentModel.DataAnnotations;
using MailKit.Security;

namespace HospitalTurnos.Infraestructura.Email;

/// <summary>
/// Configuración <c>Email:Smtp</c>. No hay valores por defecto escondidos: sin Host o sin remitente la app
/// no arranca (AC 16), en vez de fallar en silencio con cada email. Usuario y clave sólo por variables de entorno.
/// </summary>
public sealed class OpcionesSmtp
{
    // [Required] rechaza también el texto vacío y el que es sólo espacios.
    [Required(ErrorMessage = "Falta Email:Smtp:Host")]
    public string Host { get; set; } = "";

    [Range(1, 65535, ErrorMessage = "Falta Email:Smtp:Puerto (entre 1 y 65535)")]
    public int Puerto { get; set; }

    public string? Usuario { get; set; }
    public string? Clave { get; set; }

    /// None en desarrollo (Mailpit), StartTls en producción. Obligatoria y sin valor por defecto: si faltara,
    /// el enum caería en None y el correo saldría sin cifrar, con usuario y clave a la vista (revisión de seguridad).
    [Required(ErrorMessage = "Falta Email:Smtp:Seguridad")]
    public SecureSocketOptions? Seguridad { get; set; }

    public string RemitenteNombre { get; set; } = "";

    [Required(ErrorMessage = "Falta Email:Smtp:RemitenteDireccion")]
    public string RemitenteDireccion { get; set; } = "";
}
