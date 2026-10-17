using Microsoft.AspNetCore.Components;

namespace HospitalTurnos.Web.Components.Comunes;

/// <summary>
/// Una columna de TablaAbm (story 3.3, AC 27). Orden: la clave que se manda en la URL si la columna se puede
/// ordenar. Numero: alineada a la derecha. Visible: p. ej. el Estado de A2 sólo con el filtro "Todas" (AC 50).
/// </summary>
public sealed record ColumnaAbm<TFila>(string Titulo, Func<TFila, RenderFragment> Contenido, string? Orden = null,
    bool Numero = false, bool Visible = true);
