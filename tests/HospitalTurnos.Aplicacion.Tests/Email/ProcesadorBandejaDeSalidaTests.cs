using System.Diagnostics;
using HospitalTurnos.Aplicacion.Email;
using HospitalTurnos.Aplicacion.Tests.Ayudas;
using HospitalTurnos.Aplicacion.Tests.Email.Dobles;
using HospitalTurnos.Infraestructura.Email;
using HospitalTurnos.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Npgsql;

namespace HospitalTurnos.Aplicacion.Tests.Email;

// Contra la base real: el bloqueo de filas (FOR UPDATE SKIP LOCKED) y las transacciones viven ahí.
// El reloj es un FakeTimeProvider: los reintentos a 1, 5 y 15 minutos se prueban sin esperar minutos.
[Collection("Postgres")]
public class ProcesadorBandejaDeSalidaTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset Inicio = new(2026, 10, 12, 11, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan TopeReal = TimeSpan.FromSeconds(5);

    private readonly List<ServiceProvider> _proveedores = [];

    public Task InitializeAsync() => fixture.LimpiarBandejaAsync();

    public async Task DisposeAsync()
    {
        foreach (var proveedor in _proveedores) await proveedor.DisposeAsync();
    }

    private static FakeTimeProvider Reloj()
    {
        var reloj = new FakeTimeProvider(Inicio);
        reloj.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Cordoba"));
        return reloj;
    }

    private ProcesadorBandejaDeSalida CrearProcesador(FakeTimeProvider reloj, IEnviadorEmail enviador,
        RegistroEnMemoria<ProcesadorBandejaDeSalida> logger, params IManejadorFalloDefinitivo[] manejadores) =>
        new(Proveedor(enviador, manejadores).GetRequiredService<IServiceScopeFactory>(), reloj, logger);

    private ServiceProvider Proveedor(IEnviadorEmail enviador, IManejadorFalloDefinitivo[] manejadores)
    {
        var servicios = new ServiceCollection();
        servicios.AddDbContext<HospitalDbContext>(o => o.UseNpgsql(fixture.CadenaApp));
        servicios.AddSingleton(enviador);
        foreach (var manejador in manejadores) servicios.AddSingleton(manejador);
        var proveedor = servicios.BuildServiceProvider();
        _proveedores.Add(proveedor);
        return proveedor;
    }

    private async Task EncolarAsync(FakeTimeProvider reloj, string para = "ana@ejemplo.com",
        string tipo = "codigo-ingreso", string? referenciaId = "P-1")
    {
        await using var contexto = ContextoDePrueba.Crear(fixture.CadenaApp);
        new BandejaDeSalidaEnBase(contexto, reloj)
            .Encolar(new MensajeEmail(para, "Asunto", "texto", "<p>html</p>", tipo, referenciaId));
        await contexto.SaveChangesAsync();
    }

    private sealed record Fila(long Id, string Para, int Intentos, DateTimeOffset ProximoIntentoEl,
        DateTimeOffset? EnviadoEl, DateTimeOffset? FalloDefinitivoEl, DateTimeOffset? ManejadoEl, string? UltimoError);

    private async Task<Fila> LeerFilaAsync()
    {
        await using var conexion = new NpgsqlConnection(fixture.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(
            """
            SELECT id, para, intentos, proximo_intento_el, enviado_el, fallo_definitivo_el, manejado_el, ultimo_error
            FROM mensajes_salientes
            """, conexion);
        await using var lector = await comando.ExecuteReaderAsync();
        Assert.True(await lector.ReadAsync(), "no hay ninguna fila");
        var fila = new Fila(
            lector.GetInt64(0), lector.GetString(1), lector.GetInt32(2), lector.GetFieldValue<DateTimeOffset>(3),
            lector.IsDBNull(4) ? null : lector.GetFieldValue<DateTimeOffset>(4),
            lector.IsDBNull(5) ? null : lector.GetFieldValue<DateTimeOffset>(5),
            lector.IsDBNull(6) ? null : lector.GetFieldValue<DateTimeOffset>(6),
            lector.IsDBNull(7) ? null : lector.GetString(7));
        Assert.False(await lector.ReadAsync(), "hay más de una fila");
        return fila;
    }

    // Espera (con tope real) a que se cumpla una condición que depende de otro hilo.
    private static async Task EsperarAsync(Func<bool> condicion, string queSeEspera)
    {
        var reloj = Stopwatch.StartNew();
        while (!condicion())
        {
            if (reloj.Elapsed > TopeReal) Assert.Fail($"pasaron {TopeReal.TotalSeconds} s sin que {queSeEspera}");
            await Task.Delay(10);
        }
    }

    // Lleva el mensaje 1 a fallo definitivo: intento + 3 reintentos, todos fallidos (S-33).
    private static async Task FallarCuatroVecesAsync(ProcesadorBandejaDeSalida procesador, FakeTimeProvider reloj)
    {
        await procesador.ProcesarPendientesAsync(CancellationToken.None);
        foreach (var espera in new[] { 1, 5, 15 })
        {
            reloj.Advance(TimeSpan.FromMinutes(espera));
            await procesador.ProcesarPendientesAsync(CancellationToken.None);
        }
    }

    [Fact]
    [Trait("Story", "1.4")]
    [Trait("AC", "3")]
    public async Task ProcesarPendientes_EnvioExitoso_MarcaEnviadoYNoLoReenvia()
    {
        var reloj = Reloj();
        await EncolarAsync(reloj);
        var enviador = new EnviadorEmailFalso();
        var procesador = CrearProcesador(reloj, enviador, new RegistroEnMemoria<ProcesadorBandejaDeSalida>());

        var procesados = await procesador.ProcesarPendientesAsync(CancellationToken.None);
        var segundoCiclo = await procesador.ProcesarPendientesAsync(CancellationToken.None);

        Assert.Equal(1, procesados);
        Assert.Equal(0, segundoCiclo);
        Assert.Equal(1, enviador.Llamadas);
        var recibido = Assert.Single(enviador.Recibidos);
        Assert.Equal(("ana@ejemplo.com", "Asunto", "texto", "<p>html</p>"),
            (recibido.Para, recibido.Asunto, recibido.CuerpoTexto, recibido.CuerpoHtml));
        var fila = await LeerFilaAsync();
        Assert.Equal(Inicio, fila.EnviadoEl);
        Assert.Equal(1, fila.Intentos);
    }

    [Fact]
    [Trait("Story", "1.4")]
    [Trait("AC", "4")]
    [Trait("AC", "5")]
    public async Task ProcesarPendientes_SmtpCaido_ReprogramaA1_5Y15Minutos()
    {
        var reloj = Reloj();
        await EncolarAsync(reloj);
        var enviador = new EnviadorEmailFalso(EnviadorEmailFalso.Modo.SiempreLanza);
        var logger = new RegistroEnMemoria<ProcesadorBandejaDeSalida>();
        var procesador = CrearProcesador(reloj, enviador, logger);

        // Intento 1 falla → +1 min.
        await procesador.ProcesarPendientesAsync(CancellationToken.None);
        var fila = await LeerFilaAsync();
        Assert.Equal(1, enviador.Llamadas);
        Assert.Equal(1, fila.Intentos);
        Assert.Equal(Inicio.AddMinutes(1), fila.ProximoIntentoEl);
        // El texto completo queda en la base (AC 5), en la misma fila que ya tiene la dirección...
        Assert.Equal("IOException: <ana@ejemplo.com>: Recipient address rejected", fila.UltimoError);
        // ...pero nunca en el registro técnico (datos personales, Ley 25.326; revisión de seguridad).
        var aviso = Assert.Single(logger.Entradas, e => e.Nivel == LogLevel.Warning);
        Assert.Null(aviso.Excepcion);
        Assert.DoesNotContain("ana@ejemplo.com", aviso.Mensaje);
        Assert.Contains("IOException", aviso.Mensaje);

        // A los 59 s no se adelanta el reintento (AC 4).
        reloj.Advance(TimeSpan.FromSeconds(59));
        await procesador.ProcesarPendientesAsync(CancellationToken.None);
        Assert.Equal(1, enviador.Llamadas);

        // Intento 2 falla → +5 min desde ese fallo.
        reloj.Advance(TimeSpan.FromSeconds(1));
        var segundoFallo = reloj.GetUtcNow();
        await procesador.ProcesarPendientesAsync(CancellationToken.None);
        fila = await LeerFilaAsync();
        Assert.Equal(2, enviador.Llamadas);
        Assert.Equal(2, fila.Intentos);
        Assert.Equal(segundoFallo.AddMinutes(5), fila.ProximoIntentoEl);

        // Intento 3 falla → +15 min desde ese fallo.
        reloj.Advance(TimeSpan.FromMinutes(5));
        var tercerFallo = reloj.GetUtcNow();
        await procesador.ProcesarPendientesAsync(CancellationToken.None);
        fila = await LeerFilaAsync();
        Assert.Equal(3, enviador.Llamadas);
        Assert.Equal(3, fila.Intentos);
        Assert.Equal(tercerFallo.AddMinutes(15), fila.ProximoIntentoEl);
        Assert.Null(fila.FalloDefinitivoEl);
    }

    [Fact]
    [Trait("Story", "1.4")]
    [Trait("AC", "6")]
    [Trait("AC", "7")]
    public async Task ProcesarPendientes_FallaElTercerReintento_MarcaFalloDefinitivoEInvocaManejadorDelTipo()
    {
        var reloj = Reloj();
        await EncolarAsync(reloj, tipo: "aviso-bloqueo", referenciaId: "T-77");
        // Un 550 se reintenta igual (Dev Notes: no se distinguen errores permanentes) y termina en fallo definitivo.
        var enviador = new EnviadorEmailFalso(EnviadorEmailFalso.Modo.RechazaDestinatario);
        var manejadorBloqueo = new ManejadorFalloDefinitivoFalso("aviso-bloqueo");
        var manejadorIngreso = new ManejadorFalloDefinitivoFalso("codigo-ingreso");
        var logger = new RegistroEnMemoria<ProcesadorBandejaDeSalida>();
        var procesador = CrearProcesador(reloj, enviador, logger, manejadorBloqueo, manejadorIngreso);

        await FallarCuatroVecesAsync(procesador, reloj);
        var cuartoFallo = reloj.GetUtcNow();
        // Un email perdido deja un Error que una alarma puede ver (revisión: antes no quedaba ninguna línea).
        var perdido = Assert.Single(logger.Entradas, e => e.Nivel == LogLevel.Error);
        Assert.Contains("se dio por perdido tras 4 intentos: SmtpCommandException (SMTP 550)", perdido.Mensaje);
        Assert.DoesNotContain("ana@ejemplo.com", perdido.Mensaje);
        Assert.Null(perdido.Excepcion);   // el mensaje de la excepción puede traer la dirección

        var fila = await LeerFilaAsync();
        Assert.Equal(4, enviador.Llamadas);
        Assert.Equal(4, fila.Intentos);
        Assert.Equal(cuartoFallo, fila.FalloDefinitivoEl);
        Assert.Equal(cuartoFallo, fila.ManejadoEl);   // en el mismo ciclo
        var aviso = Assert.Single(manejadorBloqueo.Recibidos);
        Assert.Equal(new FalloDefinitivoEmail(fila.Id, "aviso-bloqueo", "T-77", "ana@ejemplo.com", fila.UltimoError!), aviso);
        Assert.Equal(0, manejadorIngreso.Invocaciones);

        // Una hora después nadie lo vuelve a intentar ni a manejar.
        reloj.Advance(TimeSpan.FromHours(1));
        await procesador.ProcesarPendientesAsync(CancellationToken.None);
        Assert.Equal(4, enviador.Llamadas);
        Assert.Equal(1, manejadorBloqueo.Invocaciones);
    }

    [Theory]
    [Trait("Story", "1.4")]
    [Trait("AC", "8")]
    [Trait("AC", "10")]
    [InlineData("")]
    [InlineData("paciente-sin-arroba")]
    [InlineData("ana@")]
    public async Task ProcesarPendientes_DestinatarioInvalido_FalloDefinitivoSinIntentarEnviar(string para)
    {
        var reloj = Reloj();
        var excepcion = await Record.ExceptionAsync(() => EncolarAsync(reloj, para: para, tipo: "tipo-sin-manejador"));
        Assert.Null(excepcion);
        var enviador = new EnviadorEmailFalso();
        var logger = new RegistroEnMemoria<ProcesadorBandejaDeSalida>();
        var procesador = CrearProcesador(reloj, enviador, logger);

        await procesador.ProcesarPendientesAsync(CancellationToken.None);

        Assert.Equal(0, enviador.Llamadas);
        var fila = await LeerFilaAsync();
        Assert.Equal(Inicio, fila.FalloDefinitivoEl);
        Assert.Equal(0, fila.Intentos);
        Assert.Equal("Destinatario inválido", fila.UltimoError);
        Assert.Equal(Inicio, fila.ManejadoEl);
        var aviso = Assert.Single(logger.Entradas, e => e.Nivel == LogLevel.Warning);
        Assert.Contains("sin manejador para el tipo tipo-sin-manejador", aviso.Mensaje);
        var perdido = Assert.Single(logger.Entradas, e => e.Nivel == LogLevel.Error);
        Assert.Contains("se dio por perdido: destinatario inválido", perdido.Mensaje);
    }

    [Fact]
    [Trait("Story", "1.4")]
    [Trait("AC", "9")]
    public async Task ProcesarPendientes_ManejadorFalla_ReintentaManejadorSinReenviarEmail()
    {
        var reloj = Reloj();
        await EncolarAsync(reloj, tipo: "aviso-bloqueo", referenciaId: "T-77");
        var enviador = new EnviadorEmailFalso(EnviadorEmailFalso.Modo.SiempreLanza);
        var manejador = new ManejadorFalloDefinitivoFalso("aviso-bloqueo", fallaLaPrimeraVez: true);
        var logger = new RegistroEnMemoria<ProcesadorBandejaDeSalida>();
        var procesador = CrearProcesador(reloj, enviador, logger, manejador);

        // El 4.º fallo y el primer intento del manejador (que lanza) ocurren en el mismo ciclo.
        await FallarCuatroVecesAsync(procesador, reloj);

        var fila = await LeerFilaAsync();
        Assert.NotNull(fila.FalloDefinitivoEl);
        Assert.Null(fila.ManejadoEl);
        // Dos Error en ese ciclo: el email perdido y el manejador que falló. Interesa el del manejador.
        var error = Assert.Single(logger.Entradas, e => e.Nivel == LogLevel.Error && e.Mensaje.Contains("manejador"));
        Assert.Contains($"manejador del mensaje {fila.Id}", error.Mensaje);
        Assert.Contains("InvalidOperationException", error.Mensaje);
        // El manejador recibe Para: el texto de su excepción podría traerlo (misma regla que el envío).
        Assert.Null(error.Excepcion);
        Assert.Equal(4, enviador.Llamadas);

        reloj.Advance(TimeSpan.FromSeconds(15));
        await procesador.ProcesarPendientesAsync(CancellationToken.None);

        fila = await LeerFilaAsync();
        Assert.Equal(reloj.GetUtcNow(), fila.ManejadoEl);
        Assert.Single(manejador.Recibidos);
        Assert.Equal(4, enviador.Llamadas);   // el email nunca se reenvió
    }

    [Fact]
    [Trait("Story", "1.4")]
    [Trait("AC", "11")]
    public async Task ProcesarPendientes_DosInstanciasALaVez_EnviaUnaSolaVez()
    {
        var reloj = Reloj();
        await EncolarAsync(reloj);
        var enviadorA = new EnviadorEmailFalso(EnviadorEmailFalso.Modo.Compuerta);
        var enviadorB = new EnviadorEmailFalso();
        var procA = CrearProcesador(reloj, enviadorA, new RegistroEnMemoria<ProcesadorBandejaDeSalida>());
        var procB = CrearProcesador(reloj, enviadorB, new RegistroEnMemoria<ProcesadorBandejaDeSalida>());

        var tareaA = procA.ProcesarPendientesAsync(CancellationToken.None);
        await EsperarAsync(() => enviadorA.Llamadas == 1, "la instancia A empiece a enviar");
        var nB = await procB.ProcesarPendientesAsync(CancellationToken.None);
        enviadorA.Liberar();
        var nA = await tareaA;

        Assert.Equal(0, nB);
        Assert.Equal(1, nA);
        Assert.Equal(1, enviadorA.Llamadas + enviadorB.Llamadas);
        Assert.NotNull((await LeerFilaAsync()).EnviadoEl);
    }

    [Fact]
    [Trait("Story", "1.4")]
    [Trait("AC", "12")]
    public async Task StopAsync_EnvioEnCurso_TerminaOrdenadoYElMensajeQuedaPendiente()
    {
        var reloj = Reloj();
        await EncolarAsync(reloj);
        var enviador = new EnviadorEmailFalso(EnviadorEmailFalso.Modo.EsperaCancelacion);
        var logger = new RegistroEnMemoria<ProcesadorBandejaDeSalida>();
        var procesador = CrearProcesador(reloj, enviador, logger);

        await procesador.StartAsync(CancellationToken.None);
        await EsperarAsync(() => enviador.Llamadas == 1, "empiece el envío");
        var cronometro = Stopwatch.StartNew();
        await procesador.StopAsync(CancellationToken.None);
        cronometro.Stop();

        Assert.True(cronometro.Elapsed < TopeReal, $"StopAsync tardó {cronometro.Elapsed}");
        Assert.DoesNotContain(logger.Entradas, e => e.Nivel == LogLevel.Error);
        var fila = await LeerFilaAsync();
        Assert.Equal(0, fila.Intentos);
        Assert.Equal(Inicio, fila.ProximoIntentoEl);
        Assert.Null(fila.EnviadoEl);
    }

    [Fact]
    [Trait("Story", "1.4")]
    [Trait("AC", "13")]
    public async Task ExecuteAsync_FallaUnCiclo_RegistraErrorYSigueSondeando()
    {
        var reloj = Reloj();
        await EncolarAsync(reloj);
        var enviador = new EnviadorEmailFalso();
        var logger = new RegistroEnMemoria<ProcesadorBandejaDeSalida>();
        var fabrica = new FabricaQueFallaLaPrimeraVez(
            Proveedor(enviador, []).GetRequiredService<IServiceScopeFactory>());
        var procesador = new ProcesadorBandejaDeSalida(fabrica, reloj, logger);

        await procesador.StartAsync(CancellationToken.None);
        // Advance sólo despierta un Task.Delay ya registrado: se avanza en bucle hasta que haya envío.
        var cronometro = Stopwatch.StartNew();
        while (enviador.Llamadas == 0)
        {
            if (cronometro.Elapsed > TopeReal) Assert.Fail("el ciclo siguiente nunca envió el mensaje");
            reloj.Advance(ProcesadorBandejaDeSalida.IntervaloDeSondeo);
            await Task.Delay(10);
        }

        Assert.Single(logger.Entradas, e => e.Nivel == LogLevel.Error);
        Assert.False(procesador.ExecuteTask!.IsCompleted, "el proceso de fondo terminó");
        await procesador.StopAsync(CancellationToken.None);
    }

    // Simula una causa ajena a los mensajes (la base no responde) sólo en el primer ciclo.
    private sealed class FabricaQueFallaLaPrimeraVez(IServiceScopeFactory real) : IServiceScopeFactory
    {
        private int _veces;

        public IServiceScope CreateScope() =>
            Interlocked.Increment(ref _veces) == 1
                ? throw new NpgsqlException("La base no responde")
                : real.CreateScope();
    }
}
