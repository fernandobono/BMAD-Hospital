using HospitalTurnos.Aplicacion.Email;

namespace HospitalTurnos.Infraestructura.Email;

/// <summary>
/// Una fila de la bandeja de salida. Pendiente mientras <see cref="EnviadoEl"/> y <see cref="FalloDefinitivoEl"/>
/// estén vacías. <see cref="ManejadoEl"/> separa "el email se dio por perdido" de "el manejador ya actuó",
/// para reintentar el manejador sin reenviar el email (AC 9). Todas las marcas en UTC.
/// </summary>
public sealed class MensajeSaliente
{
    // Esperas entre intentos (S-33). Fallo 1 → +1 min, 2 → +5, 3 → +15, 4 → definitivo.
    public static readonly TimeSpan[] EsperasEntreReintentos =
        [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15)];

    public const int LargoMaximoError = 500;

    public long Id { get; private set; }
    public string Para { get; private set; } = null!;
    public string Asunto { get; private set; } = null!;
    public string CuerpoTexto { get; private set; } = null!;
    public string CuerpoHtml { get; private set; } = null!;
    public string Tipo { get; private set; } = null!;
    public string? ReferenciaId { get; private set; }
    public DateTimeOffset CreadoEl { get; private set; }
    public int Intentos { get; private set; }
    public DateTimeOffset ProximoIntentoEl { get; private set; }
    public DateTimeOffset? EnviadoEl { get; private set; }
    public DateTimeOffset? FalloDefinitivoEl { get; private set; }
    public DateTimeOffset? ManejadoEl { get; private set; }
    public string? UltimoError { get; private set; }

    private MensajeSaliente() { } // para EF

    /// <summary>Nace pendiente y lista para el próximo ciclo: el primer intento es inmediato.</summary>
    public MensajeSaliente(MensajeEmail mensaje, DateTimeOffset ahora)
    {
        Para = mensaje.Para;
        Asunto = mensaje.Asunto;
        CuerpoTexto = mensaje.CuerpoTexto;
        CuerpoHtml = mensaje.CuerpoHtml;
        Tipo = mensaje.Tipo;
        ReferenciaId = mensaje.ReferenciaId;
        CreadoEl = ahora;
        ProximoIntentoEl = ahora;
    }

    public MensajeEmail AMensajeEmail() => new(Para, Asunto, CuerpoTexto, CuerpoHtml, Tipo, ReferenciaId);

    public void MarcarEnviado(DateTimeOffset ahora)
    {
        Intentos++;
        EnviadoEl = ahora;
    }

    /// <summary>Un intento fallido: reprograma el siguiente o, al fallar el 4.º, lo da por perdido.</summary>
    public void RegistrarFallo(DateTimeOffset ahora, string error)
    {
        Intentos++;
        UltimoError = Recortar(error);
        if (Intentos > EsperasEntreReintentos.Length) FalloDefinitivoEl = ahora;
        else ProximoIntentoEl = ahora + EsperasEntreReintentos[Intentos - 1];
    }

    /// <summary>Perdido sin intentar (p. ej. destinatario inválido): reintentar no lo arregla.</summary>
    public void MarcarFalloDefinitivo(DateTimeOffset ahora, string motivo)
    {
        FalloDefinitivoEl = ahora;
        UltimoError = Recortar(motivo);
    }

    public void MarcarManejado(DateTimeOffset ahora) => ManejadoEl = ahora;

    private static string Recortar(string texto) =>
        texto.Length > LargoMaximoError ? texto[..LargoMaximoError] : texto;
}
