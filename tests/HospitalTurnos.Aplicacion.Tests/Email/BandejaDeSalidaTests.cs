using HospitalTurnos.Aplicacion.Email;
using HospitalTurnos.Aplicacion.Tests.Ayudas;
using HospitalTurnos.Infraestructura.Email;
using Microsoft.Extensions.Time.Testing;
using Npgsql;

namespace HospitalTurnos.Aplicacion.Tests.Email;

// Contra la base real: el mensaje tiene que viajar en el mismo SaveChangesAsync que el caso de uso.
[Collection("Postgres")]
public class BandejaDeSalidaTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 12, 11, 0, 0, TimeSpan.Zero);

    public Task InitializeAsync() => fixture.LimpiarBandejaAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static FakeTimeProvider RelojFijo()
    {
        var reloj = new FakeTimeProvider(Ahora);
        reloj.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Cordoba"));
        return reloj;
    }

    private static MensajeEmail Mensaje() =>
        new("ana@ejemplo.com", "Asunto", "texto", "<p>html</p>", "codigo-ingreso", "P-1");

    [Fact]
    [Trait("Story", "1.4")]
    [Trait("AC", "1")]
    public async Task Encolar_ConSaveChanges_QuedaPendienteParaEnvioInmediato()
    {
        await using (var contexto = ContextoDePrueba.Crear(fixture.CadenaApp))
        {
            new BandejaDeSalidaEnBase(contexto, RelojFijo()).Encolar(Mensaje());
            await contexto.SaveChangesAsync();
        }

        await using var conexion = new NpgsqlConnection(fixture.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(
            """
            SELECT para, asunto, cuerpo_texto, cuerpo_html, tipo, referencia_id, intentos, creado_el,
                   proximo_intento_el, enviado_el, fallo_definitivo_el, manejado_el, ultimo_error
            FROM mensajes_salientes
            """, conexion);
        await using var lector = await comando.ExecuteReaderAsync();

        Assert.True(await lector.ReadAsync(), "no quedó ninguna fila");
        Assert.Equal("ana@ejemplo.com", lector.GetString(0));
        Assert.Equal("Asunto", lector.GetString(1));
        Assert.Equal("texto", lector.GetString(2));
        Assert.Equal("<p>html</p>", lector.GetString(3));
        Assert.Equal("codigo-ingreso", lector.GetString(4));
        Assert.Equal("P-1", lector.GetString(5));
        Assert.Equal(0, lector.GetInt32(6));
        Assert.Equal(Ahora, lector.GetFieldValue<DateTimeOffset>(7));
        Assert.Equal(Ahora, lector.GetFieldValue<DateTimeOffset>(8));
        Assert.True(lector.IsDBNull(9), "enviado_el debería ser NULL");
        Assert.True(lector.IsDBNull(10), "fallo_definitivo_el debería ser NULL");
        Assert.True(lector.IsDBNull(11), "manejado_el debería ser NULL");
        Assert.True(lector.IsDBNull(12), "ultimo_error debería ser NULL");
        Assert.False(await lector.ReadAsync(), "quedó más de una fila");
    }

    [Fact]
    [Trait("Story", "1.4")]
    [Trait("AC", "2")]
    public async Task Encolar_SinGuardar_NoPersisteNada()
    {
        await using (var contexto = ContextoDePrueba.Crear(fixture.CadenaApp))
        {
            new BandejaDeSalidaEnBase(contexto, RelojFijo()).Encolar(Mensaje());
        }

        await using var conexion = new NpgsqlConnection(fixture.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand("SELECT count(*) FROM mensajes_salientes", conexion);
        Assert.Equal(0L, (long)(await comando.ExecuteScalarAsync())!);
    }
}
