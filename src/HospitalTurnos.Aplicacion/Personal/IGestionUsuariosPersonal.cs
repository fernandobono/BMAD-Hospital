using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Comun;

namespace HospitalTurnos.Aplicacion.Personal;

/// <summary>
/// La administración de usuarios del personal (pantalla A4, FR-10). Cada cambio confirma en la base que
/// quien actúa es un administrador activo, audita y guarda en una sola transacción. Nunca borra usuarios.
/// </summary>
public interface IGestionUsuariosPersonal
{
    Task<IReadOnlyList<UsuarioPersonalFila>> ListarAsync(CancellationToken ct);

    /// <summary>
    /// A4 paginada en la base (story 3.3, AC 45): busca en usuario o nombre, filtra por "rol" y "estado" (activos,
    /// inactivos, todos) y ordena por "nombre" o "rol".
    /// </summary>
    Task<PaginaDeResultados<UsuarioPersonalFila>> ListarPaginadoAsync(ConsultaDeListado consulta, CancellationToken ct);

    Task<AltaUsuarioPersonal> CrearAsync(NuevoUsuarioPersonal datos, ActorAuditoria actor, CancellationToken ct);
    Task DesactivarAsync(Guid usuarioId, ActorAuditoria actor, CancellationToken ct);
    Task ReactivarAsync(Guid usuarioId, ActorAuditoria actor, CancellationToken ct);
    Task DesbloquearAsync(Guid usuarioId, ActorAuditoria actor, CancellationToken ct);

    /// Genera otra contraseña temporal para quien olvidó la suya (story 2.3). La anterior deja de servir.
    Task<ContrasenaRestablecida> RestablecerContrasenaAsync(Guid usuarioId, ActorAuditoria actor, CancellationToken ct);
}

public sealed record NuevoUsuarioPersonal(string? NombreCompleto, string? Usuario, string? Rol);

// La contraseña viaja sólo en este objeto, de vuelta a la página que la muestra una vez.
public sealed record AltaUsuarioPersonal(Guid Id, string Usuario, string Rol, string ContrasenaTemporal);

// La contraseña viaja sólo en este objeto, de vuelta a la página que la muestra una vez (como AltaUsuarioPersonal).
public sealed record ContrasenaRestablecida(Guid Id, string Usuario, string ContrasenaTemporal, bool Desbloqueado);

/// <summary>Rol: el único, "(sin rol)" o "(varios roles)". BloqueadoHasta: sólo si el bloqueo sigue vigente.</summary>
public sealed record UsuarioPersonalFila(Guid Id, string Usuario, string NombreCompleto, string Rol,
    bool Activo, DateTimeOffset? BloqueadoHasta, bool DebeCambiarContrasena);
