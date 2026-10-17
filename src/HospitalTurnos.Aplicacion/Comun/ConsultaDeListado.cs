namespace HospitalTurnos.Aplicacion.Comun;

/// <summary>
/// Lo que pide un listado paginado (story 3.3, AC 42): búsqueda, filtros por nombre ("estado", "especialidad",
/// "agenda", "rol"), orden ("medico" ascendente, "-medico" descendente), página desde 1 y tamaño. La paginación,
/// el orden y el conteo se hacen en la base. Una página mayor que la última devuelve la última (AC 81).
/// </summary>
public sealed record ConsultaDeListado(
    string? Buscar, IReadOnlyDictionary<string, string> Filtros, string Orden, int Pagina, int Tamano)
{
    public bool Descendente => Orden.StartsWith('-');

    /// <summary>El campo del orden, sin el signo.</summary>
    public string CampoDeOrden => Orden.TrimStart('-');

    public string? Filtro(string nombre) => Filtros.TryGetValue(nombre, out var valor) ? valor : null;

    /// <summary>La página que se puede mostrar con ese total: entre 1 y la última con datos.</summary>
    public int PaginaPosible(int total)
    {
        var ultima = Math.Max(1, (int)Math.Ceiling(total / (double)Tamano));
        return Math.Clamp(Pagina, 1, ultima);
    }

    /// <summary>Cuántos registros saltear para esa página (ya ajustada al total).</summary>
    public int Saltear(int total) => (PaginaPosible(total) - 1) * Tamano;
}

/// <summary>Una página de resultados y el total del filtro aplicado (el "N" de "Médicos · N").</summary>
public sealed record PaginaDeResultados<T>(IReadOnlyList<T> Items, int Total);
