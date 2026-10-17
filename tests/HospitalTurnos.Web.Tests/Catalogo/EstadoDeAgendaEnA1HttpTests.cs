using System.Globalization;
using System.Net;
using HospitalTurnos.Dominio.Agendas;
using HospitalTurnos.Infraestructura.Persistencia;
using HospitalTurnos.Web.Tests.Personal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using static HospitalTurnos.Web.Tests.Personal.FormularioHttp;

namespace HospitalTurnos.Web.Tests.Catalogo;

// Story 4.1, AC 30: el estado REAL de agenda llega a A1 (decisión 8). Las agendas se arman con los métodos
// públicos del dominio y relojes falsos derivados de app.Reloj: nunca se espera tiempo real, y app.Reloj no se
// adelanta (cortaría la sesión de admin1). Base compartida: médicos propios con apellido Prueba<marca>.
[Collection("Postgres")]
public class EstadoDeAgendaEnA1HttpTests(PostgresFixture fixture)
{
    private static FakeTimeProvider RelojCorridoDias(FakeTimeProvider base_, int dias)
    {
        var reloj = new FakeTimeProvider(base_.GetUtcNow().AddDays(dias));
        reloj.SetLocalTimeZone(base_.LocalTimeZone);
        return reloj;
    }

    private static string? FilaCon(string html, string texto)
    {
        var celda = html.IndexOf(texto, StringComparison.Ordinal);
        if (celda < 0)
            return null;
        var inicio = html.LastIndexOf("<tr", celda, StringComparison.Ordinal);
        var fin = html.IndexOf("</tr>", celda, StringComparison.Ordinal);
        return html[inicio..fin];
    }

    [Fact]
    [Trait("Story", "4.1")]
    [Trait("AC", "22")]
    [Trait("AC", "30")]
    public async Task GetMedicos_AgendasReales_MuestranVigentePendienteYSinAgendaConFecha()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var marca = Guid.NewGuid().ToString("N")[..8];
        var especialidad = await app.CrearEspecialidadAsync(nombre: $"Esp{marca}");
        var a = await app.CrearMedicoAsync("A", $"Prueba{marca}", $"T-{marca}-A", especialidad);
        var b = await app.CrearMedicoAsync("B", $"Prueba{marca}", $"T-{marca}-B", especialidad);
        var c = await app.CrearMedicoAsync("C", $"Prueba{marca}", $"T-{marca}-C", especialidad);
        var hoy = DateOnly.FromDateTime(app.Reloj.GetLocalNow().DateTime);   // fecha del hospital

        // A rige hoy: enviada y aprobada 30 días antes, desde hoy − 20 (cumple los 7 días y no estaba pasada).
        var haceUnMes = RelojCorridoDias(app.Reloj, -30);
        var agendaA = Agenda(a, hoy.AddDays(-20));
        agendaA.Enviar(haceUnMes);
        agendaA.Aprobar(haceUnMes);
        var agendaB = Agenda(b, hoy.AddDays(10));
        agendaB.Enviar(app.Reloj);
        var agendaC = Agenda(c, hoy.AddDays(30));
        agendaC.Enviar(app.Reloj);
        agendaC.Aprobar(app.Reloj);
        using (var alcance = app.Services.CreateScope())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<HospitalDbContext>();
            contexto.AgendasMedico.AddRange(agendaA, agendaB, agendaC);
            await contexto.SaveChangesAsync();
        }
        var cliente = Cliente(app);
        await IngresarAsync(cliente, PersonalWebApplicationFactory.Admin.Usuario, PersonalWebApplicationFactory.Admin.Contrasena);

        var respuesta = await cliente.GetAsync($"/personal/admin/medicos?buscar={marca}");
        var html = WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("Vigente", FilaCon(html, $"T-{marca.ToUpperInvariant()}-A"));
        Assert.Contains("Propuesta pendiente", FilaCon(html, $"T-{marca.ToUpperInvariant()}-B"));
        var fecha = agendaC.VigenteDesde.ToString("dd/MM", CultureInfo.InvariantCulture);
        Assert.Contains($"Sin agenda (rige desde {fecha})", FilaCon(html, $"T-{marca.ToUpperInvariant()}-C"));
    }

    private static AgendaMedico Agenda(long medicoId, DateOnly desde)
    {
        var agenda = new AgendaMedico(medicoId, 20, desde);
        agenda.AgregarFranja(new Franja(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0)));
        return agenda;
    }
}
