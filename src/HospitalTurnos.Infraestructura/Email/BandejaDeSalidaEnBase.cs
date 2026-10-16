using HospitalTurnos.Aplicacion.Email;
using HospitalTurnos.Infraestructura.Persistencia;

namespace HospitalTurnos.Infraestructura.Email;

/// <summary>
/// Deja el email en el mismo contexto que el cambio del caso de uso. Nunca llama SaveChanges: el mensaje
/// se guarda con el cambio o no se guarda. No valida la dirección: un email mal cargado no puede hacer
/// fallar una reserva (ADR-05); el procesador lo da por perdido (AC 8).
/// </summary>
public sealed class BandejaDeSalidaEnBase(HospitalDbContext contexto, TimeProvider reloj) : IBandejaDeSalida
{
    public void Encolar(MensajeEmail mensaje)
    {
        ArgumentNullException.ThrowIfNull(mensaje);
        contexto.MensajesSalientes.Add(new MensajeSaliente(mensaje, reloj.GetUtcNow()));
    }
}
