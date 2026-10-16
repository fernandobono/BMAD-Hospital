using HospitalTurnos.Aplicacion.Email;
using HospitalTurnos.Infraestructura.Persistencia;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace HospitalTurnos.Infraestructura.Email;

/// <summary>
/// El proceso de fondo de la bandeja de salida (ADR-05): cada 15 s envía los pendientes, reintenta a 1, 5
/// y 15 min y avisa al manejador del tipo cuando un email se da por perdido. Es singleton (hosted service):
/// el contexto se pide a un scope nuevo por ciclo, nunca por constructor.
/// </summary>
public sealed class ProcesadorBandejaDeSalida(
    IServiceScopeFactory scopes, TimeProvider reloj, ILogger<ProcesadorBandejaDeSalida> logger) : BackgroundService
{
    public static readonly TimeSpan IntervaloDeSondeo = TimeSpan.FromSeconds(15);
    private const int LoteMaximo = 50;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcesarPendientesAsync(stoppingToken); }
            // Por qué atrapar todo: desde .NET 8, una excepción que escapa de ExecuteAsync DETIENE la aplicación entera.
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Falló un ciclo de la bandeja de salida; se reintenta en {Intervalo}", IntervaloDeSondeo);
            }
            // El Delay con TimeProvider es lo que permite a FakeTimeProvider controlar el sondeo.
            await Task.Delay(IntervaloDeSondeo, reloj, stoppingToken);
        }
    }

    /// <summary>Un ciclo: envía hasta 50 pendientes y después corre los manejadores de fallo definitivo.
    /// Devuelve cuántos mensajes tomó para enviar. Público para las pruebas.</summary>
    public async Task<int> ProcesarPendientesAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var enviador = scope.ServiceProvider.GetRequiredService<IEnviadorEmail>();
        var procesados = 0;

        // Fase 1: enviar. Una transacción por mensaje: si el COMMIT falla, se reenvía uno, no cincuenta.
        for (var i = 0; i < LoteMaximo; i++)
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var ahora = reloj.GetUtcNow();
            // La hora es un parámetro de la app, nunca now() de Postgres: así el reloj de las pruebas manda.
            // ToListAsync directo: si se compone (FirstOrDefault), EF lo envuelve en una subconsulta.
            var mensaje = (await db.MensajesSalientes.FromSql($"""
                SELECT * FROM mensajes_salientes
                WHERE enviado_el IS NULL AND fallo_definitivo_el IS NULL AND proximo_intento_el <= {ahora}
                ORDER BY proximo_intento_el, id
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """).ToListAsync(ct)).SingleOrDefault();
            if (mensaje is null) break;

            // El log se escribe DESPUÉS del commit: si el apagado cae en el medio, la transacción se revierte y no
            // queda un log de algo que no pasó.
            Action? registrar = null;
            var (id, tipo) = (mensaje.Id, mensaje.Tipo);
            if (!EsDestinatarioValido(mensaje.Para))
            {
                mensaje.MarcarFalloDefinitivo(ahora, "Destinatario inválido");
                registrar = () => logger.LogError("El mensaje {MensajeId} ({Tipo}) se dio por perdido: destinatario inválido",
                    id, tipo);
            }
            else
            {
                try
                {
                    await enviador.EnviarAsync(mensaje.AMensajeEmail(), ct);
                    mensaje.MarcarEnviado(reloj.GetUtcNow());
                }
                // Si el host se está apagando, la excepción sigue: la transacción se revierte y no cuenta como intento.
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    // El texto completo va a la base (AC 5), en la fila que ya tiene la dirección.
                    mensaje.RegistrarFallo(reloj.GetUtcNow(), $"{ex.GetType().Name}: {ex.Message}");
                    // Al log NO: el texto de un rechazo SMTP suele repetir la dirección del paciente (Ley 25.326).
                    // Sólo el tipo de error y, si el servidor respondió, su código.
                    var (intentos, error) = (mensaje.Intentos, DescribirError(ex));
                    registrar = mensaje.FalloDefinitivoEl is null
                        ? () => logger.LogWarning("Falló el envío del mensaje {MensajeId} ({Tipo}), intento {Intentos}: {Error}",
                            id, tipo, intentos, error)
                        : () => logger.LogError("El mensaje {MensajeId} ({Tipo}) se dio por perdido tras {Intentos} intentos: {Error}",
                            id, tipo, intentos, error);
                }
            }
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            db.ChangeTracker.Clear();
            registrar?.Invoke();
            procesados++;
        }

        await ManejarFallosDefinitivosAsync(scope.ServiceProvider, db, ct);
        return procesados;
    }

    // Fase 2: en su propia transacción por mensaje. Si corriera junto con el fallo y el manejador lanzara,
    // el rollback volvería el mensaje a pendiente y se reenviaría. Los que fallan en este ciclo se excluyen
    // para no volver a tomarlos en el mismo bucle (si no, un manejador roto lo cuelga).
    private async Task ManejarFallosDefinitivosAsync(IServiceProvider servicios, HospitalDbContext db, CancellationToken ct)
    {
        var fallidosEsteCiclo = new List<long>();
        for (var i = 0; i < LoteMaximo; i++)
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var ahora = reloj.GetUtcNow();
            var excluidos = fallidosEsteCiclo.ToArray();
            var mensaje = (await db.MensajesSalientes.FromSql($"""
                SELECT * FROM mensajes_salientes
                WHERE fallo_definitivo_el IS NOT NULL AND manejado_el IS NULL AND NOT (id = ANY({excluidos}))
                ORDER BY id
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """).ToListAsync(ct)).SingleOrDefault();
            if (mensaje is null) break;

            // Del mismo scope que db: un manejador con repositorios scoped escribe en este contexto y se guarda
            // en la misma transacción que manejado_el.
            var manejadores = servicios.GetServices<IManejadorFalloDefinitivo>()
                .Where(m => m.Tipo == mensaje.Tipo).ToList();
            var (id, tipo) = (mensaje.Id, mensaje.Tipo);
            try
            {
                var fallo = new FalloDefinitivoEmail(mensaje.Id, mensaje.Tipo, mensaje.ReferenciaId, mensaje.Para,
                    mensaje.UltimoError ?? "");
                foreach (var manejador in manejadores)
                    await manejador.ManejarAsync(fallo, ct);
                mensaje.MarcarManejado(ahora);
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                // Después del commit, como en la fase 1: un apagado en el medio no deja un aviso de algo revertido.
                if (manejadores.Count == 0)
                    logger.LogWarning("Mensaje {MensajeId}: sin manejador para el tipo {Tipo}", id, tipo);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                // El log va ANTES del rollback: si la conexión está rota y el rollback lanza, el id y el tipo ya quedaron.
                // Sin la excepción: el manejador recibe Para y el texto de su error podría traerlo (Ley 25.326).
                logger.LogError("Falló un manejador del mensaje {MensajeId} ({Tipo}): {Error}; se reintenta en el próximo ciclo",
                    id, tipo, DescribirError(ex));
                await tx.RollbackAsync(CancellationToken.None);
                fallidosEsteCiclo.Add(id);
            }
            finally
            {
                // Descarta lo que el manejador haya agregado si se revirtió, y no arrastra entidades al siguiente.
                db.ChangeTracker.Clear();
            }
        }
    }

    // Sin ex.Message: puede traer la dirección. El código SMTP (p. ej. 550) alcanza para diagnosticar.
    private static string DescribirError(Exception ex) => ex is SmtpCommandException smtp
        ? $"{ex.GetType().Name} (SMTP {(int)smtp.StatusCode})"
        : ex.GetType().Name;

    // MimeKit acepta "paciente-sin-arroba" como dirección local: por eso se exige "@" con algo a cada lado.
    private static bool EsDestinatarioValido(string para) =>
        MailboxAddress.TryParse(para, out var dir)
        && dir.Address.IndexOf('@') is > 0 and var arroba
        && arroba < dir.Address.Length - 1;
}
