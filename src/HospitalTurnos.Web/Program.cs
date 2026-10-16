using HospitalTurnos.Aplicacion.Email;
using HospitalTurnos.Infraestructura;
using HospitalTurnos.Infraestructura.Email;
using HospitalTurnos.Infraestructura.Identidad;
using HospitalTurnos.Infraestructura.Persistencia;
using HospitalTurnos.Web.Components;
using HospitalTurnos.Web.Errores;
using HospitalTurnos.Web.Personal;
using HospitalTurnos.Web.Salud;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
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
// Story 2.1: usuarios y roles del personal (Identity sobre el mismo contexto) y su sesión con cookie.
builder.Services.AddIdentidadPersonal(builder.Configuration);
builder.Services.AddSesionPersonal();
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
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
// La carpeta se lee DIFERIDA, como las cadenas: leída acá no veía la configuración que agregan las
// pruebas y las claves iban a la carpeta por defecto del usuario (lo detectó la story 2.1, T28).
// Es lo mismo que hace PersistKeysToFileSystem, con la carpeta resuelta al crear las opciones.
builder.Services.AddDataProtection().SetApplicationName("HospitalTurnos");
builder.Services.AddOptions<KeyManagementOptions>().Configure<IConfiguration, ILoggerFactory>((o, config, registros) =>
{
    var carpetaClaves = config["ProteccionDeDatos:Carpeta"];
    if (!string.IsNullOrWhiteSpace(carpetaClaves))
        o.XmlRepository = new FileSystemXmlRepository(new DirectoryInfo(carpetaClaves), registros);
});

var app = builder.Build();

// Las dos cadenas se validan con la configuración final (AC 7). Las migraciones las corre el
// dueño del esquema ANTES de atender a nadie.
ServiciosDeInfraestructura.CadenaDeConexion(app.Configuration, "App");
await AplicadorDeMigraciones.AplicarAsync(
    ServiciosDeInfraestructura.CadenaDeConexion(app.Configuration, "Migraciones"));
// Con el esquema al día: los roles y, sólo con la base sin usuarios, el administrador inicial (AC 16-19).
using (var alcance = app.Services.CreateScope())
    await alcance.ServiceProvider.GetRequiredService<SembradorPersonal>().SembrarAsync();

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

// En este orden: quién es (cookie "Personal"), si puede (roles de cada sección) y el token de los formularios.
app.UseAuthentication();
// Story 2.2: con la contraseña temporal, todo /personal/ lleva a la pantalla de cambio (AC 20).
app.UseCambioDeContrasenaObligatorio();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapEndpointsPersonal();
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
