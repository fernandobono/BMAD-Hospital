using HospitalTurnos.Api.Dominio;

namespace HospitalTurnos.Api.Endpoints;

public record EspecialidadDto(int Id, string Nombre);

public record HorarioDto(DayOfWeek Dia, TimeOnly Desde, TimeOnly Hasta);

public record MedicoDto(int Id, string NombreCompleto, string Matricula, int EspecialidadId, string Especialidad,
    int DuracionTurnoMinutos, IEnumerable<HorarioDto> Horarios)
{
    public static MedicoDto Desde(Medico m) => new(m.Id, m.NombreCompleto, m.Matricula, m.EspecialidadId,
        m.Especialidad?.Nombre ?? "", m.DuracionTurnoMinutos,
        m.Horarios.OrderBy(h => h.Dia).ThenBy(h => h.Desde).Select(h => new HorarioDto(h.Dia, h.Desde, h.Hasta)));
}

public record PacienteDto(int Id, string Dni, string Nombre, string Apellido, string Telefono, string? Email)
{
    public static PacienteDto Desde(Paciente p) => new(p.Id, p.Dni, p.Nombre, p.Apellido, p.Telefono, p.Email);
}

public record NuevoPacienteRequest(string Dni, string Nombre, string Apellido, string Telefono, string? Email);

public record TurnoDto(int Id, DateTime FechaHora, string Estado, int PacienteId, string Paciente, string PacienteDni,
    int MedicoId, string Medico, string Especialidad)
{
    public static TurnoDto Desde(Turno t) => new(t.Id, t.FechaHora, t.Estado.ToString(),
        t.PacienteId, t.Paciente?.NombreCompleto ?? "", t.Paciente?.Dni ?? "",
        t.MedicoId, t.Medico?.NombreCompleto ?? "", t.Medico?.Especialidad?.Nombre ?? "");
}

public record NuevoTurnoRequest(int PacienteId, int MedicoId, DateTime FechaHora);
