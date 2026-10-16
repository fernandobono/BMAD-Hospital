using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Dominio.Auditoria;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Infraestructura.Identidad;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalTurnos.Aplicacion.Tests.Ayudas;

/// <summary>
/// Usuarios propios de cada prueba (p-xxxxxxxx) y su actor de auditoría. La base es compartida: nadie
/// toca usuarios ajenos. La 3.1 y la 3.2 la reusan (story 2.2, AC 26).
/// </summary>
public static class UsuariosDePrueba
{
    public const string Clave = "Clave-Correcta-1";

    public static async Task<UsuarioPersonal> CrearAsync(ServiceProvider proveedor, string[] roles, bool activo = true,
        string nombre = "Persona de Prueba")
    {
        using var alcance = proveedor.CreateScope();
        var rolesIdentity = alcance.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var rol in Roles.Todos)
            if (!await rolesIdentity.RoleExistsAsync(rol))
                await rolesIdentity.CreateAsync(new IdentityRole<Guid>(rol));

        var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>();
        var usuario = new UsuarioPersonal
        {
            UserName = $"p-{Guid.NewGuid().ToString("N")[..8]}",
            NombreCompleto = nombre,
            Activo = activo,
        };
        Exito(await usuarios.CreateAsync(usuario, Clave));
        if (roles.Length > 0)
            Exito(await usuarios.AddToRolesAsync(usuario, roles));
        return usuario;
    }

    public static ActorAuditoria ActorDe(UsuarioPersonal usuario, string rol) =>
        new(usuario.Id.ToString(), usuario.NombreCompleto, rol, CanalAuditoria.Personal);

    private static void Exito(IdentityResult resultado) =>
        Assert.True(resultado.Succeeded, string.Join("; ", resultado.Errors.Select(e => e.Description)));
}
