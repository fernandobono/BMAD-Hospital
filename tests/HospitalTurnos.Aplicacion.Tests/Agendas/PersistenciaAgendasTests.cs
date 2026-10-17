using HospitalTurnos.Aplicacion.Tests.Ayudas;
using HospitalTurnos.Dominio.Agendas;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using static HospitalTurnos.Aplicacion.Tests.Agendas.DatosDeAgenda;

namespace HospitalTurnos.Aplicacion.Tests.Agendas;

// Contra la base real: tipos, permisos, índices, controles y concurrencia viven ahí. Base compartida: cada
// prueba usa SUS médicos y filtra por ellos; nunca se limpian tablas.
[Collection("Postgres")]
public class PersistenciaAgendasTests(PostgresFixture fixture)
{
    private static readonly DateOnly Lunes2 = new(2026, 11, 2);

    private async Task<object?> EscalarAsync(string sql, string? cadena = null)
    {
        await using var conexion = new NpgsqlConnection(cadena ?? fixture.CadenaDuenio);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        return await comando.ExecuteScalarAsync();
    }

    private async Task<long> MedicoPropioAsync()
    {
        await using var proveedor = fixture.Proveedor();
        return await proveedor.MedicoPropioAsync();
    }

    [Fact]
    [Trait("Story", "4.1")]
    [Trait("AC", "23")]
    [Trait("AC", "24")]
    [Trait("AC", "32")]
    public async Task Migracion_AgregarAgendas_CreaTablasColumnasYPermisos()
    {
        var esperadas = new Dictionary<string, string>
        {
            ["agendas_medico.id"] = "bigint NO",
            ["agendas_medico.medico_id"] = "bigint NO",
            ["agendas_medico.duracion_turno_minutos"] = "integer NO",
            ["agendas_medico.vigente_desde"] = "date NO",
            ["agendas_medico.estado"] = "character varying(20) NO",
            ["agendas_medico.motivo_rechazo"] = "character varying(500) YES",
            ["agendas_medico.enviada_el"] = "timestamp with time zone YES",
            ["agendas_medico.aprobada_el"] = "timestamp with time zone YES",
            ["agendas_medico.rechazada_el"] = "timestamp with time zone YES",
            ["franjas_agenda.id"] = "bigint NO",
            ["franjas_agenda.agenda_medico_id"] = "bigint NO",
            ["franjas_agenda.dia"] = "integer NO",
            ["franjas_agenda.desde"] = "time without time zone NO",
            ["franjas_agenda.hasta"] = "time without time zone NO",
        };
        var reales = new Dictionary<string, string>();
        await using (var conexion = new NpgsqlConnection(fixture.CadenaDuenio))
        {
            await conexion.OpenAsync();
            await using var comando = new NpgsqlCommand("""
                SELECT table_name || '.' || column_name,
                       data_type || coalesce('(' || character_maximum_length || ')', '') || ' ' || is_nullable
                FROM information_schema.columns
                WHERE table_name IN ('agendas_medico', 'franjas_agenda')
                """, conexion);
            await using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
                reales[lector.GetString(0)] = lector.GetString(1);
        }

        Assert.Equal(esperadas.OrderBy(x => x.Key), reales.OrderBy(x => x.Key));   // ni más ni menos: sin columna "version"
        foreach (var privilegio in new[] { "SELECT", "INSERT", "UPDATE" })
            Assert.Equal(true, await EscalarAsync($"SELECT has_table_privilege('turnos_app', 'agendas_medico', '{privilegio}')"));
        Assert.Equal(false, await EscalarAsync("SELECT has_table_privilege('turnos_app', 'agendas_medico', 'DELETE')"));
        Assert.Equal(true, await EscalarAsync("SELECT has_table_privilege('turnos_app', 'franjas_agenda', 'DELETE')"));
        Assert.Equal(false, await EscalarAsync("SELECT has_table_privilege('turnos_app', 'registros_auditoria', 'UPDATE')"));
    }

    [Fact]
    [Trait("Story", "4.1")]
    [Trait("AC", "23")]
    [Trait("AC", "28")]
    public async Task Guardar_AgendaConFranjas_SeLeeIgual()
    {
        var medico = await MedicoPropioAsync();
        var original = ArmarAgenda(medico, EstadoPropuesta.Rechazada, Lunes2, RelojDeEnvio(), 20,
            Franja("lunes", "08:00", "12:00"), Franja("lunes", "20:00", "00:00"));
        await using (var contexto = ContextoDePrueba.Crear(fixture.CadenaApp))
        {
            contexto.AgendasMedico.Add(original);
            await contexto.SaveChangesAsync();
        }

        await using var otro = ContextoDePrueba.Crear(fixture.CadenaApp);
        var leida = await otro.AgendasMedico.Include(a => a.Franjas).SingleAsync(a => a.Id == original.Id);

        Assert.Equal(medico, leida.MedicoId);
        Assert.Equal(20, leida.DuracionTurnoMinutos);
        Assert.Equal(Lunes2, leida.VigenteDesde);
        Assert.Equal(EstadoPropuesta.Rechazada, leida.Estado);
        Assert.Equal("Motivo de prueba", leida.MotivoRechazo);
        Assert.Equal(original.EnviadaEl, leida.EnviadaEl);
        Assert.Equal(original.RechazadaEl, leida.RechazadaEl);
        Assert.Equal(["08:00-12:00", "20:00-00:00"],
            leida.Franjas.OrderBy(f => f.Desde).Select(f => $"{f.Desde:HH\\:mm}-{f.Hasta:HH\\:mm}"));
        Assert.All(leida.Franjas, f => Assert.Equal(DayOfWeek.Monday, f.Dia));
        Assert.Equal("Rechazada", await EscalarAsync($"SELECT estado FROM agendas_medico WHERE id = {original.Id}"));
    }

    [Fact]
    [Trait("Story", "4.1")]
    [Trait("AC", "24")]
    [Trait("AC", "28")]
    public async Task QuitarFranja_EnBorradorGuardado_BorraLaFila()
    {
        var medico = await MedicoPropioAsync();
        var id = await fixture.GuardarAgendaAsync(medico, EstadoPropuesta.Borrador, Lunes2, null, 20,
            Franja("lunes", "08:00", "12:00"), Franja("martes", "08:00", "12:00"));

        await using (var contexto = ContextoDePrueba.Crear(fixture.CadenaApp))
        {
            var agenda = await contexto.AgendasMedico.Include(a => a.Franjas).SingleAsync(a => a.Id == id);
            agenda.QuitarFranja(DayOfWeek.Tuesday, new TimeOnly(8, 0));
            await contexto.SaveChangesAsync();
        }

        Assert.Equal(1L, await EscalarAsync($"SELECT count(*) FROM franjas_agenda WHERE agenda_medico_id = {id}"));
    }

    [Fact]
    [Trait("Story", "4.1")]
    [Trait("AC", "25")]
    [Trait("AC", "31")]
    public async Task Base_DosEnviadasDelMismoMedico_ElIndiceRechazaLaSegunda()
    {
        var m1 = await MedicoPropioAsync();
        var m2 = await MedicoPropioAsync();
        var primera = await fixture.GuardarAgendaAsync(m1, EstadoPropuesta.Enviada, Lunes2);

        var error = await Assert.ThrowsAsync<DbUpdateException>(
            () => fixture.GuardarAgendaAsync(m1, EstadoPropuesta.Enviada, new DateOnly(2026, 11, 9)));
        var postgres = Assert.IsType<PostgresException>(error.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
        Assert.Equal("ux_agendas_medico_una_enviada", postgres.ConstraintName);

        await fixture.GuardarAgendaAsync(m2, EstadoPropuesta.Enviada, Lunes2);   // otro médico, la suya
        await using (var contexto = ContextoDePrueba.Crear(fixture.CadenaApp))
        {
            var agenda = await contexto.AgendasMedico.SingleAsync(a => a.Id == primera);
            agenda.Rechazar("Otra fecha", RelojDeEnvio());
            await contexto.SaveChangesAsync();
        }
        var segunda = await fixture.GuardarAgendaAsync(m1, EstadoPropuesta.Enviada, new DateOnly(2026, 11, 9));

        Assert.True(segunda > 0);
    }

    [Theory]
    [Trait("Story", "4.1")]
    [Trait("AC", "26")]
    [InlineData("duracion", "ck_agendas_medico_duracion")]
    [InlineData("dia", "ck_franjas_agenda_dia")]
    [InlineData("horas", "ck_franjas_agenda_horas")]
    public async Task Base_ValoresFueraDeRango_LosRechazanLosControles(string caso, string control)
    {
        var medico = await MedicoPropioAsync();
        var agenda = await fixture.GuardarAgendaAsync(medico, EstadoPropuesta.Borrador, Lunes2);
        var sql = caso switch
        {
            "duracion" => $"INSERT INTO agendas_medico (medico_id, duracion_turno_minutos, vigente_desde, estado) VALUES ({medico}, 5, '2026-11-02', 'Borrador')",
            "dia" => $"INSERT INTO franjas_agenda (agenda_medico_id, dia, desde, hasta) VALUES ({agenda}, 7, '08:00', '12:00')",
            _ => $"INSERT INTO franjas_agenda (agenda_medico_id, dia, desde, hasta) VALUES ({agenda}, 1, '12:00', '08:00')",
        };

        var error = await Assert.ThrowsAsync<PostgresException>(() => EscalarAsync(sql, fixture.CadenaApp));

        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
        Assert.Equal(control, error.ConstraintName);
    }

    [Fact]
    [Trait("Story", "4.1")]
    [Trait("AC", "27")]
    public async Task Guardar_DosCambiosSobreLaMismaAgenda_ElSegundoFallaPorConcurrencia()
    {
        var medico = await MedicoPropioAsync();
        var id = await fixture.GuardarAgendaAsync(medico, EstadoPropuesta.Enviada, Lunes2);
        await using var contexto1 = ContextoDePrueba.Crear(fixture.CadenaApp);
        await using var contexto2 = ContextoDePrueba.Crear(fixture.CadenaApp);
        var vista1 = await contexto1.AgendasMedico.SingleAsync(a => a.Id == id);
        var vista2 = await contexto2.AgendasMedico.SingleAsync(a => a.Id == id);

        vista1.Aprobar(RelojDeEnvio());
        await contexto1.SaveChangesAsync();
        vista2.Rechazar("Tarde", RelojDeEnvio());

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => contexto2.SaveChangesAsync());
        Assert.Equal("Aprobada", await EscalarAsync($"SELECT estado FROM agendas_medico WHERE id = {id}"));
        Assert.Equal(DBNull.Value, await EscalarAsync($"SELECT motivo_rechazo FROM agendas_medico WHERE id = {id}"));
    }
}
