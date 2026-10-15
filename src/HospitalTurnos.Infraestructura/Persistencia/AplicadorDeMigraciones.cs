using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HospitalTurnos.Infraestructura.Persistencia;

/// <summary>
/// Aplica las migraciones pendientes con el rol DUEÑO del esquema (turnos_owner). No usa el contexto
/// del contenedor de dependencias porque ése se conecta como turnos_app, que no tiene CREATE.
/// En desarrollo lo llama el arranque de la app; en producción las migraciones son un paso aparte y
/// la app ni siquiera recibe la contraseña del dueño (contrato técnico, I-8).
/// </summary>
public static class AplicadorDeMigraciones
{
    public static async Task AplicarAsync(string cadenaDuenio, CancellationToken ct = default)
    {
        var opciones = new DbContextOptionsBuilder<HospitalDbContext>()
            .UseNpgsql(cadenaDuenio)
            .Options;

        await using var contexto = new HospitalDbContext(opciones);
        try
        {
            // Idempotente: sólo aplica lo que falta en __EFMigrationsHistory.
            await contexto.Database.MigrateAsync(ct);
        }
        catch (NpgsqlException e)
        {
            // Sin base no se atiende a nadie: el arranque se corta con un mensaje que nombra la cadena.
            throw new InvalidOperationException(
                "No se pudieron aplicar las migraciones: la base no responde o rechazó al rol dueño (ConnectionStrings:Migraciones).", e);
        }
    }
}
