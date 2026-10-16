using DotNet.Testcontainers.Configurations;
using HospitalTurnos.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HospitalTurnos.Web.Tests.Arranque;

[Collection("Postgres")]
public class RolesDeBaseTests(PostgresFixture fixture)
{
    [Fact]
    [Trait("Story", "1.1")]
    [Trait("AC", "4")]
    [Trait("AC", "6")]
    public async Task RolApp_TablaDeMigracionesDelDuenio_PuedeLeerla()
    {
        // Dos veces: volver a arrancar no puede fallar (migraciones idempotentes).
        await AplicadorDeMigraciones.AplicarAsync(fixture.CadenaDuenio);
        await AplicadorDeMigraciones.AplicarAsync(fixture.CadenaDuenio);

        await using var conexion = new NpgsqlConnection(fixture.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand("""SELECT "MigrationId" FROM "__EFMigrationsHistory" """, conexion);
        var migraciones = new List<string>();
        await using (var lector = await comando.ExecuteReaderAsync())
            while (await lector.ReadAsync())
                migraciones.Add(lector.GetString(0));

        // Todas las del proyecto, cada una una sola vez (no "una sola fila": cada story agrega la suya, story 1.3).
        await using var contexto = new HospitalDbContext(
            new DbContextOptionsBuilder<HospitalDbContext>().UseNpgsql(fixture.CadenaApp).Options);
        Assert.Equal(contexto.Database.GetMigrations().Order(), migraciones.Order());
        Assert.Contains(migraciones, m => m.EndsWith("_Inicial"));
    }

    [Fact]
    [Trait("Story", "1.1")]
    [Trait("AC", "4")]
    [Trait("AC", "5")]
    public async Task RolApp_CrearTabla_EsRechazadoPorPermisos()
    {
        await using var conexion = new NpgsqlConnection(fixture.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand("CREATE TABLE prueba (id int)", conexion);

        var error = await Assert.ThrowsAsync<PostgresException>(() => comando.ExecuteNonQueryAsync());

        Assert.Equal("42501", error.SqlState);   // insufficient_privilege
    }
}

// Contenedor propio: el script llega SIN bit ejecutable y el entrypoint de postgres lo incluye con
// "." en vez de ejecutarlo (revisión I-14). Los roles tienen que existir igual.
public class ScriptDeRolesSinBitEjecutableTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture = new(Unix.FileMode644, aplicarMigraciones: false);

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    [Trait("Story", "1.1")]
    [Trait("AC", "20")]
    public async Task ScriptDeRoles_SinBitEjecutable_CreaLosRolesIgual()
    {
        await using var conexion = new NpgsqlConnection(_fixture.CadenaSuperusuario);
        await conexion.OpenAsync();

        await using var roles = new NpgsqlCommand(
            "SELECT count(*) FROM pg_roles WHERE rolname IN ('turnos_owner','turnos_app')", conexion);
        await using var conectar = new NpgsqlCommand(
            "SELECT has_database_privilege('turnos_app','hospital_turnos','CONNECT')", conexion);

        Assert.Equal(2L, (long)(await roles.ExecuteScalarAsync())!);
        Assert.True((bool)(await conectar.ExecuteScalarAsync())!);
    }
}
