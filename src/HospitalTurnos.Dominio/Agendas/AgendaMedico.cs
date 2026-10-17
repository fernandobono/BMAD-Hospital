using System.Globalization;
using HospitalTurnos.Dominio.Excepciones;

namespace HospitalTurnos.Dominio.Agendas;

/// <summary>
/// Una versión de la agenda semanal de un médico (FR-14, ADR-06): mientras no está aprobada es una propuesta.
/// Las franjas sólo cambian por sus métodos. Cada edición sigue el mismo orden: estado (409) → validar (400)
/// → cambiar → corregir (una rechazada pasa a borrador recién con el cambio hecho, AC 12 y 13).
/// "Hoy" es la fecha LOCAL del hospital, leída del reloj que recibe: nunca la fecha UTC.
/// </summary>
public sealed class AgendaMedico
{
    public const int DuracionMinima = 10, DuracionMaxima = 60;   // R-02
    public const int DiasMinimosDeAnticipacion = 7;              // S-31
    public const int LargoMaximoMotivo = 500;

    private const string YaEnviada = "La propuesta ya fue enviada.";
    private const string CorregirAntes = "Corregí la propuesta antes de reenviarla.";
    private const string AprobadaNoSeModifica = "Una agenda aprobada no se modifica: armá una propuesta nueva.";
    private const string SoloAprobarEnviada = "Sólo se puede aprobar una propuesta enviada.";
    private const string SoloRechazarEnviada = "Sólo se puede rechazar una propuesta enviada.";
    private const string EsperandoAprobacion = "La propuesta está esperando aprobación: no se puede modificar.";

    private readonly List<Franja> _franjas = [];

    public long Id { get; private set; }
    public long MedicoId { get; private set; }                   // tipo de Medico.Id (3.2): long
    public int DuracionTurnoMinutos { get; private set; }
    public DateOnly VigenteDesde { get; private set; }
    public EstadoPropuesta Estado { get; private set; }
    public string? MotivoRechazo { get; private set; }
    public DateTimeOffset? EnviadaEl { get; private set; }       // marcas técnicas, en UTC
    public DateTimeOffset? AprobadaEl { get; private set; }
    public DateTimeOffset? RechazadaEl { get; private set; }
    public IReadOnlyList<Franja> Franjas => _franjas;

    private AgendaMedico() { }                                   // para EF

    public AgendaMedico(long medicoId, int duracionTurnoMinutos, DateOnly vigenteDesde)
    {
        ValidarDuracion(duracionTurnoMinutos);
        (MedicoId, DuracionTurnoMinutos, VigenteDesde) = (medicoId, duracionTurnoMinutos, vigenteDesde);
        Estado = EstadoPropuesta.Borrador;
    }

    public void CambiarDuracion(int minutos)
    {
        AsegurarEditable();
        ValidarDuracion(minutos);
        DuracionTurnoMinutos = minutos;
        CorregirSiEstabaRechazada();
    }

    /// <summary>Los 7 días no se miran acá: se miran al enviar (S-31 los cuenta desde el envío).</summary>
    public void CambiarVigenteDesde(DateOnly fecha)
    {
        AsegurarEditable();
        VigenteDesde = fecha;
        CorregirSiEstabaRechazada();
    }

    public void AgregarFranja(Franja franja)
    {
        AsegurarEditable();
        var pisada = _franjas.FirstOrDefault(f => f.SeSuperponeCon(franja));
        if (pisada is not null)
            throw new ReglaDeNegocioException(
                $"La franja del {franja.NombreDelDia} de {franja.Tramo} se superpone con la de {pisada.Tramo}.");
        _franjas.Add(franja);
        CorregirSiEstabaRechazada();
    }

    public void QuitarFranja(DayOfWeek dia, TimeOnly desde)
    {
        AsegurarEditable();
        var franja = _franjas.FirstOrDefault(f => f.Dia == dia && f.Desde == desde)
            ?? throw new NoEncontradoException("Esa franja no está en la agenda.");
        _franjas.Remove(franja);
        CorregirSiEstabaRechazada();
    }

    public void Enviar(TimeProvider reloj)
    {
        switch (Estado)
        {
            case EstadoPropuesta.Enviada: throw new ConflictoException(YaEnviada);
            case EstadoPropuesta.Rechazada: throw new ConflictoException(CorregirAntes);
            case EstadoPropuesta.Aprobada: throw new ConflictoException(AprobadaNoSeModifica);
        }
        if (_franjas.Count == 0)
            throw new ReglaDeNegocioException("Agregá al menos una franja antes de enviar la propuesta.");
        var minima = Hoy(reloj).AddDays(DiasMinimosDeAnticipacion);
        if (VigenteDesde < minima)
            throw new ReglaDeNegocioException(
                $"La agenda tiene que regir desde el {Fecha(minima)} o después: {DiasMinimosDeAnticipacion} días desde hoy.");
        Estado = EstadoPropuesta.Enviada;
        EnviadaEl = reloj.GetUtcNow();
        MotivoRechazo = null;
    }

    public void Rechazar(string motivo, TimeProvider reloj)
    {
        if (Estado != EstadoPropuesta.Enviada) throw new ConflictoException(SoloRechazarEnviada);
        var recortado = motivo?.Trim();
        if (string.IsNullOrEmpty(recortado))
            throw new ReglaDeNegocioException("Escribí el motivo del rechazo.");
        if (recortado.Length > LargoMaximoMotivo)
            throw new ReglaDeNegocioException($"El motivo tiene como máximo {LargoMaximoMotivo} caracteres.");
        Estado = EstadoPropuesta.Rechazada;
        MotivoRechazo = recortado;
        RechazadaEl = reloj.GetUtcNow();
    }

    /// <summary>Decisión 11 del docente: una propuesta cuya fecha de inicio ya pasó no se aprueba.</summary>
    public void Aprobar(TimeProvider reloj)
    {
        if (Estado != EstadoPropuesta.Enviada) throw new ConflictoException(SoloAprobarEnviada);
        if (VigenteDesde < Hoy(reloj))
            throw new ConflictoException(
                $"La agenda iba a regir desde el {Fecha(VigenteDesde)}, que ya pasó: rechazala para que el médico elija otra fecha.");
        Estado = EstadoPropuesta.Aprobada;
        AprobadaEl = reloj.GetUtcNow();
    }

    /// <summary>
    /// Los inicios de turno de la fecha (hora local del hospital, Kind = Unspecified), ordenados. Un turno que no
    /// entra completo en su franja no se ofrece (decisión 12). No mira estado ni vigencia: eso es QueRigeEn.
    /// </summary>
    public IReadOnlyList<DateTime> HorariosDelDia(DateOnly fecha)
    {
        var horarios = new List<DateTime>();
        foreach (var franja in _franjas.Where(f => f.Dia == fecha.DayOfWeek).OrderBy(f => f.Inicio))
            for (var inicio = franja.Inicio; inicio + DuracionTurnoMinutos <= franja.Fin; inicio += DuracionTurnoMinutos)
                horarios.Add(fecha.ToDateTime(TimeOnly.MinValue).AddMinutes(inicio));
        return horarios;
    }

    public bool AtiendeEn(DateTime fechaHora) => HorariosDelDia(DateOnly.FromDateTime(fechaHora)).Contains(fechaHora);

    public int TurnosPorSemana() => _franjas.Sum(f => f.DuracionMinutos / DuracionTurnoMinutos);

    /// <summary>
    /// La aprobada con la VigenteDesde más reciente que no sea posterior a la fecha; con la misma fecha, la que
    /// se aprobó después (Arq. §6, decisión 13). Ninguna → null: ese día el médico no ofrece turnos (FR-13).
    /// </summary>
    public static AgendaMedico? QueRigeEn(IEnumerable<AgendaMedico> agendas, DateOnly fecha) =>
        agendas.Where(a => a.Estado == EstadoPropuesta.Aprobada && a.VigenteDesde <= fecha)
               .OrderByDescending(a => a.VigenteDesde)
               .ThenByDescending(a => a.AprobadaEl)
               .FirstOrDefault();

    private void AsegurarEditable()
    {
        if (Estado == EstadoPropuesta.Enviada) throw new ConflictoException(EsperandoAprobacion);
        if (Estado == EstadoPropuesta.Aprobada) throw new ConflictoException(AprobadaNoSeModifica);
    }

    // Rechazada → Borrador: el médico corrige. El motivo queda a la vista hasta el próximo envío (UX M2).
    private void CorregirSiEstabaRechazada()
    {
        if (Estado == EstadoPropuesta.Rechazada)
            Estado = EstadoPropuesta.Borrador;
    }

    private static void ValidarDuracion(int minutos)
    {
        if (minutos is < DuracionMinima or > DuracionMaxima)
            throw new ReglaDeNegocioException(
                $"La duración del turno tiene que estar entre {DuracionMinima} y {DuracionMaxima} minutos.");
    }

    // La fecha del HOSPITAL, no la UTC: RelojHospital (y el FakeTimeProvider de las pruebas) traen Córdoba.
    private static DateOnly Hoy(TimeProvider reloj) => DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);

    private static string Fecha(DateOnly fecha) => fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
}
