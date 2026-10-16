using Microsoft.AspNetCore.Identity;

namespace HospitalTurnos.Infraestructura.Identidad;

/// <summary>
/// Los errores de Identity que puede ver el personal, en castellano (story 2.2, AC 24). Los demás quedan
/// con el texto en inglés de Identity: no llegan a ninguna pantalla.
/// </summary>
public sealed class DescriptorErroresIdentity : IdentityErrorDescriber
{
    public override IdentityError PasswordTooShort(int length) =>
        Error(nameof(PasswordTooShort), $"La contraseña tiene que tener al menos {length} caracteres.");

    public override IdentityError PasswordRequiresUpper() =>
        Error(nameof(PasswordRequiresUpper), "La contraseña tiene que tener al menos una mayúscula.");

    public override IdentityError PasswordRequiresLower() =>
        Error(nameof(PasswordRequiresLower), "La contraseña tiene que tener al menos una minúscula.");

    public override IdentityError PasswordRequiresDigit() =>
        Error(nameof(PasswordRequiresDigit), "La contraseña tiene que tener al menos un número.");

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        Error(nameof(PasswordRequiresNonAlphanumeric), "La contraseña tiene que tener al menos un símbolo, como - o #.");

    public override IdentityError PasswordMismatch() =>
        Error(nameof(PasswordMismatch), "La contraseña actual no es correcta.");

    public override IdentityError DuplicateUserName(string userName) =>
        Error(nameof(DuplicateUserName), $"Ya existe un usuario con el nombre {userName}. Elegí otro.");

    public override IdentityError InvalidUserName(string? userName) =>
        Error(nameof(InvalidUserName), $"El usuario {userName} tiene caracteres no permitidos.");

    private static IdentityError Error(string codigo, string descripcion) => new() { Code = codigo, Description = descripcion };
}
