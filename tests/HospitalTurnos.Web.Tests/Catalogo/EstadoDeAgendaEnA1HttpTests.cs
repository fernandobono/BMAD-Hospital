using System.Globalization;
using Bunit;
using Bunit.TestDoubles;
using HospitalTurnos.Dominio.Agendas;
using HospitalTurnos.Infraestructura.Persistencia;
using HospitalTurnos.Web.Components.Personal.Administracion;
using HospitalTurnos.Web.Personal;
using HospitalTurnos.Web.Tests.Ayudas;
using HospitalTurnos.Web.Tests.Personal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace HospitalTurnos.Web.Tests.Catalogo;

// Story 4.1, AC 30: el estado REAL de agenda llega a A1 (decisión 8). Las agendas se arman con los métodos
// públicos del dominio y relojes falsos derivados de app.Reloj: nunca se espera tiempo real, y app.Reloj no se
// adelanta. Base compartida: médicos propios con apellido Prueba<marca>. Story 3.3: A1 es interactiva (sin
// prerender), así que el listado se mira como componente y el estado sale como badge (AC 61).
[Collection("Postgres")]
public class EstadoDeAgendaEnA1HttpTests(PostgresFixture fixture) : BunitContext
{
    private static FakeTimeProvider RelojCorridoDias(FakeTimeProvider base_, int dias)
    {
        var reloj = new FakeTimeProvider(base_.GetUtcNow().AddDays(dias));
        reloj.SetLocalTimeZone(base_.LocalTimeZone);
        return reloj;
    }

    [Fact]
    [Trait("Story", "4.1")]
    [Trait("AC", "22")]
    [Trait("AC", "30")]
    [Trait("Story", "3.3")]
    [Trait("AC", "61")]
    public async Task Medicos_AgendasReales_MuestranVigentePendienteYProgramadaConFecha()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        using var alcance = this.ConLaApp(app);
        await this.ComoAdmin1Async(app);
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
        using (var guardado = app.Services.CreateScope())
        {
            var contexto = guardado.ServiceProvider.GetRequiredService<HospitalDbContext>();
            contexto.AgendasMedico.AddRange(agendaA, agendaB, agendaC);
            await contexto.SaveChangesAsync();
        }
        Services.GetRequiredService<BunitNavigationManager>().NavigateTo($"{RutasPersonal.Medicos}?buscar={marca}");

        var pagina = Render<Medicos>();
        await pagina.WaitForAssertionAsync(() => Assert.Equal(3, pagina.FindAll("table.tabla-abm tbody tr:not(.esqueleto)").Count));

        string Estado(string matricula) => pagina.FindAll("table.tabla-abm tbody tr")
            .Single(f => f.TextContent.Contains(matricula, StringComparison.Ordinal)).QuerySelectorAll("td")[3].TextContent.Trim();
        Assert.Equal("Vigente", Estado($"T-{marca.ToUpperInvariant()}-A"));
        Assert.Equal("Pendiente de aprobación", Estado($"T-{marca.ToUpperInvariant()}-B"));
        var fecha = agendaC.VigenteDesde.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        Assert.Equal($"Programada · desde {fecha}", Estado($"T-{marca.ToUpperInvariant()}-C"));
    }

    private static AgendaMedico Agenda(long medicoId, DateOnly desde)
    {
        var agenda = new AgendaMedico(medicoId, 20, desde);
        agenda.AgregarFranja(new Franja(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0)));
        return agenda;
    }
}
