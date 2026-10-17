using HospitalTurnos.Aplicacion.Comun;
using Microsoft.AspNetCore.WebUtilities;

namespace HospitalTurnos.Web.Components.Comunes;

/// <summary>Qué acepta la URL de un listado: los órdenes válidos (el primero, por defecto) y los valores de filtro.</summary>
public sealed record ReglasDeListado(string OrdenPorDefecto, IReadOnlyCollection<string> Ordenes,
    IReadOnlyCollection<string>? Agendas = null, IReadOnlyCollection<string>? Roles = null);

/// <summary>
/// Búsqueda, filtros, orden y página de un listado, ida y vuelta con la URL (guía, "Barra de filtros"; story 3.3,
/// AC 26): se puede compartir y volver con Atrás. Sólo van a la URL los que no están en su valor por defecto, y
/// un valor desconocido se ignora (AC 80): la página nunca falla por lo que alguien escribió en la dirección.
/// </summary>
public sealed record EstadoDeListado
{
    public const string EstadoPorDefecto = "activos";
    public const int TamanoPorDefecto = 25;
    public static readonly IReadOnlyList<string> Estados = ["activos", "inactivos", "todos"];
    public static readonly IReadOnlyList<int> Tamanos = [25, 50, 100];

    public string? Buscar { get; init; }
    public string Estado { get; init; } = EstadoPorDefecto;
    public long? Especialidad { get; init; }
    public string? Agenda { get; init; }
    public string? Rol { get; init; }
    public string Orden { get; init; } = "";
    public int Pagina { get; init; } = 1;
    public int Tamano { get; init; } = TamanoPorDefecto;

    public static EstadoDeListado PorDefecto(ReglasDeListado reglas) => new() { Orden = reglas.OrdenPorDefecto };

    /// <summary>"Limpiar filtros" (AC 24): búsqueda, filtros, orden y página a su defecto; el tamaño elegido queda.</summary>
    public EstadoDeListado Limpio(ReglasDeListado reglas) => PorDefecto(reglas) with { Tamano = Tamano };

    public bool TieneFiltros =>
        Buscar is not null || Estado != EstadoPorDefecto || Especialidad is not null || Agenda is not null || Rol is not null;

    public static EstadoDeListado Leer(string uri, ReglasDeListado reglas)
    {
        var consulta = QueryHelpers.ParseQuery(new Uri(uri, UriKind.Absolute).Query);
        string? Valor(string nombre) => consulta.TryGetValue(nombre, out var v) ? v.ToString() : null;

        var buscar = Valor("buscar")?.Trim();
        var estado = Valor("estado");
        var orden = Valor("orden");
        var campoDeOrden = orden?.TrimStart('-');
        return new EstadoDeListado
        {
            Buscar = string.IsNullOrEmpty(buscar) ? null : buscar,
            Estado = estado is not null && Estados.Contains(estado) ? estado : EstadoPorDefecto,
            Especialidad = long.TryParse(Valor("especialidad"), out var especialidad) && especialidad > 0 ? especialidad : null,
            Agenda = Elegido(Valor("agenda"), reglas.Agendas),
            Rol = Elegido(Valor("rol"), reglas.Roles),
            Orden = orden is not null && orden.Count(c => c == '-') <= 1 && reglas.Ordenes.Contains(campoDeOrden!) ? orden : reglas.OrdenPorDefecto,
            Pagina = int.TryParse(Valor("pagina"), out var pagina) && pagina >= 1 ? pagina : 1,
            Tamano = int.TryParse(Valor("tamano"), out var tamano) && Tamanos.Contains(tamano) ? tamano : TamanoPorDefecto,
        };
    }

    private static string? Elegido(string? valor, IReadOnlyCollection<string>? validos) =>
        valor is not null && validos is not null && validos.Contains(valor) ? valor : null;

    public string Url(string ruta, ReglasDeListado reglas)
    {
        var partes = new List<string>();
        void Agregar(string nombre, string? valor, bool esDefecto)
        {
            if (valor is not null && !esDefecto)
                partes.Add($"{nombre}={Uri.EscapeDataString(valor)}");
        }
        Agregar("buscar", Buscar, false);
        Agregar("estado", Estado, Estado == EstadoPorDefecto);
        Agregar("especialidad", Especialidad?.ToString(System.Globalization.CultureInfo.InvariantCulture), false);
        Agregar("agenda", Agenda, false);
        Agregar("rol", Rol, false);
        Agregar("orden", Orden, Orden == reglas.OrdenPorDefecto);
        Agregar("pagina", Pagina.ToString(System.Globalization.CultureInfo.InvariantCulture), Pagina == 1);
        Agregar("tamano", Tamano.ToString(System.Globalization.CultureInfo.InvariantCulture), Tamano == TamanoPorDefecto);
        return partes.Count == 0 ? ruta : $"{ruta}?{string.Join('&', partes)}";
    }

    /// <summary>Lo que el caso de uso necesita para la consulta paginada (AC 42).</summary>
    public ConsultaDeListado AConsulta()
    {
        var filtros = new Dictionary<string, string> { ["estado"] = Estado };
        if (Especialidad is { } especialidad)
            filtros["especialidad"] = especialidad.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (Agenda is not null)
            filtros["agenda"] = Agenda;
        if (Rol is not null)
            filtros["rol"] = Rol;
        return new ConsultaDeListado(Buscar, filtros, Orden, Pagina, Tamano);
    }

    /// <summary>Los filtros aplicados en palabras, para mostrarlos junto a "Limpiar filtros" (AC 24).</summary>
    public IReadOnlyList<string> FiltrosActivos(Func<long, string?> nombreDeEspecialidad, bool femenino = false,
        Func<string, string>? etiquetaDeAgenda = null, Func<string, string>? etiquetaDeRol = null)
    {
        var activos = new List<string>();
        if (Buscar is not null)
            activos.Add($"Búsqueda: {Buscar}");
        if (Estado != EstadoPorDefecto)
            activos.Add($"Estado: {FiltroDeEstado.Etiqueta(Estado, femenino)}");
        if (Especialidad is { } especialidad)
            activos.Add($"Especialidad: {nombreDeEspecialidad(especialidad) ?? "la elegida"}");
        if (Agenda is not null)
            activos.Add($"Estado de agenda: {etiquetaDeAgenda?.Invoke(Agenda) ?? Agenda}");
        if (Rol is not null)
            activos.Add($"Rol: {etiquetaDeRol?.Invoke(Rol) ?? Rol}");
        return activos;
    }
}
