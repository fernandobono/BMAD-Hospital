using Microsoft.AspNetCore.Authentication.Cookies;

namespace HospitalTurnos.Web.Personal;

/// <summary>
/// La sesión del personal: cookie del esquema "Personal" (contrato), 30 minutos sin uso (NFR-04, S-24)
/// y revalidada contra la base cada 5 minutos (AC 25). La protección de datos (claves persistentes,
/// AC 27) la registra Program.cs desde la story 1.1: acá no se repite.
/// </summary>
public static class SesionPersonal
{
    public const string Esquema = "Personal";
    public const string NombreCookie = ".HospitalTurnos.Personal";

    public static readonly TimeSpan Vencimiento = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan IntervaloRevalidacion = TimeSpan.FromMinutes(5);

    public static IServiceCollection AddSesionPersonal(this IServiceCollection servicios)
    {
        servicios.AddScoped<ValidadorSesionPersonal>();
        servicios.AddAuthentication(o => o.DefaultScheme = Esquema)
            .AddCookie(Esquema, o =>
            {
                o.Cookie.Name = NombreCookie;
                o.Cookie.HttpOnly = true;
                o.Cookie.SameSite = SameSiteMode.Strict;
                o.ExpireTimeSpan = Vencimiento;
                o.SlidingExpiration = true;   // "sin uso", no "desde el ingreso"
                o.LoginPath = RutasPersonal.Ingreso;
                o.AccessDeniedPath = RutasPersonal.AccesoDenegado;
                o.LogoutPath = RutasPersonal.Salir;
                o.EventsType = typeof(ValidadorSesionPersonal);
            });

        // Diferido, con lo que hay en el contenedor al resolver: así ve el entorno y el reloj que fijan
        // las pruebas (WithWebHostBuilder, ConfigureTestServices) sin depender del orden de registro.
        servicios.AddOptions<CookieAuthenticationOptions>(Esquema)
            .Configure<TimeProvider, IWebHostEnvironment>((o, reloj, entorno) =>
            {
                // La prueba de vencimiento (T16) necesita que la cookie lea el mismo reloj que el sistema.
                o.TimeProvider = reloj;
                // NFR-03: detrás de un proxy que termina TLS el pedido llega por HTTP; igual la cookie
                // tiene que viajar sólo cifrada. En Development se deja probar con http://localhost.
                o.Cookie.SecurePolicy = entorno.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
            });
        return servicios;
    }
}
