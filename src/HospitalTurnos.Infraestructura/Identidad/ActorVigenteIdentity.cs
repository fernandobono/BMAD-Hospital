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
    public async Task<ActorAuditoria> ExigirAdministradorActivoAsync(ActorAuditoria actor, string mensajeSiNoLoEs, CancellationToken ct)
    {
        var admin = Guid.TryParse(actor.UsuarioId, out var id) ? await usuarios.FindByIdAsync(id.ToString()) : null;
        if (admin is null || !admin.Activo || await usuarios.GetRolesAsync(admin) is not [Roles.Administrador])
            throw new AccesoDenegadoException(mensajeSiNoLoEs);
        return new(admin.Id.ToString(), admin.NombreCompleto, Roles.Administrador, CanalAuditoria.Personal);
    }
}
