using Microsoft.AspNetCore.Identity;

namespace HospitalTurnos.Infraestructura.Identidad;

/// <summary>
/// Un integrante del personal (contrato técnico, R-13). Identity aporta usuario, hash, bloqueo y sello
/// de seguridad; acá se suma lo propio del hospital.
/// </summary>
public class UsuarioPersonal : IdentityUser<Guid>
{
    /// <summary>Lo que se muestra en el menú y queda como "quien" en la auditoría.</summary>
    public string NombreCompleto { get; set; } = "";

    /// <summary>Desactivar (story 2.2) corta el ingreso y, al revalidar, las sesiones abiertas.</summary>
    public bool Activo { get; set; } = true;
}
