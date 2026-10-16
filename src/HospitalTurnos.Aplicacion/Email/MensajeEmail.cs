namespace HospitalTurnos.Aplicacion.Email;

/// <summary>
/// Un email que un caso de uso quiere mandar. <paramref name="Tipo"/> decide qué manejador se entera si
/// se da por perdido (p. ej. "aviso-bloqueo"); <paramref name="ReferenciaId"/> dice de qué turno o paciente es.
/// </summary>
public sealed record MensajeEmail(string Para, string Asunto, string CuerpoTexto, string CuerpoHtml,
                                  string Tipo, string? ReferenciaId);
