using HospitalTurnos.Dominio.Personal;

namespace HospitalTurnos.Web.Personal;

/// <summary>
/// Las rutas del personal (todas bajo /personal/: la raíz queda para la web del paciente, sprint 5) y
/// qué rol entra a cada una (AC 13). Un solo lugar para el menú (agrupado desde la story 3.3), los inicios por
/// rol y el ReturnUrl.
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
    // Story 3.2: dentro de A1, fuera del menú. "Nuevo…" y no "MedicoNuevo": así se llama la página (I-5).
    public const string NuevoMedico = "/personal/admin/medicos/nuevo";
    public static string FichaMedico(long id) => $"/personal/admin/medicos/{id}";
    public const string Propuestas = "/personal/admin/propuestas";
    public const string Especialidades = "/personal/admin/especialidades";
    public const string Bloqueos = "/personal/admin/bloqueos";
    public const string Usuarios = "/personal/admin/usuarios";
    public const string Auditoria = "/personal/admin/auditoria";

    /// <summary>Una opción del menú. Pendientes: el contador de trabajo por resolver (B-1.7); lo trae la 4.3.</summary>
    public sealed record Seccion(string Ruta, string Titulo, int? Pendientes = null);

    /// <summary>Un grupo del menú lateral, con su rótulo (story 3.3, AC 13).</summary>
    public sealed record GrupoDeMenu(string Rotulo, IReadOnlyList<Seccion> Secciones);

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

    // Agrupadas por tarea y ordenadas por frecuencia de uso (guía, "Menú lateral"). La guía define los grupos
    // del administrador; los de recepción y médico los aprobó el docente con la story 3.3 (S-36).
    private static readonly Dictionary<string, GrupoDeMenu[]> PorRol = new()
    {
        [Roles.Recepcionista] =
        [
            new("Recepción", [SeccionTurnos]),
            new("Agenda", [SeccionAgendas, SeccionParaAvisar]),
        ],
        [Roles.Medico] = [new("Mi agenda", [SeccionMiAgenda, SeccionMiPropuesta])],
        [Roles.Administrador] =
        [
            new("Agenda", [SeccionAgendas, SeccionPropuestas, SeccionBloqueos, SeccionParaAvisar]),
            new("Catálogos", [SeccionMedicos, SeccionEspecialidades]),
            new("Administración", [SeccionUsuarios, SeccionAuditoria]),
        ],
    };

    // La pantalla principal de cada rol, explícita: reordenar el menú no cambia a dónde entra cada uno (AC 14).
    private static readonly Dictionary<string, string> InicioPorRol = new()
    {
        [Roles.Recepcionista] = Turnos,
        [Roles.Medico] = MiAgenda,
        [Roles.Administrador] = Medicos,
    };

    private static readonly Seccion[] Todas =
    [
        SeccionTurnos, SeccionAgendas, SeccionParaAvisar, SeccionMiAgenda, SeccionMiPropuesta, SeccionMedicos,
        SeccionPropuestas, SeccionEspecialidades, SeccionBloqueos, SeccionUsuarios, SeccionAuditoria,
    ];

    public static IReadOnlyList<GrupoDeMenu> GruposDe(string? rol) =>
        rol is not null && PorRol.TryGetValue(rol, out var grupos) ? grupos : [];

    /// <summary>Las secciones del rol en el orden del menú (los grupos, uno detrás del otro).</summary>
    public static IReadOnlyList<Seccion> SeccionesDe(string? rol) => GruposDe(rol).SelectMany(g => g.Secciones).ToList();

    /// <summary>La pantalla principal del rol. Sin rol conocido, el ingreso.</summary>
    public static string InicioDe(string? rol) =>
        rol is not null && InicioPorRol.TryGetValue(rol, out var inicio) ? inicio : Ingreso;

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
