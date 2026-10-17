using System.ComponentModel.DataAnnotations;
using HospitalTurnos.Aplicacion.Catalogo;
using HospitalTurnos.Dominio.Catalogo;

namespace HospitalTurnos.Web.Components.Personal.Administracion;

/// <summary>
/// El formulario de A1 (alta y ficha, story 3.2). Valida en el servidor con los mismos textos que el dominio
/// (AC 7): la pantalla y la entidad dicen lo mismo. La regla de caracteres de la matrícula es la del dominio
/// (un solo lugar para la expresión) y mide la matrícula ya normalizada.
/// </summary>
public sealed class FormularioMedico : IValidatableObject
{
    [Required(ErrorMessage = "Ingresá el nombre.")]
    [StringLength(Medico.LargoMaximoNombre, ErrorMessage = "El nombre tiene como máximo 100 caracteres.")]
    public string? Nombre { get; set; }

    [Required(ErrorMessage = "Ingresá el apellido.")]
    [StringLength(Medico.LargoMaximoNombre, ErrorMessage = "El apellido tiene como máximo 100 caracteres.")]
    public string? Apellido { get; set; }

    [Required(ErrorMessage = "Ingresá la matrícula.")]
    public string? Matricula { get; set; }

    [Required(ErrorMessage = "Elegí una especialidad.")]
    public long? EspecialidadId { get; set; }

    public Guid? UsuarioPersonalId { get; set; }   // opcional: "Sin usuario" tiene value=""

    public static FormularioMedico Desde(DatosMedico datos) => new()
    {
        Nombre = datos.Nombre,
        Apellido = datos.Apellido,
        Matricula = datos.Matricula,
        EspecialidadId = datos.EspecialidadId,
        UsuarioPersonalId = datos.UsuarioPersonalId,
    };

    public DatosMedico ADatos() => new(Nombre!, Apellido!, Matricula!, EspecialidadId!.Value, UsuarioPersonalId);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Medico.MatriculaValida(Medico.NormalizarMatricula(Matricula)))
            yield return new ValidationResult(Medico.MensajeMatricula, [nameof(Matricula)]);
    }
}
