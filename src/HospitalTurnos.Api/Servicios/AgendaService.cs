using HospitalTurnos.Api.Datos;
using HospitalTurnos.Api.Dominio;
using Microsoft.EntityFrameworkCore;

namespace HospitalTurnos.Api.Servicios;

/// <summary>Reglas que necesitan mirar más de un turno: disponibilidad, reserva y cancelación.</summary>
public class AgendaService(HospitalDbContext db, TimeProvider reloj)
{
    private DateTime Ahora => reloj.GetLocalNow().DateTime;

    public async Task<IReadOnlyList<DateTime>> HorariosDisponiblesAsync(int medicoId, DateOnly fecha)
    {
        var medico = await BuscarMedicoAsync(medicoId);
        var desde = fecha.ToDateTime(TimeOnly.MinValue);
        var hasta = desde.AddDays(1);

        var ocupados = await db.Turnos
            .Where(t => t.MedicoId == medicoId && t.Estado == EstadoTurno.Reservado
                        && t.FechaHora >= desde && t.FechaHora < hasta)
            .Select(t => t.FechaHora)
            .ToListAsync();

        var ahora = Ahora;
        return medico.HorariosDelDia(fecha)
            .Where(h => h > ahora && !ocupados.Contains(h))
            .ToList();
    }

    public async Task<Turno> ReservarAsync(int pacienteId, int medicoId, DateTime fechaHora)
    {
        var paciente = await db.Pacientes.FindAsync(pacienteId)
                       ?? throw new NoEncontradoException($"No existe el paciente {pacienteId}.");
        var medico = await BuscarMedicoAsync(medicoId);

        var turno = new Turno(paciente, medico, fechaHora, Ahora);

        if (await db.Turnos.AnyAsync(t => t.MedicoId == medicoId && t.FechaHora == fechaHora
                                          && t.Estado == EstadoTurno.Reservado))
            throw new ConflictoException("Ese horario ya está tomado.");
        if (await db.Turnos.AnyAsync(t => t.PacienteId == pacienteId && t.FechaHora == fechaHora
                                          && t.Estado == EstadoTurno.Reservado))
            throw new ConflictoException("El paciente ya tiene otro turno a esa hora.");

        db.Turnos.Add(turno);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Otra reserva ganó la carrera entre el chequeo y el guardado; el índice único la frenó.
            db.Entry(turno).State = EntityState.Detached;
            throw new ConflictoException("Ese horario se acaba de ocupar. Elegí otro.");
        }
        return turno;
    }

    public async Task<Turno> CancelarAsync(int turnoId)
    {
        var turno = await db.Turnos
                        .Include(t => t.Paciente)
                        .Include(t => t.Medico).ThenInclude(m => m!.Especialidad)
                        .FirstOrDefaultAsync(t => t.Id == turnoId)
                    ?? throw new NoEncontradoException($"No existe el turno {turnoId}.");
        turno.Cancelar(Ahora);
        await db.SaveChangesAsync();
        return turno;
    }

    private async Task<Medico> BuscarMedicoAsync(int medicoId) =>
        await db.Medicos
            .Include(m => m.Horarios)
            .Include(m => m.Especialidad)
            .FirstOrDefaultAsync(m => m.Id == medicoId)
        ?? throw new NoEncontradoException($"No existe el médico {medicoId}.");
}
