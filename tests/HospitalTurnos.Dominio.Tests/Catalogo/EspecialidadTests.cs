using HospitalTurnos.Dominio.Catalogo;
using HospitalTurnos.Dominio.Excepciones;

namespace HospitalTurnos.Dominio.Tests.Catalogo;

public class EspecialidadTests
{
    [Theory]
    [Trait("Story", "3.1")]
    [Trait("AC", "1")]
    [InlineData("Traumatología", "Traumatología")]
    [InlineData("  Clínica   médica ", "Clínica médica")]
    public void Constructor_NombreValido_NaceActivaConElNombreLimpio(string tipeado, string esperado)
    {
        var especialidad = new Especialidad(tipeado);

        Assert.True(especialidad.Activa);
        Assert.Equal(esperado, especialidad.Nombre);
    }

    public static TheoryData<string, string> NombresInvalidos => new()
    {
        { "", "Ingresá el nombre de la especialidad." },
        { "   ", "Ingresá el nombre de la especialidad." },
        { new string('a', 81), "El nombre de la especialidad tiene como máximo 80 caracteres." },
    };

    [Theory]
    [Trait("Story", "3.1")]
    [Trait("AC", "1")]
    [MemberData(nameof(NombresInvalidos))]
    public void Constructor_NombreVacioOLargo_LanzaReglaDeNegocio(string tipeado, string mensaje)
    {
        // Assert.Throws exige el tipo exacto: una ConflictoException (que hereda) no pasaría.
        var error = Assert.Throws<ReglaDeNegocioException>(() => new Especialidad(tipeado));

        Assert.Equal(mensaje, error.Message);
    }

    [Theory]
    [Trait("Story", "3.1")]
    [Trait("AC", "2")]
    [InlineData("Cardiología", "CARDIOLOGIA")]
    [InlineData("cardiologia", "CARDIOLOGIA")]
    [InlineData(" CARDIOLOGÍA ", "CARDIOLOGIA")]
    [InlineData("Cardio  logía", "CARDIO LOGIA")]
    public void Normalizar_MayusculasTildesYEspacios_DevuelveLaMismaClave(string nombre, string clave)
    {
        Assert.Equal(clave, Especialidad.Normalizar(nombre));
        Assert.Equal(clave, new Especialidad(nombre).NombreNormalizado);
    }

    [Theory]
    [Trait("Story", "3.1")]
    [Trait("AC", "3")]
    [InlineData(true)]
    [InlineData(false)]
    public void Desactivar_SegunEstado_DesactivaOLanzaConflicto(bool activa)
    {
        var especialidad = new Especialidad("Cardiología");
        if (!activa) especialidad.Desactivar();

        if (activa)
        {
            especialidad.Desactivar();
        }
        else
        {
            var error = Assert.Throws<ConflictoException>(especialidad.Desactivar);
            Assert.Equal("La especialidad Cardiología ya está desactivada.", error.Message);
        }

        Assert.False(especialidad.Activa);
    }

    [Theory]
    [Trait("Story", "3.1")]
    [Trait("AC", "4")]
    [InlineData(false)]
    [InlineData(true)]
    public void Reactivar_SegunEstado_ReactivaOLanzaConflicto(bool activa)
    {
        var especialidad = new Especialidad("Cardiología");
        if (!activa) especialidad.Desactivar();

        if (!activa)
        {
            especialidad.Reactivar();
        }
        else
        {
            var error = Assert.Throws<ConflictoException>(especialidad.Reactivar);
            Assert.Equal("La especialidad Cardiología ya está activa.", error.Message);
        }

        Assert.True(especialidad.Activa);
    }
}
