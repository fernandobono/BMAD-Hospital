using HospitalTurnos.Aplicacion.Agendas;
using HospitalTurnos.Aplicacion.Catalogo;
using HospitalTurnos.Dominio.Agendas;
using HospitalTurnos.Infraestructura.Agendas;
using HospitalTurnos.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using static HospitalTurnos.Aplicacion.Tests.Agendas.DatosDeAgenda;

namespace HospitalTurnos.Aplicacion.Tests.Agendas;

// Contra la base real. Base compartida: cada prueba usa SUS médicos y pide sólo por ellos.
[Collection("Postgres")]
public class ConsultaAgendasTests(PostgresFixture fixture)
{
    private static readonly DateOnly Lunes2 = new(2026, 11, 2);
    private static readonly DateOnly Lunes16 = new(2026, 11, 16);

    private static int ConsultasDeAgendas(IEnumerable<string> comandos) =>
        comandos.Count(c => c.Contains("FROM agendas_medico", StringComparison.Ordinal));

    /// <summary>Las agendas A, B, C y D del T29: A aprobada desde 02/11, B aprobada desde 16/11, C enviada, D rechazada.</summary>
    private async Task<(long Medico, long A, long B)> MedicoConCuatroAgendasAsync()
    {
        await using var proveedor = fixture.Proveedor();
        var medico = await proveedor.MedicoPropioAsync();
        var a = await fixture.GuardarAgendaAsync(medico, EstadoPropuesta.Aprobada, Lunes2, null, 20, Franja("lunes", "08:00", "12:00"));
        var b = await fixture.GuardarAgendaAsync(medico, EstadoPropuesta.Aprobada, Lunes16, null, 30, Franja("lunes", "14:00", "16:00"));
        await fixture.GuardarAgendaAsync(medico, EstadoPropuesta.Enviada, new DateOnly(2026, 11, 9));
        await fixture.GuardarAgendaAsync(medico, EstadoPropuesta.Rechazada, new DateOnly(2026, 10, 26));
        return (medico, a, b);
    }

    /// <summary>Los 4 médicos del T41/T43: sin agendas; aprobada 02/11; aprobada 02/11 + enviada; sólo aprobada 16/11.</summary>
    private async Task<long[]> CuatroMedicosAsync(ServiceProvider proveedor, string apellido)
    {
        var especialidad = await proveedor.EspecialidadPropiaAsync();
        var ids = new long[4];
        for (var i = 0; i < 4; i++)
            ids[i] = await proveedor.MedicoPropioAsync(especialidad, apellido, ((char)('A' + i)).ToString());
        await fixture.GuardarAgendaAsync(ids[1], EstadoPropuesta.Aprobada, Lunes2);
        await fixture.GuardarAgendaAsync(ids[2], EstadoPropuesta.Aprobada, Lunes2);
        await fixture.GuardarAgendaAsync(ids[2], EstadoPropuesta.Enviada, new DateOnly(2026, 11, 9));
        await fixture.GuardarAgendaAsync(ids[3], EstadoPropuesta.Aprobada, Lunes16);
        return ids;
    }

    private static readonly SituacionDeAgenda[] SituacionesEsperadas =
    [
        SituacionDeAgenda.SinAgenda,
        new(EstadoAgendaMedico.Vigente),
        new(EstadoAgendaMedico.PropuestaPendiente),
        new(EstadoAgendaMedico.SinAgenda, Lunes16),
    ];

    [Theory]
    [Trait("Story", "4.1")]
    [Trait("AC", "29")]
    [Trait("AC", "20")]
    [Trait("AC", "21")]
    [Trait("AC", "31")]
    [InlineData(2, "A")]
    [InlineData(15, "A")]
    [InlineData(16, "B")]
    public async Task AgendaQueRigeAsync_SegunFecha_DevuelveLaQueRigeConSusFranjas(int dia, string esperada)
    {
        var (medico, a, b) = await MedicoConCuatroAgendasAsync();
        await using var contexto = Ayudas.ContextoDePrueba.Crear(fixture.CadenaApp);
        var consulta = new ConsultaAgendasEnBase(contexto, RelojDeEnvio());

        var rige = await consulta.AgendaQueRigeAsync(medico, new DateOnly(2026, 11, dia), CancellationToken.None);

        Assert.NotNull(rige);
        Assert.Equal(esperada == "A" ? a : b, rige.Id);
        var franja = Assert.Single(rige.Franjas);
        Assert.Equal(esperada == "A" ? new TimeOnly(8, 0) : new TimeOnly(14, 0), franja.Desde);
    }

    [Theory]
    [Trait("Story", "4.1")]
    [Trait("AC", "29")]
    [InlineData("sin agendas")]
    [InlineData("antes de todas")]
    [InlineData("sólo enviada")]
    [InlineData("inexistente")]
    public async Task AgendaQueRigeAsync_SinAgendaQueRija_DevuelveNull(string caso)
    {
        await using var proveedor = fixture.Proveedor();
        var (medico, fecha) = caso switch
        {
            "sin agendas" => (await proveedor.MedicoPropioAsync(), Lunes16),
            "antes de todas" => ((await MedicoConCuatroAgendasAsync()).Medico, new DateOnly(2026, 11, 1)),
            "sólo enviada" => (await MedicoConSoloUnaEnviadaAsync(proveedor), Lunes16),
            _ => (-1L, Lunes16),
        };
        await using var contexto = Ayudas.ContextoDePrueba.Crear(fixture.CadenaApp);

        var rige = await new ConsultaAgendasEnBase(contexto, RelojDeEnvio()).AgendaQueRigeAsync(medico, fecha, CancellationToken.None);

        Assert.Null(rige);
    }

    private async Task<long> MedicoConSoloUnaEnviadaAsync(ServiceProvider proveedor)
    {
        var medico = await proveedor.MedicoPropioAsync();
        await fixture.GuardarAgendaAsync(medico, EstadoPropuesta.Enviada, Lunes2);
        return medico;
    }

    [Fact]
    [Trait("Story", "4.1")]
    [Trait("AC", "29")]
    [Trait("AC", "22")]
    public async Task EstadosDeAgendaAsync_VariosMedicos_DevuelveUnaSituacionPorCadaId()
    {
        await using var proveedor = fixture.Proveedor();
        var ids = await CuatroMedicosAsync(proveedor, $"Agenda{Marca()}");
        var comandos = new List<string>();
        await using var contexto = new HospitalDbContext(new DbContextOptionsBuilder<HospitalDbContext>()
            .UseNpgsql(fixture.CadenaApp).LogTo(comandos.Add, [RelationalEventId.CommandExecuted]).Options);
        var consulta = new ConsultaAgendasEnBase(contexto, Reloj(2026, 11, 3));

        var estados = await consulta.EstadosDeAgendaAsync([.. ids, -1L], CancellationToken.None);

        Assert.Equal(5, estados.Count);
        for (var i = 0; i < 4; i++)
            Assert.Equal(SituacionesEsperadas[i], estados[ids[i]]);
        Assert.Equal(SituacionDeAgenda.SinAgenda, estados[-1L]);
        Assert.Equal(1, ConsultasDeAgendas(comandos));
    }

    [Fact]
    [Trait("Story", "4.1")]
    [Trait("AC", "29")]
    [Trait("AC", "30")]
    public async Task AgregarInfraestructura_ResuelveLaConsultaYLaGestionDeMedicos()
    {
        await using var proveedor = fixture.Proveedor();
        using var alcance = proveedor.CreateScope();

        Assert.IsType<ConsultaAgendasEnBase>(alcance.ServiceProvider.GetRequiredService<IConsultaAgendas>());
        Assert.NotNull(alcance.ServiceProvider.GetRequiredService<IGestionMedicos>());
    }

    [Fact]
    [Trait("Story", "4.1")]
    [Trait("AC", "30")]
    [Trait("AC", "31")]
    public async Task ListarYObtener_MedicosConAgendas_MuestranElEstadoReal()
    {
        var comandos = new List<string>();
        await using var proveedor = fixture.Proveedor(s =>
        {
            s.AddSingleton<TimeProvider>(Reloj(2026, 11, 3));
            s.ConfigureDbContext<HospitalDbContext>(o => o.LogTo(comandos.Add, [RelationalEventId.CommandExecuted]));
        });
        var apellido = $"Agenda{Marca()}";
        var ids = await CuatroMedicosAsync(proveedor, apellido);

        IReadOnlyList<MedicoListado> listado;
        using (var alcance = proveedor.CreateScope())
        {
            comandos.Clear();
            listado = await alcance.ServiceProvider.GetRequiredService<IGestionMedicos>().ListarAsync(apellido, CancellationToken.None);
        }

        Assert.Equal(ids, listado.Select(m => m.Id));
        Assert.Equal(SituacionesEsperadas, listado.Select(m => m.Agenda));
        Assert.Equal(1, ConsultasDeAgendas(comandos));
        for (var i = 0; i < 4; i++)
        {
            using var alcance = proveedor.CreateScope();
            var ficha = await alcance.ServiceProvider.GetRequiredService<IGestionMedicos>().ObtenerAsync(ids[i], CancellationToken.None);
            Assert.Equal(SituacionesEsperadas[i], ficha!.Agenda);
        }
    }
}
