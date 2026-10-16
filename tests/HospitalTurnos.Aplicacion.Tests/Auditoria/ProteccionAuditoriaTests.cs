using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Tests.Ayudas;
using HospitalTurnos.Dominio.Auditoria;
using HospitalTurnos.Infraestructura.Auditoria;
using Microsoft.Extensions.Time.Testing;
using Npgsql;

namespace HospitalTurnos.Aplicacion.Tests.Auditoria;

// ADR-07: el registro se protege en la base, no sólo en el código. Los permisos frenan a la app;
// el disparador frena a cualquier rol mientras esté activo, también al dueño.
[Collection("Postgres")]
public class ProteccionAuditoriaTests(PostgresFixture fixture)
{
    // Siempre sobre una fila existente: un DELETE que no encuentra filas no dispara el disparador por fila y "pasa".
    public static TheoryData<string> Cambios() => new()
    {
        "UPDATE registros_auditoria SET quien = 'X' WHERE id = @id",
        "DELETE FROM registros_auditoria WHERE id = @id",
        "TRUNCATE registros_auditoria",
    };

    private async Task<long> InsertarFilaAsync()
    {
        await using var contexto = ContextoDePrueba.Crear(fixture.CadenaApp);
        var actor = new ActorAuditoria("u-1", "Ana Pérez", "Recepcionista", CanalAuditoria.Recepcion);
        new AuditoriaEnBase(contexto, new FakeTimeProvider(new DateTimeOffset(2026, 10, 12, 11, 0, 0, TimeSpan.Zero)))
            .Registrar(actor, AccionesAuditoria.TurnoReservado, "Turno", $"T-{Guid.NewGuid():N}");
        await contexto.SaveChangesAsync();
        return contexto.RegistrosAuditoria.Local.Single().Id;
    }

    private static async Task<PostgresException> IntentarCambioAsync(string cadena, string sql, long id)
    {
        await using var conexion = new NpgsqlConnection(cadena);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("id", id);
        return await Assert.ThrowsAsync<PostgresException>(() => comando.ExecuteNonQueryAsync());
    }

    private async Task<string?> QuienDeLaFilaAsync(long id)
    {
        await using var conexion = new NpgsqlConnection(fixture.CadenaDuenio);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand("SELECT quien FROM registros_auditoria WHERE id = @id", conexion);
        comando.Parameters.AddWithValue("id", id);
        return (string?)await comando.ExecuteScalarAsync();
    }

    private async Task<List<string>> ListaAsync(string sql)
    {
        await using var conexion = new NpgsqlConnection(fixture.CadenaDuenio);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        await using var lector = await comando.ExecuteReaderAsync();
        var valores = new List<string>();
        while (await lector.ReadAsync())
            valores.Add(lector.GetString(0));
        return valores;
    }

    [Theory]
    [MemberData(nameof(Cambios))]
    [Trait("Story", "1.3")]
    [Trait("AC", "7")]
    public async Task ModificarRegistro_ComoTurnosApp_RechazadoPorPermisos(string sql)
    {
        var id = await InsertarFilaAsync();

        var error = await IntentarCambioAsync(fixture.CadenaApp, sql, id);

        Assert.Equal("42501", error.SqlState);
        Assert.Equal("Ana Pérez", await QuienDeLaFilaAsync(id));
    }

    [Theory]
    [MemberData(nameof(Cambios))]
    [Trait("Story", "1.3")]
    [Trait("AC", "8")]
    public async Task ModificarRegistro_ComoTurnosOwner_RechazadoPorDisparador(string sql)
    {
        var id = await InsertarFilaAsync();

        var error = await IntentarCambioAsync(fixture.CadenaDuenio, sql, id);

        Assert.Equal("P0001", error.SqlState);
        Assert.Contains("registros_auditoria es de sólo inserción", error.MessageText);
        Assert.Equal("Ana Pérez", await QuienDeLaFilaAsync(id));
    }

    [Fact]
    [Trait("Story", "1.3")]
    [Trait("AC", "9")]
    public async Task Migracion_RegistrosAuditoria_SoloOtorgaInsertYSelectATurnosApp()
    {
        // Verificación previa (hallazgo C-1): sin privilegios por defecto activos, esta prueba pasaría aunque
        // se borrara el REVOKE de la migración. __EFMigrationsHistory la crea el mismo dueño, sin REVOKE.
        var previa = await ListaAsync(
            """SELECT has_table_privilege('turnos_app', '"__EFMigrationsHistory"', 'UPDATE')::text""");
        Assert.True(previa.Single() == "true",
            "La fixture no reproduce los privilegios por defecto de 01-roles.sh: la prueba no demostraría nada.");

        var privilegios = await ListaAsync(
            """
            SELECT privilege_type FROM information_schema.role_table_grants
            WHERE grantee = 'turnos_app' AND table_name = 'registros_auditoria'
            """);
        var puedeActualizar = await ListaAsync(
            "SELECT has_table_privilege('turnos_app', 'registros_auditoria', 'UPDATE')::text");
        var disparadores = await ListaAsync(
            "SELECT tgname::text FROM pg_trigger WHERE tgrelid = 'registros_auditoria'::regclass AND NOT tgisinternal");

        Assert.Equal(["INSERT", "SELECT"], privilegios.Order());
        Assert.Equal("false", puedeActualizar.Single());
        Assert.Equal(["auditoria_sin_truncate", "auditoria_sin_update_ni_delete"], disparadores.Order());
    }
}
