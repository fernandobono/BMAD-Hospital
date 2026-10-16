using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Personal;
using HospitalTurnos.Dominio.Auditoria;
using HospitalTurnos.Dominio.Excepciones;
using HospitalTurnos.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Identity;

namespace HospitalTurnos.Infraestructura.Identidad;

/// <summary>
/// El cambio de la propia contraseña. ChangePasswordAsync de Identity verifica la actual, valida la nueva
/// y cambia el hash y el sello de seguridad; una actual equivocada NO suma intentos de bloqueo (la
/// pantalla exige sesión abierta y bloquear no cortaría esa sesión).
/// </summary>
public sealed class CambioContrasenaPersonalIdentity(
    UserManager<UsuarioPersonal> usuarios,
    HospitalDbContext contexto,
    IAuditoria auditoria,
    FabricaPrincipalPersonal fabricaDePrincipal) : ICambioContrasenaPersonal
{
    public async Task<ResultadoCambioContrasena> CambiarAsync(string usuarioId, string actual, string nueva, CancellationToken ct)
    {
        var usuario = await usuarios.FindByIdAsync(usuarioId);
        var roles = usuario is null ? [] : await usuarios.GetRolesAsync(usuario);
        if (usuario is null || !usuario.Activo || roles.Count != 1)
            throw new AccesoDenegadoException("Tu sesión ya no es válida. Volvé a ingresar.");
        if (nueva == actual)
            return Fallido(["La nueva contraseña tiene que ser distinta de la actual."]);

        await using var tx = await contexto.Database.BeginTransactionAsync(ct);
        var cambio = await usuarios.ChangePasswordAsync(usuario, actual, nueva);   // también cambia el sello
        if (!cambio.Succeeded)
            return Fallido([.. cambio.Errors.Select(e => e.Description)]);          // sin Commit: no queda nada
        var motivo = usuario.DebeCambiarContrasena ? "primer-ingreso" : "voluntario";
        usuario.DebeCambiarContrasena = false;
        auditoria.Registrar(new ActorAuditoria(usuario.Id.ToString(), usuario.NombreCompleto, roles[0], CanalAuditoria.Personal),
            AccionesAuditoria.ContrasenaPersonalCambiada, "UsuarioPersonal", usuario.Id.ToString(), new { motivo });
        await contexto.SaveChangesAsync(ct);   // guarda la marca y la auditoría
        await tx.CommitAsync(ct);
        return new(true, [], await fabricaDePrincipal.CrearAsync(usuario), roles[0]);
    }

    private static ResultadoCambioContrasena Fallido(IReadOnlyList<string> errores) => new(false, errores);
}
