using HospitalTurnos.Aplicacion.Auditoria;

namespace HospitalTurnos.Aplicacion.Personal;

/// <summary>
/// La administración de usuarios del personal (pantalla A4, FR-10). Cada cambio confirma en la base que
/// quien actúa es un administrador activo, audita y guarda en una sola transacción. Nunca borra usuarios.
/// </summary>
public interface IGestionUsuariosPersonal
{
    Task<IReadOnlyList<UsuarioPersonalFila>> ListarAsync(CancellationToken ct);
    Task<AltaUsuarioPersonal> CrearAsync(NuevoUsuarioPersonal datos, ActorAuditoria actor, CancellationToken ct);
    Task DesactivarAsync(Guid usuarioId, ActorAuditoria actor, CancellationToken ct);
    Task ReactivarAsync(Guid usuarioId, ActorAuditoria actor, CancellationToken ct);
    Task DesbloquearAsync(Guid usuarioId, ActorAuditoria actor, CancellationToken ct);
}

public sealed record NuevoUsuarioPersonal(string? NombreCompleto, string? Usuario, string? Rol);

// La contraseña viaja sólo en este objeto, de vuelta a la página que la muestra una vez.
public sealed record AltaUsuarioPersonal(Guid Id, string Usuario, string Rol, string ContrasenaTemporal);

/// <summary>Rol: el único, "(sin rol)" o "(varios roles)". BloqueadoHasta: sólo si el bloqueo sigue vigente.</summary>
public sealed record UsuarioPersonalFila(Guid Id, string Usuario, string NombreCompleto, string Rol,
    bool Activo, DateTimeOffset? BloqueadoHasta, bool DebeCambiarContrasena);
