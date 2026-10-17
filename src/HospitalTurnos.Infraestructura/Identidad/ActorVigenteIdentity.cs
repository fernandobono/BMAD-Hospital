using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Personal;
using HospitalTurnos.Dominio.Auditoria;
using HospitalTurnos.Dominio.Excepciones;
using HospitalTurnos.Dominio.Personal;
using Microsoft.AspNetCore.Identity;

namespace HospitalTurnos.Infraestructura.Identidad;

/// <summary>
/// UserManager trabaja sobre el HospitalDbContext del alcance: llamado dentro de una transacción abierta
/// en ese contexto (después del candado de DesactivarAsync), lee lo que ya confirmó el otro pedido.
/// </summary>
public sealed class ActorVigenteIdentity(UserManager<UsuarioPersonal> usuarios) : IActorVigente
{
    public async Task<ActorAuditoria> ExigirActivoConRolAsync(ActorAuditoria actor, string rol, string mensajeSiNoLoEs,
        CancellationToken ct)
    {
        var usuario = Guid.TryParse(actor.UsuarioId, out var id) ? await usuarios.FindByIdAsync(id.ToString()) : null;
        // Un rol por usuario (FR-09): con dos roles tampoco pasa.
        if (usuario is null || !usuario.Activo || await usuarios.GetRolesAsync(usuario) is not [var unico] || unico != rol)
            throw new AccesoDenegadoException(mensajeSiNoLoEs);
        return new(usuario.Id.ToString(), usuario.NombreCompleto, rol, CanalAuditoria.Personal);
    }

    public Task<ActorAuditoria> ExigirAdministradorActivoAsync(ActorAuditoria actor, string mensajeSiNoLoEs, CancellationToken ct) =>
        ExigirActivoConRolAsync(actor, Roles.Administrador, mensajeSiNoLoEs, ct);
}
