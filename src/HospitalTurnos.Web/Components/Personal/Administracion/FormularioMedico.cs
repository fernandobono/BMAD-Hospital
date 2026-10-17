using System.ComponentModel.DataAnnotations;
using HospitalTurnos.Aplicacion.Catalogo;
using HospitalTurnos.Dominio.Catalogo;

namespace HospitalTurnos.Web.Components.Personal.Administracion;

/// <summary>
/// El formulario de A1 (alta y ficha, story 3.2). Valida en el servidor con los mismos textos que el dominio
/// (AC 7): la pantalla y la entidad dicen lo mismo. La regla de caracteres de la matrícula es la del dominio
/// (un solo lugar para la expresión) y mide la matrícula ya normalizada. Story 3.3 (AC 34): es un atributo del
/// campo y no del formulario, así el mensaje aparece al salir de la matrícula y no recién al guardar.
/// </summary>
public sealed class FormularioMedico
{
    [Required(ErrorMessage = "Ingresá el nombre.")]
    [StringLength(Medico.LargoMaximoNombre, ErrorMessage = "El nombre tiene como máximo 100 caracteres.")]
    public string? Nombre { get; set; }

    [Required(ErrorMessage = "Ingresá el apellido.")]
    [StringLength(Medico.LargoMaximoNombre, ErrorMessage = "El apellido tiene como máximo 100 caracteres.")]
    public string? Apellido { get; set; }

    [Required(ErrorMessage = "Ingresá la matrícula.")]
    [MatriculaDelDominio]
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

    /// <summary>La regla de la matrícula del dominio (Medico.MatriculaValida), sobre la matrícula normalizada.</summary>
    private sealed class MatriculaDelDominioAttribute : ValidationAttribute
    {
        public MatriculaDelDominioAttribute() : base(Medico.MensajeMatricula)
        {
        }

        // Vacía no es asunto de esta regla: la cubre [Required] con su propio mensaje.
        public override bool IsValid(object? value) =>
            value is not string matricula || string.IsNullOrWhiteSpace(matricula)
            || Medico.MatriculaValida(Medico.NormalizarMatricula(matricula));
    }
}
