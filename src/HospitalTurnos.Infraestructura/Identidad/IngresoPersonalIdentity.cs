using System.Security.Claims;
using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Personal;
using HospitalTurnos.Dominio.Auditoria;
using HospitalTurnos.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace HospitalTurnos.Infraestructura.Identidad;

/// <summary>
/// El ingreso paso a paso con UserManager (no SignInManager): así la decisión queda probada sin HTTP y
/// el esquema de la cookie lo elige Web. El orden de los pasos importa: el estado del usuario
/// (desactivado, sin rol) se informa sólo a quien ya probó conocer la contraseña.
/// </summary>
public sealed class IngresoPersonalIdentity(
    UserManager<UsuarioPersonal> usuarios,
    IPasswordHasher<UsuarioPersonal> hasher,
    IUserClaimsPrincipalFactory<UsuarioPersonal> fabricaDePrincipal,
    IAuditoria auditoria,
    HospitalDbContext contexto,
    ILogger<IngresoPersonalIdentity> registro) : IIngresoPersonal
{
    private const string EntidadTipo = "UsuarioPersonal";
    private const string SinRolTexto = "(sin rol)";
    private const string VariosRolesTexto = "(varios roles)";

    // Para que el tiempo de respuesta no delate que un usuario no existe: se verifica igual contra este
    // hash (mismo hasher, mismo costo PBKDF2). Se calcula una vez; la frase no es credencial de nada.
    private static readonly UsuarioPersonal UsuarioFicticio = new() { UserName = "ficticio" };
    private static string? _hashFicticio;

    public async Task<ResultadoIngreso> ValidarAsync(string usuario, string contrasena, CancellationToken ct)
    {
        // 1. Buscar. Si no existe, NO se audita: registros_auditoria es inmutable y es común tipear la
        //    contraseña en el campo usuario. Va al registro técnico sin lo tipeado.
        var encontrado = await usuarios.FindByNameAsync(usuario);
        if (encontrado is null)
        {
            var hash = LazyInitializer.EnsureInitialized(ref _hashFicticio,
                () => hasher.HashPassword(UsuarioFicticio, "frase fija para igualar el costo del ingreso"));
            hasher.VerifyHashedPassword(UsuarioFicticio, hash, contrasena);   // el resultado no importa
            registro.LogWarning("Ingreso del personal fallido: el usuario no existe.");
            return ResultadoIngreso.CredencialesInvalidas();
        }

        // 2. El actor de la auditoría, con su rol tal como está hoy.
        var roles = await usuarios.GetRolesAsync(encontrado);
        var rolTexto = roles.Count switch { 0 => SinRolTexto, 1 => roles[0], _ => VariosRolesTexto };
        var actor = new ActorAuditoria(encontrado.Id.ToString(), encontrado.NombreCompleto, rolTexto, CanalAuditoria.Personal);

        // 3. Bloqueado: ni siquiera se mira la contraseña.
        if (await usuarios.IsLockedOutAsync(encontrado))
            return await AuditarAsync(actor, encontrado, AccionesAuditoria.IngresoPersonalBloqueado,
                "intento-durante-bloqueo", ResultadoIngreso.Bloqueado(), ct);

        // 4. Contraseña mala: suma un intento; al 5.º Identity fija el bloqueo.
        if (!await usuarios.CheckPasswordAsync(encontrado, contrasena))
        {
            Asegurar(await usuarios.AccessFailedAsync(encontrado));
            return await usuarios.IsLockedOutAsync(encontrado)
                ? await AuditarAsync(actor, encontrado, AccionesAuditoria.IngresoPersonalBloqueado,
                    "bloqueo", ResultadoIngreso.Bloqueado(), ct)
                : await AuditarAsync(actor, encontrado, AccionesAuditoria.IngresoPersonalFallido,
                    "contrasena-incorrecta", ResultadoIngreso.CredencialesInvalidas(), ct);
        }

        // 5. Contraseña buena: recién ahora se informa el estado del usuario.
        if (!encontrado.Activo)
            return await AuditarAsync(actor, encontrado, AccionesAuditoria.IngresoPersonalFallido,
                "desactivado", ResultadoIngreso.Desactivado(), ct);
        if (roles.Count == 0)
            return await AuditarAsync(actor, encontrado, AccionesAuditoria.IngresoPersonalFallido,
                "sin-rol", ResultadoIngreso.SinRol(), ct);
        if (roles.Count > 1)
            return await AuditarAsync(actor, encontrado, AccionesAuditoria.IngresoPersonalFallido,
                "varios-roles", ResultadoIngreso.VariosRoles(), ct);

        // 6. Éxito. La fábrica de Identity ya trae id, rol y sello de seguridad; se suma el nombre.
        Asegurar(await usuarios.ResetAccessFailedCountAsync(encontrado));
        var principal = await fabricaDePrincipal.CreateAsync(encontrado);
        ((ClaimsIdentity)principal.Identity!).AddClaim(new Claim(ClaimsPersonal.NombreCompleto, encontrado.NombreCompleto));
        return await AuditarAsync(actor, encontrado, AccionesAuditoria.IngresoPersonal, motivo: null,
            ResultadoIngreso.Exitoso(principal, roles[0]), ct);
    }

    public async Task<bool> SesionVigenteAsync(string usuarioId, string? selloDeSeguridad, CancellationToken ct)
    {
        // Sin try/catch: si la base no responde, el pedido termina en el error genérico (no se deja pasar
        // a nadie sin revisar).
        var encontrado = await usuarios.FindByIdAsync(usuarioId);
        if (encontrado is null || !encontrado.Activo || encontrado.SecurityStamp != selloDeSeguridad)
            return false;
        return (await usuarios.GetRolesAsync(encontrado)).Count == 1;
    }

    // Si Identity no pudo guardar el contador (p. ej. un conflicto de concurrencia), no se sigue como si
    // nada: un intento sin sumar es un bloqueo que no llega. Llega al manejador global como error.
    private static void Asegurar(IdentityResult resultado)
    {
        if (!resultado.Succeeded)
            throw new InvalidOperationException(
                $"Identity no pudo actualizar el usuario del personal: {string.Join(" ", resultado.Errors.Select(e => e.Code))}");
    }

    // 7. Registrar sólo AGREGA (story 1.3): se guarda acá, ANTES de devolver. Así la cookie nunca se
    //    emite si la auditoría no quedó escrita.
    private async Task<ResultadoIngreso> AuditarAsync(ActorAuditoria actor, UsuarioPersonal usuario, string accion,
        string? motivo, ResultadoIngreso resultado, CancellationToken ct)
    {
        auditoria.Registrar(actor, accion, EntidadTipo, usuario.Id.ToString(), motivo is null ? null : new { motivo });
        await contexto.SaveChangesAsync(ct);
        return resultado;
    }
}
