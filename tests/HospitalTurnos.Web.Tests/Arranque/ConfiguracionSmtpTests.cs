using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace HospitalTurnos.Web.Tests.Arranque;

// Con la base de la fixture: las migraciones corren antes de validar las opciones, así que una cadena
// falsa cortaría el arranque por otro motivo.
[Collection("Postgres")]
public class ConfiguracionSmtpTests(PostgresFixture fixture)
{
    // Hallazgo I-10: sin esto, producción sin la variable de entorno arrancaría "bien" y cada email
    // fallaría en silencio hasta el fallo definitivo, 21 minutos después.
    [Theory]
    [Trait("Story", "1.4")]
    [Trait("AC", "16")]
    [InlineData("Email:Smtp:Host", "Falta Email:Smtp:Host")]
    [InlineData("Email:Smtp:RemitenteDireccion", "Falta Email:Smtp:RemitenteDireccion")]
    // Revisión de seguridad: sin Seguridad el correo saldría sin cifrar; sin Puerto, al puerto 0.
    [InlineData("Email:Smtp:Seguridad", "Falta Email:Smtp:Seguridad")]
    [InlineData("Email:Smtp:Puerto", "Falta Email:Smtp:Puerto", "0")]
    public async Task Arranque_SinConfiguracionSmtp_FallaConMensajeQueNombraLaClave(string clave, string mensajeEsperado,
        string valor = "")
    {
        await using var fabrica = new HospitalWebApplicationFactory(fixture);
        // Una fuente en memoria agregada después de la de la fábrica: es la última y gana.
        await using var app = fabrica.WithWebHostBuilder(b => b.ConfigureAppConfiguration(c =>
            c.AddInMemoryCollection(new Dictionary<string, string?> { [clave] = valor })));

        var error = Record.Exception(() => app.CreateClient());

        Assert.NotNull(error);
        var invalida = Cadena(error).OfType<OptionsValidationException>()
            .FirstOrDefault(e => e.Message.Contains(mensajeEsperado));
        Assert.True(invalida is not null,
            $"Se esperaba OptionsValidationException con «{mensajeEsperado}»; llegó: {error}");
    }

    private static IEnumerable<Exception> Cadena(Exception? e)
    {
        for (; e is not null; e = e.InnerException)
        {
            yield return e;
            if (e is AggregateException agregada)
                foreach (var interna in agregada.InnerExceptions.SelectMany(Cadena))
                    yield return interna;
        }
    }
}
