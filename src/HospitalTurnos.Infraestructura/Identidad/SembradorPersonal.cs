using HospitalTurnos.Dominio.Personal;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HospitalTurnos.Infraestructura.Identidad;

/// <summary>
/// Deja lo mínimo para poder entrar: los roles y, sólo con la base sin usuarios, el administrador
/// inicial de la configuración. Nunca toca un usuario existente (no pisa la contraseña de nadie).
/// </summary>
public sealed class SembradorPersonal(
    UserManager<UsuarioPersonal> usuarios,
    RoleManager<IdentityRole<Guid>> roles,
    IOptions<OpcionesAdministradorInicial> opciones,
    ILogger<SembradorPersonal> registro)
{
    public async Task SembrarAsync(CancellationToken ct = default)
    {
        foreach (var rol in Roles.Todos)
            if (!await roles.RoleExistsAsync(rol))
                Asegurar(await roles.CreateAsync(new IdentityRole<Guid>(rol)));

        // Ya hay alguien: no hay nada que sembrar, ni siquiera se valida la configuración (AC 17).
        if (await usuarios.Users.AnyAsync(ct))
            return;

        var admin = opciones.Value;
        if (!admin.EstaConfigurado)
        {
            // No lanza: con restart: unless-stopped un error acá dejaría el contenedor reiniciándose en
            // bucle. Arranca sin administrador y lo avisa (AC 18).
            registro.LogWarning("No hay usuarios del personal y falta Personal:AdministradorInicial (Usuario y Contrasena). La aplicación arranca sin administrador: completá ADMIN_INICIAL_USUARIO y ADMIN_INICIAL_CONTRASENA en .env y reiniciá.");
            return;
        }

        var usuario = new UsuarioPersonal
        {
            UserName = admin.Usuario!.Trim(),
            NombreCompleto = string.IsNullOrWhiteSpace(admin.NombreCompleto)
                ? OpcionesAdministradorInicial.NombrePorDefecto
                : admin.NombreCompleto.Trim(),
        };

        // Alguien quiso crear un administrador y la contraseña no sirve: mejor cortar con un mensaje
        // claro (sin la contraseña) que arrancar sin él (AC 19).
        var incumplidas = new List<string>();
        foreach (var validador in usuarios.PasswordValidators)
        {
            var resultado = await validador.ValidateAsync(usuarios, usuario, admin.Contrasena!);
            incumplidas.AddRange(resultado.Errors.Select(e => e.Description));
        }
        if (incumplidas.Count > 0)
            throw new InvalidOperationException(
                $"La contraseña de {OpcionesAdministradorInicial.Seccion}:Contrasena no cumple la política: {string.Join(" ", incumplidas)}");

        Asegurar(await usuarios.CreateAsync(usuario, admin.Contrasena!));
        Asegurar(await usuarios.AddToRoleAsync(usuario, Roles.Administrador));
        registro.LogInformation("Administrador inicial creado: {Usuario}", usuario.UserName);
    }

    private static void Asegurar(IdentityResult resultado)
    {
        if (!resultado.Succeeded)
            throw new InvalidOperationException(
                // Código y descripción: la descripción puede venir vacía y el mensaje quedaría sin causa.
                $"No se pudo sembrar el personal: {string.Join(" ", resultado.Errors.Select(e => $"{e.Code} {e.Description}".Trim()))}");
    }
}
