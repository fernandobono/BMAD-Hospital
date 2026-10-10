namespace HospitalTurnos.Api.Dominio;

/// <summary>Franja semanal en la que un médico atiende (por ejemplo, lunes de 08:00 a 12:00).</summary>
public class HorarioAtencion
{
    public int Id { get; private set; }
    public int MedicoId { get; private set; }
    public DayOfWeek Dia { get; private set; }
    public TimeOnly Desde { get; private set; }
    public TimeOnly Hasta { get; private set; }

    private HorarioAtencion() { }

    public HorarioAtencion(DayOfWeek dia, TimeOnly desde, TimeOnly hasta)
    {
        if (hasta <= desde)
            throw new ReglaDeNegocioException("El horario debe terminar después de empezar.");
        Dia = dia;
        Desde = desde;
        Hasta = hasta;
    }

    public bool SeSuperponeCon(HorarioAtencion otro) =>
        Dia == otro.Dia && Desde < otro.Hasta && otro.Desde < Hasta;
}
