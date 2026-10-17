namespace HospitalTurnos.Web.Components.Comunes;

/// <summary>
/// Un nivel de las migas de pan (story 3.3, AC 19). Ruta: a dónde lleva el enlace; el último nivel no la usa. El
/// rótulo de un grupo del menú ("Catálogos") lleva a la primera sección del grupo.
/// </summary>
public sealed record Miga(string Texto, string? Ruta);
