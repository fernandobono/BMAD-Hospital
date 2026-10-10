namespace HospitalTurnos.Api.Dominio;

public class Medico
{
    private readonly List<HorarioAtencion> _horarios = [];

    public int Id { get; private set; }
    public string Nombre { get; private set; } = "";
    public string Apellido { get; private set; } = "";
    public string Matricula { get; private set; } = "";
    public int EspecialidadId { get; private set; }
    public Especialidad? Especialidad { get; private set; }
    public int DuracionTurnoMinutos { get; private set; }
    public IReadOnlyCollection<HorarioAtencion> Horarios => _horarios;

    private Medico() { }

    public Medico(string nombre, string apellido, string matricula, Especialidad especialidad, int duracionTurnoMinutos)
    {
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido))
            throw new ReglaDeNegocioException("El médico necesita nombre y apellido.");
        if (string.IsNullOrWhiteSpace(matricula))
            throw new ReglaDeNegocioException("El médico necesita matrícula.");
        if (duracionTurnoMinutos is < 5 or > 240)
            throw new ReglaDeNegocioException("La duración del turno debe estar entre 5 y 240 minutos.");

        Nombre = nombre.Trim();
        Apellido = apellido.Trim();
        Matricula = matricula.Trim();
        Especialidad = especialidad;
        EspecialidadId = especialidad.Id;
        DuracionTurnoMinutos = duracionTurnoMinutos;
    }

    public string NombreCompleto => $"{Apellido}, {Nombre}";

    public void AgregarHorario(HorarioAtencion horario)
    {
        if (_horarios.Any(h => h.SeSuperponeCon(horario)))
            throw new ReglaDeNegocioException("El horario se superpone con otro del mismo médico.");
        _horarios.Add(horario);
    }

    /// <summary>
    /// Todos los inicios de turno que el médico ofrece en una fecha, según sus horarios
    /// y la duración del turno. Un turno que no entra completo en la franja no se ofrece.
    /// </summary>
    public IEnumerable<DateTime> HorariosDelDia(DateOnly fecha)
    {
        var duracion = TimeSpan.FromMinutes(DuracionTurnoMinutos);
        foreach (var franja in _horarios.Where(h => h.Dia == fecha.DayOfWeek).OrderBy(h => h.Desde))
        {
            var inicio = fecha.ToDateTime(franja.Desde);
            var fin = fecha.ToDateTime(franja.Hasta);
            for (var t = inicio; t + duracion <= fin; t += duracion)
                yield return t;
        }
    }

    public bool AtiendeEn(DateTime fechaHora) =>
        HorariosDelDia(DateOnly.FromDateTime(fechaHora)).Contains(fechaHora);
}
