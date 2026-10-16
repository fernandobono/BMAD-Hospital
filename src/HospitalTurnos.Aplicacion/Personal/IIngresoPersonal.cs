using System.Security.Claims;

namespace HospitalTurnos.Aplicacion.Personal;

/// <summary>
/// El ingreso del personal con usuario y contraseña (FR-09). Decide el resultado y deja la auditoría
/// guardada; la cookie la emite Web sólo si el resultado es Exitoso.
/// </summary>
public interface IIngresoPersonal
{
    Task<ResultadoIngreso> ValidarAsync(string usuario, string contrasena, CancellationToken ct);

    /// <summary>
    /// La sesión sigue valiendo si el usuario existe, está activo, tiene un solo rol y su sello de
    /// seguridad es el mismo que cuando ingresó (AC 25).
    /// </summary>
    Task<bool> SesionVigenteAsync(string usuarioId, string? selloDeSeguridad, CancellationToken ct);
}

public enum EstadoIngreso
{
    Exitoso,
    CredencialesInvalidas,
    Bloqueado,
    Desactivado,
    SinRol,
    VariosRoles,
}

/// <summary>Principal y Rol sólo vienen en el ingreso exitoso.</summary>
public sealed record ResultadoIngreso(EstadoIngreso Estado, ClaimsPrincipal? Principal = null, string? Rol = null)
{
    public static ResultadoIngreso Exitoso(ClaimsPrincipal principal, string rol) => new(EstadoIngreso.Exitoso, principal, rol);

    public static ResultadoIngreso CredencialesInvalidas() => new(EstadoIngreso.CredencialesInvalidas);

    public static ResultadoIngreso Bloqueado() => new(EstadoIngreso.Bloqueado);

    public static ResultadoIngreso Desactivado() => new(EstadoIngreso.Desactivado);

    public static ResultadoIngreso SinRol() => new(EstadoIngreso.SinRol);

    public static ResultadoIngreso VariosRoles() => new(EstadoIngreso.VariosRoles);
}

public static class ClaimsPersonal
{
    /// <summary>El nombre que muestra el menú (Identity sólo trae el nombre de usuario).</summary>
    public const string NombreCompleto = "nombre_completo";
}
