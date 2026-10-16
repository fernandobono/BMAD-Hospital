using Npgsql;

namespace HospitalTurnos.Aplicacion.Tests.Email;

public static class LimpiezaDeBandeja
{
    // La bandeja sí se puede vaciar entre pruebas (a diferencia de registros_auditoria, que tiene disparador).
    // Sólo esta tabla: un limpiador genérico chocaría con el disparador de la auditoría (story 1.3).
    public static async Task LimpiarBandejaAsync(this PostgresFixture fixture)
    {
        await using var con = new NpgsqlConnection(fixture.CadenaDuenio);
        await con.OpenAsync();
        await using var comando = new NpgsqlCommand("TRUNCATE mensajes_salientes RESTART IDENTITY;", con);
        await comando.ExecuteNonQueryAsync();
    }
}
