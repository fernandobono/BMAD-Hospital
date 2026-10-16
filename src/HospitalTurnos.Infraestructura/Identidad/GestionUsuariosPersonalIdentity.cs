using System.Text.RegularExpressions;
using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Personal;
using HospitalTurnos.Dominio.Auditoria;
using HospitalTurnos.Dominio.Excepciones;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HospitalTurnos.Infraestructura.Identidad;

/// <summary>
/// A4 sobre Identity. El UserStore guarda en cada operación sobre el HospitalDbContext del alcance: con la
/// transacción abierta en ese mismo contexto, el usuario, su rol y la auditoría se guardan juntos o nada.
/// </summary>
public sealed class GestionUsuariosPersonalIdentity(
    UserManager<UsuarioPersonal> usuarios,
    HospitalDbContext contexto,
    IAuditoria auditoria,
    IActorVigente actorVigente,
    IdentityErrorDescriber errores,
    TimeProvider reloj) : IGestionUsuariosPersonal
{
    private const long CandadoAdministradores = 2_200_001;   // número fijo: sólo identifica este candado
    private const string SinPermiso = "No tenés permiso para administrar usuarios.";
    private const string EntidadTipo = "UsuarioPersonal";
    private const string SinRolTexto = "(sin rol)";
    private const string VariosRolesTexto = "(varios roles)";
    private const string IndiceNombreDeUsuario = "user_name_index";   // migración IdentidadPersonal (2.1)

    public async Task<IReadOnlyList<UsuarioPersonalFila>> ListarAsync(CancellationToken ct)
    {
        // El hospital tiene decenas de usuarios, no miles: dos consultas y el armado en memoria.
        var lista = await contexto.Users.AsNoTracking()
            .OrderBy(u => u.NombreCompleto).ThenBy(u => u.UserName)
            .ToListAsync(ct);
        var roles = await (from ur in contexto.UserRoles.AsNoTracking()
                           join r in contexto.Roles.AsNoTracking() on ur.RoleId equals r.Id
                           select new { ur.UserId, r.Name }).ToListAsync(ct);
        var rolesPorUsuario = roles.ToLookup(x => x.UserId, x => x.Name!);
        var ahora = reloj.GetUtcNow();
        return
        [
            .. lista.Select(u =>
            {
                var suyos = rolesPorUsuario[u.Id].ToList();
                var rol = suyos.Count switch { 0 => SinRolTexto, 1 => suyos[0], _ => VariosRolesTexto };
                return new UsuarioPersonalFila(u.Id, u.UserName!, u.NombreCompleto, rol, u.Activo,
                    u.LockoutEnd > ahora ? u.LockoutEnd : null, u.DebeCambiarContrasena);
            }),
        ];
    }

    public async Task<AltaUsuarioPersonal> CrearAsync(NuevoUsuarioPersonal datos, ActorAuditoria actor, CancellationToken ct)
    {
        // NFR-10: la misma validación que la pantalla, para un POST armado a mano. Antes de la transacción.
        var nombre = datos.NombreCompleto?.Trim() ?? "";
        var usuarioTipeado = datos.Usuario?.Trim() ?? "";
        if (nombre.Length == 0)
            throw new ReglaDeNegocioException(ReglasUsuarioPersonal.NombreVacio);
        if (nombre.Length > ReglasUsuarioPersonal.LargoMaximoNombre)
            throw new ReglaDeNegocioException(ReglasUsuarioPersonal.NombreLargo);
        if (usuarioTipeado.Length == 0)
            throw new ReglaDeNegocioException(ReglasUsuarioPersonal.UsuarioVacio);
        if (!Regex.IsMatch(usuarioTipeado, ReglasUsuarioPersonal.PatronUsuario))
            throw new ReglaDeNegocioException(ReglasUsuarioPersonal.UsuarioInvalido);
        if (datos.Rol is null || !Roles.Todos.Contains(datos.Rol))
            throw new ReglaDeNegocioException(ReglasUsuarioPersonal.RolInvalido);
        // Identity ya compara sin mayúsculas; guardarlo en minúsculas evita "Ana.Ruiz" y "ana.ruiz" en la tabla.
        var nombreDeUsuario = usuarioTipeado.ToLowerInvariant();

        try
        {
            await using var tx = await contexto.Database.BeginTransactionAsync(ct);
            var admin = await actorVigente.ExigirAdministradorActivoAsync(actor, SinPermiso, ct);   // AC 15
            var temporal = ContrasenaTemporal.Generar();
            var usuario = new UsuarioPersonal
            {
                UserName = nombreDeUsuario,
                NombreCompleto = nombre,
                Activo = true,
                DebeCambiarContrasena = true,
            };
            var creado = await usuarios.CreateAsync(usuario, temporal);
            if (creado.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.DuplicateUserName)))
                throw new ConflictoException(errores.DuplicateUserName(nombreDeUsuario).Description);   // AC 6
            Asegurar(creado);
            Asegurar(await usuarios.AddToRoleAsync(usuario, datos.Rol));
            auditoria.Registrar(admin, AccionesAuditoria.UsuarioPersonalCreado, EntidadTipo, usuario.Id.ToString(),
                new { usuario = nombreDeUsuario, nombreCompleto = nombre, rol = datos.Rol });   // nunca la contraseña
            await contexto.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return new AltaUsuarioPersonal(usuario.Id, nombreDeUsuario, datos.Rol, temporal);
        }
        // Dos altas iguales a la vez: Identity no ve el duplicado y lo rechaza el índice único (AC 6). Sólo
        // ese índice: otro choque de unicidad no es "usuario repetido" y sigue al manejador global.
        catch (DbUpdateException e) when (e.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: IndiceNombreDeUsuario,
        })
        {
            throw new ConflictoException(errores.DuplicateUserName(nombreDeUsuario).Description);
        }
    }

    public async Task DesactivarAsync(Guid usuarioId, ActorAuditoria actor, CancellationToken ct)
    {
        await using var tx = await contexto.Database.BeginTransactionAsync(ct);
        // Primero el candado, DESPUÉS leer actor y objetivo: así el segundo de dos pedidos simultáneos ve
        // lo que confirmó el primero (AC 10).
        await contexto.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({CandadoAdministradores})", ct);
        var admin = await actorVigente.ExigirAdministradorActivoAsync(actor, SinPermiso, ct);  // AC 15
        var usuario = await BuscarAsync(usuarioId);                                            // AC 16
        if (usuario.Id == Guid.Parse(admin.UsuarioId!))
            throw new ReglaDeNegocioException("No podés desactivar tu propio usuario. Pedíselo a otro administrador.");
        if (!usuario.Activo)
            throw new ConflictoException("Ese usuario ya estaba desactivado.");
        usuario.Activo = false;
        // Guarda Activo y el sello nuevo juntos: la revalidación de 2.1 corta su sesión (AC 8).
        Asegurar(await usuarios.UpdateSecurityStampAsync(usuario));
        auditoria.Registrar(admin, AccionesAuditoria.UsuarioPersonalDesactivado, EntidadTipo,
            usuario.Id.ToString(), new { usuario = usuario.UserName });
        await contexto.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task ReactivarAsync(Guid usuarioId, ActorAuditoria actor, CancellationToken ct)
    {
        await using var tx = await contexto.Database.BeginTransactionAsync(ct);
        var admin = await actorVigente.ExigirAdministradorActivoAsync(actor, SinPermiso, ct);
        var usuario = await BuscarAsync(usuarioId);
        if (usuario.Activo)
            throw new ConflictoException("Ese usuario ya estaba activo.");
        usuario.Activo = true;   // sin tocar contraseña, bloqueo ni marca (AC 11)
        Asegurar(await usuarios.UpdateAsync(usuario));
        auditoria.Registrar(admin, AccionesAuditoria.UsuarioPersonalReactivado, EntidadTipo,
            usuario.Id.ToString(), new { usuario = usuario.UserName });
        await contexto.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task DesbloquearAsync(Guid usuarioId, ActorAuditoria actor, CancellationToken ct)
    {
        await using var tx = await contexto.Database.BeginTransactionAsync(ct);
        var admin = await actorVigente.ExigirAdministradorActivoAsync(actor, SinPermiso, ct);
        var usuario = await BuscarAsync(usuarioId);
        if (!(usuario.LockoutEnd > reloj.GetUtcNow()))
            throw new ConflictoException("Ese usuario no está bloqueado.");
        Asegurar(await usuarios.SetLockoutEndDateAsync(usuario, null));
        Asegurar(await usuarios.ResetAccessFailedCountAsync(usuario));
        auditoria.Registrar(admin, AccionesAuditoria.UsuarioPersonalDesbloqueado, EntidadTipo,
            usuario.Id.ToString(), new { usuario = usuario.UserName });
        await contexto.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private async Task<UsuarioPersonal> BuscarAsync(Guid id) =>
        await usuarios.FindByIdAsync(id.ToString())
        ?? throw new NoEncontradoException("No encontramos ese usuario. Recargá la página.");

    // Un resultado fallido de Identity no se sigue como si nada: llega al manejador global como error.
    private static void Asegurar(IdentityResult resultado)
    {
        if (!resultado.Succeeded)
            throw new InvalidOperationException(
                $"Identity no pudo actualizar el usuario del personal: {string.Join(" ", resultado.Errors.Select(e => $"{e.Code} {e.Description}".Trim()))}");
    }
}
