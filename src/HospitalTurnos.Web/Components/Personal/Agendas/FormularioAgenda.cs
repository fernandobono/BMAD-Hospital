using System.ComponentModel.DataAnnotations;
using System.Globalization;
using HospitalTurnos.Aplicacion.Agendas;
using HospitalTurnos.Dominio.Agendas;
using HospitalTurnos.Dominio.Excepciones;

namespace HospitalTurnos.Web.Components.Personal.Agendas;

/// <summary>
/// El formulario de una agenda (story 4.2): lo usan M2 y, heredándolo, la corrección de urgencia de la 4.4 (por eso no
/// es sealed). Las reglas son las del dominio: la revisión por fila arma una AgendaMedico que nunca se guarda y le pasa
/// cada franja, así el texto en rojo es el mismo del cartel al guardar y el número de turnos sale de TurnosPorSemana().
/// </summary>
public class FormularioAgenda
{
    public const string AccionDesconocida = "No reconocemos esa acción. Recargá la página.";
    private const string QuitarFranja = "quitar-franja:";

    [Required(ErrorMessage = "Ingresá la duración del turno.")]
    [Range(AgendaMedico.DuracionMinima, AgendaMedico.DuracionMaxima,
        ErrorMessage = "La duración del turno tiene que estar entre 10 y 60 minutos.")]
    public int? DuracionTurnoMinutos { get; set; }

    [Required(ErrorMessage = "Elegí desde qué fecha rige.")]
    public DateOnly? VigenteDesde { get; set; }

    // Con la tabla vacía el navegador no manda ninguna Input.Franjas[i] y el formulario SSR la asigna en null: acá
    // queda vacía, nunca nula.
    [FilasCompletas]
    public List<FilaFranja> Franjas { get => franjas; set => franjas = value ?? []; }
    private List<FilaFranja> franjas = [];

    /// <summary>El botón pulsado (Input.Accion).</summary>
    public string? Accion { get; set; }

    /// <summary>
    /// Los botones del editor (AC 8): true si era uno de ellos, ya aplicado; false si la acción es de la página.
    /// Un "quitar" de una fila que no existe es un pedido armado a mano: ReglaDeNegocioException.
    /// </summary>
    public bool AplicarAccionDeEdicion()
    {
        switch (Accion)
        {
            case "actualizar":
                return true;
            case "agregar-franja":
                Franjas.Add(new FilaFranja());
                return true;
            case { } accion when accion.StartsWith(QuitarFranja, StringComparison.Ordinal):
                if (!int.TryParse(accion[QuitarFranja.Length..], NumberStyles.None, CultureInfo.InvariantCulture, out var fila)
                    || fila >= Franjas.Count)
                    throw new ReglaDeNegocioException(AccionDesconocida);
                Franjas.RemoveAt(fila);
                return true;
            default:
                return false;
        }
    }

    /// <summary>Problemas y avisos por fila, y los turnos por semana (null si algo impide calcularlos).</summary>
    public RevisionDeAgenda Revisar()
    {
        var duracionValida = DuracionTurnoMinutos is >= AgendaMedico.DuracionMinima and <= AgendaMedico.DuracionMaxima;
        // Si la duración no es válida igual se revisan las franjas, pero no se da el número.
        var agenda = new AgendaMedico(0, duracionValida ? DuracionTurnoMinutos!.Value : AgendaMedico.DuracionMinima,
            VigenteDesde ?? DateOnly.MinValue);
        var problemas = new Dictionary<int, string>();
        var avisos = new Dictionary<int, string>();
        for (var i = 0; i < Franjas.Count; i++)
        {
            if (Franjas[i] is not { Desde: { } desde, Hasta: { } hasta } fila)
            {
                problemas[i] = "Completá la hora desde y la hora hasta, o quitá la franja.";
                continue;
            }
            try
            {
                var franja = new Franja(fila.Dia, desde, hasta);
                agenda.AgregarFranja(franja);
                var sobran = franja.DuracionMinutos % agenda.DuracionTurnoMinutos;
                if (duracionValida && sobran != 0)
                    avisos[i] = $"Los últimos {sobran} minutos de esta franja no se usan: no entra un turno completo.";
            }
            catch (ReglaDeNegocioException e)
            {
                problemas[i] = e.Message;
            }
        }
        var turnos = duracionValida && problemas.Count == 0 && Franjas.Count > 0 ? agenda.TurnosPorSemana() : (int?)null;
        return new RevisionDeAgenda(problemas, avisos, turnos, Franjas.Count == 0);
    }

    /// <summary>Los datos para el caso de uso; sólo después de validar.</summary>
    public DatosAgenda ADatos() => new(DuracionTurnoMinutos!.Value, VigenteDesde!.Value,
        [.. Franjas.Select(f => new DatosFranja(f.Dia, f.Desde!.Value, f.Hasta!.Value))]);

    /// <summary>Las filas de una agenda guardada, de lunes a domingo y en cada día por hora.</summary>
    public static FormularioAgenda Desde(AgendaMedico agenda) => new()
    {
        DuracionTurnoMinutos = agenda.DuracionTurnoMinutos,
        VigenteDesde = agenda.VigenteDesde,
        Franjas = [.. agenda.Franjas.OrderBy(f => DeLunesADomingo(f.Dia)).ThenBy(f => f.Desde)
            .Select(f => new FilaFranja { Dia = f.Dia, Desde = f.Desde, Hasta = f.Hasta })],
    };

    public static FormularioAgenda Nueva(DateOnly vigenteDesde) => new() { DuracionTurnoMinutos = 20, VigenteDesde = vigenteDesde };

    /// <summary>Lunes = 0 … domingo = 6 (DayOfWeek empieza en domingo).</summary>
    public static int DeLunesADomingo(DayOfWeek dia) => ((int)dia + 6) % 7;

    // Un control del campo y no de todo el formulario: así sale junto con los de duración y fecha (AC 11). Un
    // IValidatableObject sólo corre cuando los demás campos ya están bien.
    private sealed class FilasCompletasAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? valor, ValidationContext contexto) =>
            valor is IEnumerable<FilaFranja> filas && filas.Any(f => f.Desde is null || f.Hasta is null)
                ? new ValidationResult("Completá la hora desde y la hora hasta de cada franja, o quitá las que sobran.",
                    [contexto.MemberName ?? nameof(Franjas)])
                : ValidationResult.Success;
    }
}

/// <summary>Una fila de la tabla de franjas. Las horas son opcionales mientras se escribe.</summary>
public sealed class FilaFranja
{
    public DayOfWeek Dia { get; set; } = DayOfWeek.Monday;
    public TimeOnly? Desde { get; set; }
    public TimeOnly? Hasta { get; set; }
}

/// <summary>Lo que el editor muestra en cada dibujo (AC 9 y 10).</summary>
public sealed record RevisionDeAgenda(
    IReadOnlyDictionary<int, string> Problemas,   // fila → texto (marca roja)
    IReadOnlyDictionary<int, string> Avisos,      // fila → "Los últimos N minutos…" (sin rojo)
    int? TurnosPorSemana,                         // null con problemas, duración inválida o sin franjas
    bool SinFranjas);
