using System.Text.Json;
using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Tests.Ayudas;
using HospitalTurnos.Dominio.Auditoria;
using HospitalTurnos.Infraestructura.Auditoria;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Npgsql;

namespace HospitalTurnos.Aplicacion.Tests.Auditoria;

// Contra la base real de las migraciones: los permisos y el disparador viven ahí, un mock no los tiene.
// La tabla no se puede truncar, así que cada prueba filtra por un EntidadId propio.
[Collection("Postgres")]
public class AuditoriaEnBaseTests(PostgresFixture fixture)
{
    private static readonly ActorAuditoria Actor =
        new(UsuarioId: "u-1", Quien: "Ana Pérez", Rol: "Recepcionista", Canal: CanalAuditoria.Recepcion);

    private static FakeTimeProvider RelojFijo()
    {
        var reloj = new FakeTimeProvider(new DateTimeOffset(2026, 10, 12, 11, 0, 0, TimeSpan.Zero));
        reloj.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Cordoba"));
        return reloj;
    }

    private static string EntidadIdPropio() => $"T-{Guid.NewGuid():N}";

    private async Task<long> ContarAsync(string entidadId)
    {
        await using var conexion = new NpgsqlConnection(fixture.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(
            "SELECT count(*) FROM registros_auditoria WHERE entidad_id = @id", conexion);
        comando.Parameters.AddWithValue("id", entidadId);
        return (long)(await comando.ExecuteScalarAsync())!;
    }

    [Fact]
    [Trait("Story", "1.3")]
    [Trait("AC", "5")]
    public async Task Registrar_SinGuardar_NoPersisteNada()
    {
        var entidadId = EntidadIdPropio();
        await using (var contexto = ContextoDePrueba.Crear(fixture.CadenaApp))
        {
            new AuditoriaEnBase(contexto, RelojFijo())
                .Registrar(Actor, AccionesAuditoria.TurnoReservado, "Turno", entidadId);
        }

        Assert.Equal(0, await ContarAsync(entidadId));
    }

    [Fact]
    [Trait("Story", "1.3")]
    [Trait("AC", "4")]
    public async Task Registrar_ConSaveChanges_PersisteElRegistroCompletoComoTurnosApp()
    {
        var entidadId = EntidadIdPropio();
        await using (var contexto = ContextoDePrueba.Crear(fixture.CadenaApp))
        {
            new AuditoriaEnBase(contexto, RelojFijo())
                .Registrar(Actor, AccionesAuditoria.TurnoReservado, "Turno", entidadId, new { turnoId = entidadId });
            await contexto.SaveChangesAsync();
        }

        await using var conexion = new NpgsqlConnection(fixture.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(
            """
            SELECT cuando, usuario_id, quien, rol, canal, accion, entidad_tipo, detalle::text
            FROM registros_auditoria WHERE entidad_id = @id
            """, conexion);
        comando.Parameters.AddWithValue("id", entidadId);
        await using var lector = await comando.ExecuteReaderAsync();

        Assert.True(await lector.ReadAsync(), "no quedó ninguna fila");
        Assert.Equal(new DateTimeOffset(2026, 10, 12, 11, 0, 0, TimeSpan.Zero), lector.GetFieldValue<DateTimeOffset>(0));
        Assert.Equal("u-1", lector.GetString(1));
        Assert.Equal("Ana Pérez", lector.GetString(2));
        Assert.Equal("Recepcionista", lector.GetString(3));
        Assert.Equal("Recepcion", lector.GetString(4)); // texto, no número
        Assert.Equal("turno.reservado", lector.GetString(5));
        Assert.Equal("Turno", lector.GetString(6));
        // jsonb reordena claves y quita espacios: se compara parseado, nunca como texto.
        using var detalle = JsonDocument.Parse(lector.GetString(7));
        Assert.Equal(entidadId, detalle.RootElement.GetProperty("turnoId").GetString());
        Assert.False(await lector.ReadAsync(), "quedó más de una fila");
    }

    [Fact]
    [Trait("Story", "1.3")]
    [Trait("AC", "6")]
    public async Task SaveChangesAsync_UnaFilaDelMismoGuardadoFalla_NoQuedaNingunRegistro()
    {
        var entidadId = EntidadIdPropio();
        await using (var contexto = ContextoDePrueba.Crear(fixture.CadenaApp))
        {
            var auditoria = new AuditoriaEnBase(contexto, RelojFijo());
            auditoria.Registrar(Actor, AccionesAuditoria.TurnoReservado, "Turno", entidadId);
            auditoria.Registrar(Actor, new string('a', 101), "Turno", entidadId); // la columna admite 100

            var error = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
            Assert.Equal("22001", Assert.IsType<PostgresException>(error.InnerException).SqlState);
        }

        Assert.Equal(0, await ContarAsync(entidadId));
    }
}
