using System.Security.Claims;

namespace HospitalTurnos.Aplicacion.Personal;

/// <summary>
/// Cambiar la propia contraseña (S-30): obligatorio en el primer ingreso con la temporal, voluntario
/// después. Si sale bien, trae el principal nuevo (sin la marca y con el sello nuevo) para la cookie.
/// </summary>
public interface ICambioContrasenaPersonal
{
    Task<ResultadoCambioContrasena> CambiarAsync(string usuarioId, string actual, string nueva, CancellationToken ct);
}

/// <summary>Errores: los textos para la pantalla, en castellano. Principal y Rol sólo si fue exitoso.</summary>
public sealed record ResultadoCambioContrasena(bool Exitoso, IReadOnlyList<string> Errores,
    ClaimsPrincipal? Principal = null, string? Rol = null);
