using HospitalTurnos.Dominio.Agendas;

namespace HospitalTurnos.Dominio.Tests.Ayudas;

/// <summary>
/// El "Given" de las agendas, con la forma del VentaBuilder del proyecto guía. Llega a cada estado SÓLO con los
/// métodos públicos (Enviar, Aprobar, Rechazar): así no puede armar un estado que el dominio no permite.
/// Por defecto: médico 1, turnos de 20 minutos, desde el lunes 02/11/2026, sin franjas.
/// </summary>
public class AgendaBuilder
{
    private long _medicoId = 1;
    private int _minutos = 20;
    private DateOnly _desde = new(2026, 11, 2);
    private readonly List<Franja> _franjas = [];

    public static AgendaBuilder UnaAgenda() => new();

    /// <summary>El reloj por defecto: viernes 16/10/2026 12:00 en Córdoba.</summary>
    public static TimeProvider RelojPorDefecto() => RelojDePrueba.En(2026, 10, 16);

    public AgendaBuilder DelMedico(long medicoId) { _medicoId = medicoId; return this; }
    public AgendaBuilder DeMinutos(int minutos) { _minutos = minutos; return this; }
    public AgendaBuilder Desde(DateOnly desde) { _desde = desde; return this; }

    public AgendaBuilder ConFranja(DayOfWeek dia, string desde, string hasta)
    {
        _franjas.Add(new Franja(dia, TimeOnly.Parse(desde), TimeOnly.Parse(hasta)));
        return this;
    }

    public AgendaMedico Borrador()
    {
        var agenda = new AgendaMedico(_medicoId, _minutos, _desde);
        foreach (var franja in _franjas)
            agenda.AgregarFranja(franja);
        return agenda;
    }

    /// <summary>Enviar exige al menos una franja: si no se cargó ninguna, agrega lunes 08:00–12:00.</summary>
    public AgendaMedico Enviada(TimeProvider? reloj = null)
    {
        if (_franjas.Count == 0)
            ConFranja(DayOfWeek.Monday, "08:00", "12:00");
        var agenda = Borrador();
        agenda.Enviar(reloj ?? RelojPorDefecto());
        return agenda;
    }

    public AgendaMedico Rechazada(TimeProvider? reloj = null, string motivo = "Motivo de prueba")
    {
        var relojUsado = reloj ?? RelojPorDefecto();
        var agenda = Enviada(relojUsado);
        agenda.Rechazar(motivo, relojUsado);
        return agenda;
    }

    /// <summary>Envía y aprueba con el mismo reloj (la VigenteDesde tiene que estar a 7 días o más de él).</summary>
    public AgendaMedico Aprobada(TimeProvider? reloj = null)
    {
        var relojUsado = reloj ?? RelojPorDefecto();
        var agenda = Enviada(relojUsado);
        agenda.Aprobar(relojUsado);
        return agenda;
    }
}
