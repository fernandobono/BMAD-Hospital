using HospitalTurnos.Dominio.Personal;

namespace HospitalTurnos.Web.Personal;

/// <summary>
/// Las rutas del personal (todas bajo /personal/: la raíz queda para la web del paciente, sprint 5) y
/// qué rol entra a cada una (AC 13). Un solo lugar para el menú, los inicios por rol y el ReturnUrl.
/// </summary>
public static class RutasPersonal
{
    public const string Prefijo = "/personal/";
    public const string Ingreso = "/personal/ingreso";
    public const string AccesoDenegado = "/personal/acceso-denegado";
    public const string Salir = "/personal/salir";
    // Story 2.2: fuera del menú (rompería la prueba del menú de la 2.1); se llega por la dirección.
    public const string CambiarContrasena = "/personal/cambiar-contrasena";

    public const string Turnos = "/personal/recepcion/turnos";
    public const string Agendas = "/personal/agendas";
    public const string ParaAvisar = "/personal/para-avisar";
    public const string MiAgenda = "/personal/medico/mi-agenda";
    public const string MiPropuesta = "/personal/medico/mi-propuesta";
    public const string Medicos = "/personal/admin/medicos";
    public const string Propuestas = "/personal/admin/propuestas";
    public const string Especialidades = "/personal/admin/especialidades";
    public const string Bloqueos = "/personal/admin/bloqueos";
    public const string Usuarios = "/personal/admin/usuarios";
    public const string Auditoria = "/personal/admin/auditoria";

    public sealed record Seccion(string Ruta, string Titulo);

    private static readonly Seccion SeccionTurnos = new(Turnos, "Turnos");
    private static readonly Seccion SeccionAgendas = new(Agendas, "Agendas");
    private static readonly Seccion SeccionParaAvisar = new(ParaAvisar, "Pacientes para avisar");
    private static readonly Seccion SeccionMiAgenda = new(MiAgenda, "Mi agenda");
    private static readonly Seccion SeccionMiPropuesta = new(MiPropuesta, "Mi propuesta de agenda");
    private static readonly Seccion SeccionMedicos = new(Medicos, "Médicos");
    private static readonly Seccion SeccionPropuestas = new(Propuestas, "Propuestas de agenda");
    private static readonly Seccion SeccionEspecialidades = new(Especialidades, "Especialidades");
    private static readonly Seccion SeccionBloqueos = new(Bloqueos, "Bloqueos de agenda");
    private static readonly Seccion SeccionUsuarios = new(Usuarios, "Usuarios del personal");
    private static readonly Seccion SeccionAuditoria = new(Auditoria, "Auditoría");

    // El orden es el del mapa de navegación de UX §3 (AC 14); el primero es el inicio del rol.
    private static readonly Dictionary<string, Seccion[]> PorRol = new()
    {
        [Roles.Recepcionista] = [SeccionTurnos, SeccionAgendas, SeccionParaAvisar],
        [Roles.Medico] = [SeccionMiAgenda, SeccionMiPropuesta],
        [Roles.Administrador] =
        [
            SeccionMedicos, SeccionPropuestas, SeccionEspecialidades, SeccionBloqueos,
            SeccionUsuarios, SeccionAuditoria, SeccionParaAvisar, SeccionAgendas,
        ],
    };

    private static readonly Seccion[] Todas =
    [
        SeccionTurnos, SeccionAgendas, SeccionParaAvisar, SeccionMiAgenda, SeccionMiPropuesta, SeccionMedicos,
        SeccionPropuestas, SeccionEspecialidades, SeccionBloqueos, SeccionUsuarios, SeccionAuditoria,
    ];

    public static IReadOnlyList<Seccion> SeccionesDe(string? rol) =>
        rol is not null && PorRol.TryGetValue(rol, out var secciones) ? secciones : [];

    /// <summary>La pantalla principal del rol. Sin rol conocido, el ingreso.</summary>
    public static string InicioDe(string? rol) => SeccionesDe(rol).FirstOrDefault()?.Ruta ?? Ingreso;

    public static string TituloDe(string ruta) =>
        Todas.FirstOrDefault(s => string.Equals(s.Ruta, ruta, StringComparison.OrdinalIgnoreCase))?.Titulo ?? "";

    /// <summary>
    /// Sólo rutas propias del personal: evita la redirección abierta (AC 7). "//malo.example" es una
    /// dirección de otro sitio para el navegador; "https://…" también.
    /// </summary>
    public static bool EsReturnUrlValido(string? url) =>
        !string.IsNullOrEmpty(url)
        && url.StartsWith(Prefijo, StringComparison.Ordinal)
        && !url.Contains('\\');
}
