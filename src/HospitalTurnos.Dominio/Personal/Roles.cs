namespace HospitalTurnos.Dominio.Personal;

/// <summary>
/// Los roles del personal (R-13). Cada usuario tiene exactamente uno (PRD FR-09). El texto es el que
/// guarda Identity y el que queda en la columna "rol" de la auditoría: no se traduce ni se acentúa.
/// </summary>
public static class Roles
{
    public const string Recepcionista = "Recepcionista";
    public const string Medico = "Medico";
    public const string Administrador = "Administrador";

    public static readonly IReadOnlyList<string> Todos = [Recepcionista, Medico, Administrador];
}
