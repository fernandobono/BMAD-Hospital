using System.Globalization;
using System.Text.RegularExpressions;
using HospitalTurnos.Dominio.Excepciones;

namespace HospitalTurnos.Dominio.Catalogo;

/// <summary>
/// Un médico del plantel (FR-13, story 3.2). No se borra ni se desactiva: sólo se da de alta y se corrige.
/// Nace válido y valida cada cambio antes de tocar nada. Del usuario vinculado guarda sólo el id: el dominio
/// no conoce a Identity.
/// </summary>
public sealed partial class Medico
{
    public const int LargoMaximoNombre = 100;
    public const int LargoMaximoMatricula = 20;
    public const string MensajeMatricula =
        "La matrícula tiene hasta 20 caracteres: letras, números, espacios, puntos, guiones o barras.";

    public long Id { get; private set; }                 // long → identity, como toda entidad propia
    public string Nombre { get; private set; } = null!;
    public string Apellido { get; private set; } = null!;
    public string Matricula { get; private set; } = null!;
    public long EspecialidadId { get; private set; }       // tipo = el de Especialidad.Id (3.1)
    public Guid? UsuarioPersonalId { get; private set; }  // el dominio no conoce a Identity: sólo el id

    private Medico() { } // para EF

    public Medico(string nombre, string apellido, string matricula, long especialidadId, Guid? usuarioPersonalId)
    {
        (Nombre, Apellido, Matricula, EspecialidadId) = Validar(nombre, apellido, matricula, especialidadId);
        UsuarioPersonalId = usuarioPersonalId;
    }

    public string NombreCompleto => $"{Apellido}, {Nombre}";   // como lo muestran A1 y A6

    /// <summary>Valida todo ANTES de tocar nada y devuelve sólo lo que cambió (para la auditoría, AC 16).</summary>
    public IReadOnlyList<CambioDeDato> CambiarDatos(string nombre, string apellido, string matricula,
        long especialidadId, Guid? usuarioPersonalId)
    {
        var (nuevoNombre, nuevoApellido, nuevaMatricula, nuevaEspecialidad) =
            Validar(nombre, apellido, matricula, especialidadId);

        var cambios = new List<CambioDeDato>();
        if (nuevoNombre != Nombre) cambios.Add(new("nombre", Nombre, nuevoNombre));
        if (nuevoApellido != Apellido) cambios.Add(new("apellido", Apellido, nuevoApellido));
        if (nuevaMatricula != Matricula) cambios.Add(new("matricula", Matricula, nuevaMatricula));
        if (nuevaEspecialidad != EspecialidadId)
            cambios.Add(new("especialidadId", Texto(EspecialidadId), Texto(nuevaEspecialidad)));
        if (usuarioPersonalId != UsuarioPersonalId)
            cambios.Add(new("usuarioPersonalId", UsuarioPersonalId?.ToString(), usuarioPersonalId?.ToString()));

        (Nombre, Apellido, Matricula, EspecialidadId) = (nuevoNombre, nuevoApellido, nuevaMatricula, nuevaEspecialidad);
        UsuarioPersonalId = usuarioPersonalId;
        return cambios;
    }

    /// <summary>" mp  1234 " → "MP 1234". Pública: la usa el caso de uso para buscar la repetida.</summary>
    public static string NormalizarMatricula(string? matricula) =>
        string.Join(' ', (matricula ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToUpperInvariant();

    /// <summary>
    /// La regla de la matrícula ya normalizada (decisión 9). Pública: el formulario de A1 la usa para mostrar
    /// el mensaje debajo del campo (AC 7) sin copiar la expresión.
    /// </summary>
    public static bool MatriculaValida(string normalizada) => FormatoMatricula().IsMatch(normalizada);

    private static (string Nombre, string Apellido, string Matricula, long EspecialidadId) Validar(string? nombre,
        string? apellido, string? matricula, long especialidadId)
    {
        var limpioNombre = (nombre ?? "").Trim();
        if (limpioNombre.Length == 0) throw new ReglaDeNegocioException("Ingresá el nombre.");
        if (limpioNombre.Length > LargoMaximoNombre)
            throw new ReglaDeNegocioException($"El nombre tiene como máximo {LargoMaximoNombre} caracteres.");

        var limpioApellido = (apellido ?? "").Trim();
        if (limpioApellido.Length == 0) throw new ReglaDeNegocioException("Ingresá el apellido.");
        if (limpioApellido.Length > LargoMaximoNombre)
            throw new ReglaDeNegocioException($"El apellido tiene como máximo {LargoMaximoNombre} caracteres.");

        var normalizada = NormalizarMatricula(matricula);
        if (normalizada.Length == 0) throw new ReglaDeNegocioException("Ingresá la matrícula.");
        if (!MatriculaValida(normalizada)) throw new ReglaDeNegocioException(MensajeMatricula);

        if (especialidadId <= 0) throw new ReglaDeNegocioException("Elegí una especialidad.");

        return (limpioNombre, limpioApellido, normalizada, especialidadId);
    }

    private static string Texto(long valor) => valor.ToString(CultureInfo.InvariantCulture);

    // Ya normalizada (mayúsculas, espacios simples): MP 1234, MN-45678, 12345/7 (decisión 9).
    [GeneratedRegex("^[A-ZÑ0-9 ./-]{1,20}$")]
    private static partial Regex FormatoMatricula();
}
