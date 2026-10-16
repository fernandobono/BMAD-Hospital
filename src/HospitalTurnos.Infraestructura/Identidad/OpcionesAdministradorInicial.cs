namespace HospitalTurnos.Infraestructura.Identidad;

/// <summary>
/// El primer administrador, sólo para una base sin usuarios. Los valores llegan de .env (Compose) o de
/// user-secrets: ningún archivo versionado trae la contraseña (contrato §Secretos).
/// </summary>
public sealed class OpcionesAdministradorInicial
{
    public const string Seccion = "Personal:AdministradorInicial";

    /// <summary>El nombre por defecto vive en el código: el compose no puede tener valores por defecto.</summary>
    public const string NombrePorDefecto = "Administración";

    public string? Usuario { get; set; }

    public string? Contrasena { get; set; }

    public string? NombreCompleto { get; set; }

    /// <summary>
    /// "Sin configuración" incluye las claves vacías o con espacios: es lo que llega desde Compose con un
    /// clon limpio sin .env (para .NET la clave existe con valor "").
    /// </summary>
    public bool EstaConfigurado =>
        !string.IsNullOrWhiteSpace(Usuario) && !string.IsNullOrWhiteSpace(Contrasena);
}
