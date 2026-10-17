using HospitalTurnos.Aplicacion.Agendas;
using HospitalTurnos.Aplicacion.Catalogo;
using HospitalTurnos.Aplicacion.Comun;
using HospitalTurnos.Dominio.Agendas;
using Microsoft.Extensions.DependencyInjection;
using static HospitalTurnos.Aplicacion.Tests.Agendas.DatosDeAgenda;

namespace HospitalTurnos.Aplicacion.Tests.Catalogo;

// Story 3.3, AC 44 (decisión del docente, sesión 17): el filtro por estado de agenda se resuelve en la consulta para
// paginar y contar en la base, y la regla de EstadoDeAgenda.Calcular queda escrita también en SQL. Esta prueba
// verifica que las dos clasifican igual: un médico por cada caso de EstadoDeAgendaTests, más vigente con programada.
[Collection("Postgres")]
public class FiltroDeAgendaEnBaseTests(PostgresFixture fixture)
{
    private static string ClaveDe(SituacionDeAgenda situacion) => situacion switch
    {
        { Estado: EstadoAgendaMedico.Vigente } => "vigente",
        { Estado: EstadoAgendaMedico.PropuestaPendiente } => "pendiente",
        { Estado: EstadoAgendaMedico.SinAgenda, RigeDesde: not null } => "programada",
        _ => "sin-agenda",
    };

    [Theory]
    [Trait("Story", "3.3")]
    [Trait("AC", "44")]
    [InlineData("vigente")]
    [InlineData("programada")]
    [InlineData("pendiente")]
    [InlineData("sin-agenda")]
    public async Task FiltroDeAgendaEnLaBase_ClasificaIgualQueElDominio(string filtro)
    {
        await using var proveedor = fixture.Proveedor(s => s.AddSingleton<TimeProvider>(Reloj(2026, 10, 16)));
        var especialidad = await proveedor.EspecialidadPropiaAsync();
        var vigenteDesde = new DateOnly(2026, 10, 12);
        var aprobadoEl = Reloj(2026, 10, 1);
        var casos = new Dictionary<string, Func<long, Task>>
        {
            ["a"] = _ => Task.CompletedTask,
            ["b"] = m => fixture.GuardarAgendaAsync(m, EstadoPropuesta.Borrador, new DateOnly(2026, 11, 2)),
            ["c"] = m => fixture.GuardarAgendaAsync(m, EstadoPropuesta.Rechazada, new DateOnly(2026, 11, 2)),
            ["d"] = m => fixture.GuardarAgendaAsync(m, EstadoPropuesta.Aprobada, vigenteDesde, aprobadoEl),
            ["e"] = m => fixture.GuardarAgendaAsync(m, EstadoPropuesta.Aprobada, new DateOnly(2026, 11, 2)),
            ["f"] = async m =>
            {
                await fixture.GuardarAgendaAsync(m, EstadoPropuesta.Aprobada, vigenteDesde, aprobadoEl);
                await fixture.GuardarAgendaAsync(m, EstadoPropuesta.Enviada, new DateOnly(2026, 11, 9));
            },
            ["g"] = m => fixture.GuardarAgendaAsync(m, EstadoPropuesta.Enviada, new DateOnly(2026, 11, 9)),
            ["h"] = async m =>
            {
                await fixture.GuardarAgendaAsync(m, EstadoPropuesta.Aprobada, new DateOnly(2026, 11, 16));
                await fixture.GuardarAgendaAsync(m, EstadoPropuesta.Aprobada, new DateOnly(2026, 11, 2));
            },
            ["vigente-con-programada"] = async m =>
            {
                await fixture.GuardarAgendaAsync(m, EstadoPropuesta.Aprobada, vigenteDesde, aprobadoEl);
                await fixture.GuardarAgendaAsync(m, EstadoPropuesta.Aprobada, new DateOnly(2026, 11, 2));
            },
        };
        var medicos = new List<long>();
        foreach (var (caso, armar) in casos)
        {
            var medico = await proveedor.MedicoPropioAsync(especialidad, $"Caso-{caso}");
            await armar(medico);
            medicos.Add(medico);
        }

        using var alcance = proveedor.CreateScope();
        var segunElDominio = await alcance.ServiceProvider.GetRequiredService<IConsultaAgendas>()
            .EstadosDeAgendaAsync(medicos, CancellationToken.None);
        var consulta = new ConsultaDeListado(null,
            new Dictionary<string, string> { ["especialidad"] = especialidad.ToString(), ["agenda"] = filtro }, "medico", 1, 100);
        var segunLaBase = await alcance.ServiceProvider.GetRequiredService<IGestionMedicos>()
            .ListarPaginadoAsync(consulta, CancellationToken.None);

        var esperados = medicos.Where(m => ClaveDe(segunElDominio[m]) == filtro).Order().ToList();
        Assert.NotEmpty(esperados);
        Assert.Equal(esperados, segunLaBase.Items.Select(m => m.Id).Order());
        Assert.Equal(esperados.Count, segunLaBase.Total);
    }
}
