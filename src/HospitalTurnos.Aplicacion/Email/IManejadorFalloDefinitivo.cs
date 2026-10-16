namespace HospitalTurnos.Aplicacion.Email;

/// <summary>Un email que se dio por perdido: lo recibe el manejador de su <see cref="Tipo"/>.</summary>
public sealed record FalloDefinitivoEmail(long MensajeId, string Tipo, string? ReferenciaId, string Para, string Motivo);

/// <summary>
/// Se entera cuando un email de su tipo se da por perdido (p. ej. la story 7.2 pone al paciente en
/// "Pacientes para avisar"). Puede haber varios por tipo, o ninguno.
/// </summary>
public interface IManejadorFalloDefinitivo
{
    string Tipo { get; }   // p. ej. "aviso-bloqueo" (lo registra la story 7.2)

    /// Agrega cambios al contexto SIN guardar; el procesador guarda junto con ManejadoEl.
    Task ManejarAsync(FalloDefinitivoEmail fallo, CancellationToken ct);
}
