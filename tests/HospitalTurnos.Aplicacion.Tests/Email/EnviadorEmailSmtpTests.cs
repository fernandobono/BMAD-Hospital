using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using HospitalTurnos.Aplicacion.Email;
using HospitalTurnos.Infraestructura.Email;
using MailKit.Security;
using Microsoft.Extensions.Options;

namespace HospitalTurnos.Aplicacion.Tests.Email;

/// <summary>
/// La única prueba que ejercita MailKit de verdad (conexión, remitente, cuerpo texto + HTML): sin ella,
/// EnviadorEmailSmtp llegaría a producción sin haber enviado nunca un email. Mailpit en su propio
/// contenedor, igual que el proyecto guía levanta su dependencia externa en InitializeAsync.
/// </summary>
public class EnviadorEmailSmtpTests : IAsyncLifetime
{
    // Mismo tag que docker-compose.yml.
    private readonly IContainer _mailpit = new ContainerBuilder("axllent/mailpit")
        .WithPortBinding(1025, true)
        .WithPortBinding(8025, true)
        .WithWaitStrategy(Wait.ForUnixContainer()
            .UntilHttpRequestIsSucceeded(r => r.ForPort(8025).ForPath("/api/v1/messages")))
        .Build();

    public Task InitializeAsync() => _mailpit.StartAsync();

    public Task DisposeAsync() => _mailpit.DisposeAsync().AsTask();

    private static EnviadorEmailSmtp Enviador(string host, int puerto) =>
        new(Options.Create(new OpcionesSmtp
        {
            Host = host,
            Puerto = puerto,
            Seguridad = SecureSocketOptions.None,
            RemitenteNombre = "Hospital Las Acacias",
            RemitenteDireccion = "turnos@lasacacias.example",
        }));

    [Fact]
    [Trait("Story", "1.4")]
    [Trait("AC", "14")]
    public async Task EnviarAsync_ContraMailpit_ElMensajeLlegaConAsuntoYDestinatario()
    {
        var mensaje = new MensajeEmail("ana@ejemplo.com", "Tu código de ingreso", "texto", "<p>html</p>", "codigo-ingreso", "P-1");

        await Enviador(_mailpit.Hostname, _mailpit.GetMappedPublicPort(1025)).EnviarAsync(mensaje, CancellationToken.None);

        using var http = new HttpClient { BaseAddress = new Uri($"http://{_mailpit.Hostname}:{_mailpit.GetMappedPublicPort(8025)}") };
        var respuesta = await http.GetFromJsonAsync<JsonElement>("/api/v1/messages");
        Assert.Equal(1, respuesta.GetProperty("total").GetInt32());
        var recibido = respuesta.GetProperty("messages")[0];
        Assert.Equal("Tu código de ingreso", recibido.GetProperty("Subject").GetString());
        Assert.Equal("ana@ejemplo.com", recibido.GetProperty("To")[0].GetProperty("Address").GetString());
        Assert.Equal("turnos@lasacacias.example", recibido.GetProperty("From").GetProperty("Address").GetString());
    }

    [Fact]
    [Trait("Story", "1.4")]
    [Trait("AC", "15")]
    public async Task EnviarAsync_ServidorInaccesible_LanzaExcepcion()
    {
        var mensaje = new MensajeEmail("ana@ejemplo.com", "Asunto", "texto", "<p>html</p>", "codigo-ingreso", null);

        // Puerto 1: nadie escucha. El enviador no traga el error: el procesador lo convierte en reintento.
        await Assert.ThrowsAnyAsync<Exception>(() => Enviador("127.0.0.1", 1).EnviarAsync(mensaje, CancellationToken.None));
    }
}
