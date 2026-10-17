namespace HospitalTurnos.Dominio.Catalogo;

/// <summary>
/// Un campo que cambió en una edición, con su valor antes y después como texto (story 3.2, AC 16). El
/// campo ya viene en camelCase: la auditoría lo guarda como clave de un diccionario, que no se convierte.
/// </summary>
public sealed record CambioDeDato(string Campo, string? Antes, string? Despues);
