using System.Security.Claims;
using HospitalTurnos.Aplicacion.Personal;
using Microsoft.AspNetCore.Identity;

namespace HospitalTurnos.Infraestructura.Identidad;

/// <summary>
/// Lo que lleva la cookie del personal: lo de Identity (id, rol, sello de seguridad), el nombre para el
/// menú y, si entró con la contraseña temporal, la marca que lo obliga a cambiarla (story 2.2, AC 19).
/// Un solo lugar para el ingreso y para la cookie nueva después del cambio de contraseña.
/// </summary>
public sealed class FabricaPrincipalPersonal(IUserClaimsPrincipalFactory<UsuarioPersonal> fabricaDeIdentity)
{
    public async Task<ClaimsPrincipal> CrearAsync(UsuarioPersonal usuario)
    {
        var principal = await fabricaDeIdentity.CreateAsync(usuario);
        var identidad = (ClaimsIdentity)principal.Identity!;
        identidad.AddClaim(new Claim(ClaimsPersonal.NombreCompleto, usuario.NombreCompleto));
        if (usuario.DebeCambiarContrasena)
            identidad.AddClaim(new Claim(ClaimsPersonal.DebeCambiarContrasena, "si"));
        return principal;
    }
}
