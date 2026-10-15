using HospitalTurnos.Infraestructura.Persistencia;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HospitalTurnos.Web.Salud;

/// <summary>
/// /salud chequea de verdad la base (el proyecto guía devuelve "ok" siempre). Si no hay conexión,
/// Unhealthy SIN la excepción: la respuesta no puede mostrar detalles técnicos (NFR-09). La causa
/// sí queda en el registro del servidor, para poder diagnosticarla.
/// </summary>
public sealed class ChequeoBaseDeDatos(HospitalDbContext contexto, ILogger<ChequeoBaseDeDatos> registro) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await contexto.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Lo canceló quien pidió /salud (o venció el tope de 3 s): no es una falla de la base.
            throw;
        }
        catch (Exception e)
        {
            // Cualquier otra falla (conexión rota del pool, clave rechazada) es "sin base".
            registro.LogWarning(e, "El chequeo de salud no pudo conectarse a la base");
            return HealthCheckResult.Unhealthy();
        }
    }
}
