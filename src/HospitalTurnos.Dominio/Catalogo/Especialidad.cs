using System.Globalization;
using System.Text;
using HospitalTurnos.Dominio.Excepciones;

namespace HospitalTurnos.Dominio.Catalogo;

/// <summary>Una especialidad del hospital (FR-12, story 3.1). No se borra nunca: se desactiva.</summary>
public sealed class Especialidad
{
    public const int LargoMaximo = 80;

    public long Id { get; private set; }
    public string Nombre { get; private set; } = null!;
    // La clave con la que se comparan nombres (AC 2). Va en su columna con índice único:
    // la base, no sólo el código, impide "Cardiología" y "cardiologia" a la vez.
    public string NombreNormalizado { get; private set; } = null!;
    public bool Activa { get; private set; }

    private Especialidad() { } // para EF

    public Especialidad(string nombre)
    {
        Nombre = Limpiar(nombre);            // lanza ReglaDeNegocioException (AC 1)
        NombreNormalizado = Normalizar(Nombre);
        Activa = true;                       // en la clase, no como default de la base (EF tomaría false como "sin valor")
    }

    /// <summary>Sin espacios al principio ni al final y los internos repetidos en uno (AC 1).</summary>
    public static string Limpiar(string? nombre)
    {
        var limpio = string.Join(' ', (nombre ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        if (limpio.Length == 0)
            throw new ReglaDeNegocioException("Ingresá el nombre de la especialidad.");
        if (limpio.Length > LargoMaximo)
            throw new ReglaDeNegocioException($"El nombre de la especialidad tiene como máximo {LargoMaximo} caracteres.");
        return limpio;
    }

    /// <summary>
    /// La clave del AC 2: el nombre limpio, sin tildes y en mayúsculas. Sin cultura: el resultado no
    /// depende del servidor. La ñ también pierde su tilde (Señal y Senal dan la misma clave).
    /// </summary>
    public static string Normalizar(string nombre)
    {
        var separado = Limpiar(nombre).Normalize(NormalizationForm.FormD);
        var sinTildes = new StringBuilder(separado.Length);
        foreach (var letra in separado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(letra) != UnicodeCategory.NonSpacingMark)
                sinTildes.Append(letra);
        }
        return sinTildes.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }

    public void Desactivar()
    {
        if (!Activa) throw new ConflictoException($"La especialidad {Nombre} ya está desactivada.");
        Activa = false;
    }

    public void Reactivar()
    {
        if (Activa) throw new ConflictoException($"La especialidad {Nombre} ya está activa.");
        Activa = true;
    }
}
