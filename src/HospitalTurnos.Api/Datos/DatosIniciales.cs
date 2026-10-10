using HospitalTurnos.Api.Dominio;

namespace HospitalTurnos.Api.Datos;

/// <summary>Carga especialidades y médicos de ejemplo la primera vez que se crea la base.</summary>
public static class DatosIniciales
{
    public static void Cargar(HospitalDbContext db)
    {
        if (db.Especialidades.Any()) return;

        var clinica = new Especialidad("Clínica médica");
        var pediatria = new Especialidad("Pediatría");
        var cardiologia = new Especialidad("Cardiología");
        var traumatologia = new Especialidad("Traumatología");
        db.Especialidades.AddRange(clinica, pediatria, cardiologia, traumatologia);

        var manana = (Desde: new TimeOnly(8, 0), Hasta: new TimeOnly(12, 0));
        var tarde = (Desde: new TimeOnly(14, 0), Hasta: new TimeOnly(18, 0));

        db.Medicos.AddRange(
            CrearMedico("Laura", "Gómez", "MP-10234", clinica, 20,
                (DayOfWeek.Monday, manana), (DayOfWeek.Wednesday, manana), (DayOfWeek.Friday, manana)),
            CrearMedico("Martín", "Pereyra", "MP-11872", clinica, 20,
                (DayOfWeek.Tuesday, tarde), (DayOfWeek.Thursday, tarde)),
            CrearMedico("Sofía", "Ruiz", "MP-09311", pediatria, 30,
                (DayOfWeek.Monday, tarde), (DayOfWeek.Tuesday, manana), (DayOfWeek.Thursday, manana)),
            CrearMedico("Diego", "Fernández", "MP-12455", cardiologia, 30,
                (DayOfWeek.Wednesday, tarde), (DayOfWeek.Friday, tarde)),
            CrearMedico("Carla", "Méndez", "MP-13020", traumatologia, 15,
                (DayOfWeek.Monday, manana), (DayOfWeek.Thursday, tarde)));

        db.SaveChanges();
    }

    private static Medico CrearMedico(string nombre, string apellido, string matricula, Especialidad especialidad,
        int duracion, params (DayOfWeek Dia, (TimeOnly Desde, TimeOnly Hasta) Franja)[] horarios)
    {
        var medico = new Medico(nombre, apellido, matricula, especialidad, duracion);
        foreach (var (dia, franja) in horarios)
            medico.AgregarHorario(new HorarioAtencion(dia, franja.Desde, franja.Hasta));
        return medico;
    }
}
