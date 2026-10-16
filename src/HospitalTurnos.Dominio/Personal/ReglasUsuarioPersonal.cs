namespace HospitalTurnos.Dominio.Personal;

/// <summary>
/// Lo que tiene que cumplir el alta de un usuario del personal (story 2.2, AC 5). Lo usan la pantalla
/// (para el mensaje debajo de cada campo) y el caso de uso (para un POST armado a mano, NFR-10).
/// </summary>
public static class ReglasUsuarioPersonal
{
    public const int LargoMaximoNombre = 120;

    // Más estricto que Identity (que acepta @ y +): un usuario que se dicta y se tipea sin dudas.
    public const string PatronUsuario = "^[A-Za-z0-9._-]{3,100}$";

    public const string NombreVacio = "Ingresá el nombre completo.";
    public const string NombreLargo = "El nombre completo tiene como máximo 120 caracteres.";
    public const string UsuarioVacio = "Ingresá el nombre de usuario.";
    public const string UsuarioInvalido =
        "El usuario tiene entre 3 y 100 caracteres: letras sin acento, números, punto, guion o guion bajo.";
    public const string RolInvalido = "Elegí un rol.";
}
