using HospitalTurnos.Aplicacion.Email;
using HospitalTurnos.Infraestructura;
using HospitalTurnos.Infraestructura.Email;
using HospitalTurnos.Infraestructura.Persistencia;
using HospitalTurnos.Web.Components;
using HospitalTurnos.Web.Errores;
using HospitalTurnos.Web.Salud;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Composición de dependencias: Web es el único proyecto que conoce a todas las capas.
// Infraestructura registra el DbContext con el rol de la app (turnos_app). La cadena se lee DIFERIDA
// (dentro del lambda de AddDbContext), así ve también la configuración que agregan las pruebas.
builder.Services.AgregarInfraestructura(builder.Configuration);
// Bandeja de salida (ADR-05): los casos de uso encolan; el proceso de fondo envía y reintenta.
// ValidateOnStart: sin Email:Smtp:Host o sin remitente, el arranque se corta (AC 16), no el primer email.
builder.Services.AddOptions<OpcionesSmtp>().BindConfiguration("Email:Smtp").ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddScoped<IBandejaDeSalida, BandejaDeSalidaEnBase>();
builder.Services.AddTransient<IEnviadorEmail, EnviadorEmailSmtp>();
builder.Services.AddHostedService<ProcesadorBandejaDeSalida>();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
// NFR-09: un solo lugar decide qué ve el usuario ante un error y qué queda en el registro técnico.
builder.Services.AddExceptionHandler<ManejadorGlobalDeErrores>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks()
    .AddCheck<ChequeoBaseDeDatos>("base", timeout: TimeSpan.FromSeconds(3));

// NFR-03: el HTTPS lo termina el proxy. Sólo se le cree a X-Forwarded-* si viene de una red
// conocida: loopback (por defecto) + ProxyInverso:RedesConfiables. Nunca "confiar en todos".
builder.Services.AddOptions<ForwardedHeadersOptions>().Configure<IConfiguration>((o, config) =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    foreach (var red in config.GetSection("ProxyInverso:RedesConfiables").Get<string[]>() ?? [])
        o.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(red));
});

// M-11: claves de protección de datos en una carpeta persistente (volumen en Compose), para que
// recrear el contenedor no invalide sesiones ni formularios abiertos.
var carpetaClaves = builder.Configuration["ProteccionDeDatos:Carpeta"];
var proteccion = builder.Services.AddDataProtection().SetApplicationName("HospitalTurnos");
if (!string.IsNullOrWhiteSpace(carpetaClaves))
    proteccion.PersistKeysToFileSystem(new DirectoryInfo(carpetaClaves));

var app = builder.Build();

// Las dos cadenas se validan con la configuración final (AC 7). Las migraciones las corre el
// dueño del esquema ANTES de atender a nadie.
ServiciosDeInfraestructura.CadenaDeConexion(app.Configuration, "App");
await AplicadorDeMigraciones.AplicarAsync(
    ServiciosDeInfraestructura.CadenaDeConexion(app.Configuration, "Migraciones"));

app.UseForwardedHeaders();          // primero: el resto del pipeline ve el esquema real (https)
// En TODOS los entornos (AC 12): la página de excepciones para desarrolladores queda por fuera y
// nunca ve la excepción, porque este middleware ya la manejó. El detalle va a la consola.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    ExceptionHandlingPath = "/Error",
    CreateScopeForErrors = true,
    // Por qué suprimir siempre: ManejadorGlobalDeErrores ya registró cada excepción una vez y con el
    // nivel correcto. Sin esto, en el camino HTML (el manejador devuelve false) el middleware la
    // vuelve a registrar como Error al re-ejecutar /Error (AC 14).
    SuppressDiagnosticsCallback = _ => true
});
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapHealthChecks("/salud", new HealthCheckOptions { ResponseWriter = EscribirSalud });

app.Run();

// Sólo el estado, nunca report.Entries[..].Exception: la respuesta no muestra detalles técnicos.
static Task EscribirSalud(HttpContext contexto, HealthReport reporte)
{
    // Texto fijo y no WriteAsJsonAsync: el serializador escaparía la "ó" como ó.
    contexto.Response.ContentType = "application/json; charset=utf-8";
    return contexto.Response.WriteAsync(reporte.Status == HealthStatus.Healthy
        ? """{"estado":"ok"}"""
        : """{"estado":"sin conexión con la base"}""");
}

// Hace falta para que las pruebas de integración usen WebApplicationFactory<Program>.
public partial class Program;
