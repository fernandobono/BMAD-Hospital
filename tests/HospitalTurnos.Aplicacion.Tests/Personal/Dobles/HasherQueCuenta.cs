using HospitalTurnos.Infraestructura.Identidad;
using Microsoft.AspNetCore.Identity;

namespace HospitalTurnos.Aplicacion.Tests.Personal.Dobles;

/// <summary>
/// Un "spy" escrito a mano: delega en el hasher real de Identity (mismo PBKDF2, mismo costo) y cuenta
/// cuántas veces se verificó una contraseña. Sirve para probar que el usuario inexistente paga la misma
/// verificación que uno real (AC 2a), sin medir tiempos.
/// </summary>
public sealed class HasherQueCuenta : IPasswordHasher<UsuarioPersonal>
{
    private readonly PasswordHasher<UsuarioPersonal> _real = new();
    private int _verificaciones;

    public int Verificaciones => _verificaciones;

    public string HashPassword(UsuarioPersonal user, string password) => _real.HashPassword(user, password);

    public PasswordVerificationResult VerifyHashedPassword(UsuarioPersonal user, string hashedPassword, string providedPassword)
    {
        Interlocked.Increment(ref _verificaciones);
        return _real.VerifyHashedPassword(user, hashedPassword, providedPassword);
    }
}
