using HospitalTurnos.Dominio.Catalogo;
using HospitalTurnos.Dominio.Excepciones;

namespace HospitalTurnos.Dominio.Tests.Catalogo;

public class MedicoTests
{
    private const string MensajeMatricula =
        "La matrícula tiene hasta 20 caracteres: letras, números, espacios, puntos, guiones o barras.";

    [Fact]
    [Trait("Story", "3.2")]
    [Trait("AC", "6")]
    public void Constructor_DatosValidos_RecortaYNormalizaLaMatricula()
    {
        var medico = new Medico("  Martín ", " Pereyra", " mp  1234 ", 3, null);

        Assert.Equal("Martín", medico.Nombre);
        Assert.Equal("Pereyra", medico.Apellido);
        Assert.Equal("MP 1234", medico.Matricula);
        Assert.Equal(3, medico.EspecialidadId);
        Assert.Null(medico.UsuarioPersonalId);
        Assert.Equal("Pereyra, Martín", medico.NombreCompleto);
    }

    public static TheoryData<string, string, string, long, string> DatosInvalidos => new()
    {
        { "", "Pereyra", "MP 1", 1, "Ingresá el nombre." },
        { "  ", "Pereyra", "MP 1", 1, "Ingresá el nombre." },
        { new string('a', 101), "Pereyra", "MP 1", 1, "El nombre tiene como máximo 100 caracteres." },
        { "Martín", "", "MP 1", 1, "Ingresá el apellido." },
        { "Martín", "  ", "MP 1", 1, "Ingresá el apellido." },
        { "Martín", new string('a', 101), "MP 1", 1, "El apellido tiene como máximo 100 caracteres." },
        { "Martín", "Pereyra", "", 1, "Ingresá la matrícula." },
        { "Martín", "Pereyra", new string('1', 21), 1, MensajeMatricula },
        { "Martín", "Pereyra", "MP#12", 1, MensajeMatricula },
        { "Martín", "Pereyra", "MP 1", 0, "Elegí una especialidad." },
        { "Martín", "Pereyra", "MP 1", -1, "Elegí una especialidad." },
    };

    [Theory]
    [Trait("Story", "3.2")]
    [Trait("AC", "7")]
    [MemberData(nameof(DatosInvalidos))]
    public void Constructor_DatosInvalidos_LanzaReglaDeNegocioConElMensajeDelCampo(string nombre, string apellido,
        string matricula, long especialidadId, string mensaje)
    {
        // Assert.Throws exige el tipo exacto: una ConflictoException (que hereda) no pasaría.
        var error = Assert.Throws<ReglaDeNegocioException>(() => new Medico(nombre, apellido, matricula, especialidadId, null));

        Assert.Equal(mensaje, error.Message);
    }

    [Fact]
    [Trait("Story", "3.2")]
    [Trait("AC", "16")]
    public void CambiarDatos_ConYSinCambios_DevuelveSoloLosCamposQueCambiaron()
    {
        var usuario = Guid.NewGuid();

        // (a) los mismos datos, con la matrícula escrita de otra forma: nada cambia
        var a = new Medico("Martín", "Pereyra", "MP 1234", 3, null);
        Assert.Empty(a.CambiarDatos("Martín", "Pereyra", " mp 1234", 3, null));
        Assert.Equal("MP 1234", a.Matricula);

        // (b) cambian apellido y matrícula
        var b = new Medico("Martín", "Pereyra", "MP 1234", 3, null);
        var cambiosB = b.CambiarDatos("Martín", "Pereira", "MP 9", 3, null);
        Assert.Equal(
            [new CambioDeDato("apellido", "Pereyra", "Pereira"), new CambioDeDato("matricula", "MP 1234", "MP 9")],
            cambiosB);
        Assert.Equal("Pereira", b.Apellido);
        Assert.Equal("MP 9", b.Matricula);

        // (c) el vínculo pasa de ninguno a un usuario
        var c = new Medico("Martín", "Pereyra", "MP 1234", 3, null);
        var cambiosC = c.CambiarDatos("Martín", "Pereyra", "MP 1234", 3, usuario);
        Assert.Equal([new CambioDeDato("usuarioPersonalId", null, usuario.ToString())], cambiosC);
        Assert.Equal(usuario, c.UsuarioPersonalId);

        // (d) un dato inválido no deja nada a medio cambiar: valida todo ANTES de tocar nada
        var d = new Medico("Martín", "Pereyra", "MP 1234", 3, null);
        Assert.Throws<ReglaDeNegocioException>(() => d.CambiarDatos("Ana", "", "MP 1234", 3, null));
        Assert.Equal("Martín", d.Nombre);
    }
}
