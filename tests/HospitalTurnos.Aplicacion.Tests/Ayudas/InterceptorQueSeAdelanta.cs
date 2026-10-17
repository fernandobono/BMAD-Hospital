using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace HospitalTurnos.Aplicacion.Tests.Ayudas;

/// <summary>
/// Doble de prueba para la carrera contra un índice único (story 3.1, T8; la 3.2 lo reusa con su propio
/// INSERT). Justo antes del primer guardado, y una sola vez, otra conexión ejecuta el SQL que se le dio:
/// la verificación previa del caso de uso ya no ve nada y el índice sí. Así se prueba la traducción del
/// 23505 sin depender de la suerte.
/// </summary>
public sealed class InterceptorQueSeAdelanta(string cadena, string sqlQueInserta) : SaveChangesInterceptor
{
    private int disparado;

    public bool SeAdelanto => disparado == 1;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref disparado, 1) == 0)
        {
            await using var conexion = new NpgsqlConnection(cadena);
            await conexion.OpenAsync(cancellationToken);
            await using var comando = new NpgsqlCommand(sqlQueInserta, conexion);
            await comando.ExecuteNonQueryAsync(cancellationToken);
        }
        return result;
    }
}
