using HospitalTurnos.Dominio.Auditoria;

namespace HospitalTurnos.Aplicacion.Auditoria;

/// <summary>Quién hizo el cambio y por dónde. Lo arma quien llama (desde la sesión, story 2.1).</summary>
public sealed record ActorAuditoria(string? UsuarioId, string Quien, string Rol, CanalAuditoria Canal);
