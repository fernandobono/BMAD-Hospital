namespace HospitalTurnos.Api.Dominio;

public enum EstadoTurno
{
    Reservado = 0,
    Cancelado = 1
}

public class Turno
{
    public int Id { get; private set; }
    public int PacienteId { get; private set; }
    public Paciente? Paciente { get; private set; }
    public int MedicoId { get; private set; }
    public Medico? Medico { get; private set; }
    public DateTime FechaHora { get; private set; }
    public EstadoTurno Estado { get; private set; }
    public DateTime ReservadoEl { get; private set; }
    public DateTime? CanceladoEl { get; private set; }

    private Turno() { }

    /// <summary>
    /// Crea un turno reservado. Valida lo que depende sólo del turno; lo que depende de
    /// otros turnos (que el horario esté libre) lo valida el servicio de agenda.
    /// </summary>
    public Turno(Paciente paciente, Medico medico, DateTime fechaHora, DateTime ahora)
    {
        if (fechaHora <= ahora)
            throw new ReglaDeNegocioException("No se puede reservar un turno en el pasado.");
        if (!medico.AtiendeEn(fechaHora))
            throw new ReglaDeNegocioException("El médico no atiende en ese horario.");

        Paciente = paciente;
        PacienteId = paciente.Id;
        Medico = medico;
        MedicoId = medico.Id;
        FechaHora = fechaHora;
        Estado = EstadoTurno.Reservado;
        ReservadoEl = ahora;
    }

    public void Cancelar(DateTime ahora)
    {
        if (Estado == EstadoTurno.Cancelado)
            throw new ConflictoException("El turno ya estaba cancelado.");
        if (FechaHora <= ahora)
            throw new ReglaDeNegocioException("No se puede cancelar un turno que ya pasó.");

        Estado = EstadoTurno.Cancelado;
        CanceladoEl = ahora;
    }
}
