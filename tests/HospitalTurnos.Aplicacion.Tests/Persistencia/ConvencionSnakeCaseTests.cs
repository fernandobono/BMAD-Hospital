using HospitalTurnos.Infraestructura.Persistencia;

namespace HospitalTurnos.Aplicacion.Tests.Persistencia;

public class ConvencionSnakeCaseTests
{
    // Contrato con las stories 1.3 y 1.4: sus tablas salen con estos nombres sin configurarlos a mano.
    [Theory]
    [Trait("Story", "1.1")]
    [Trait("AC", "8")]
    [InlineData("RegistrosAuditoria", "registros_auditoria")]
    [InlineData("MensajesSalientes", "mensajes_salientes")]
    [InlineData("FalloDefinitivoEl", "fallo_definitivo_el")]
    [InlineData("CuerpoHtml", "cuerpo_html")]
    [InlineData("UsuarioId", "usuario_id")]
    [InlineData("Id", "id")]
    public void ASnakeCase_NombrePascalCase_DevuelveMinusculasConGuionBajo(string pascal, string esperado)
    {
        Assert.Equal(esperado, ConvencionSnakeCase.ASnakeCase(pascal));
    }
}
