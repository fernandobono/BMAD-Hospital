using HospitalTurnos.Aplicacion.Personal;
using HospitalTurnos.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalTurnos.Infraestructura.Identidad;

public static class RegistroIdentidad
{
    /// <summary>
    /// Identity "núcleo": usuarios, roles, hash y bloqueo, sin sus pantallas ni su cookie. La cookie es la
    /// del esquema "Personal" (contrato) y la emite Web; el paciente tendrá su propio esquema (sprint 5).
    /// Por eso AddIdentityCore y no AddIdentity/AddDefaultIdentity, que registran Identity.Application.
    /// </summary>
    public static IServiceCollection AddIdentidadPersonal(this IServiceCollection servicios, IConfiguration configuracion)
    {
        servicios.AddIdentityCore<UsuarioPersonal>(o =>
            {
                // AC 21: los valores por defecto de Identity, salvo el largo (6 → 10).
                o.Password.RequiredLength = 10;
                o.Password.RequireUppercase = true;
                o.Password.RequireLowercase = true;
                o.Password.RequireDigit = true;
                // FR-09 y pedido del docente: 5 intentos, 15 minutos.
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                o.Lockout.AllowedForNewUsers = true;
                // Se ingresa por nombre de usuario; el email no es obligatorio ni único.
                o.User.RequireUniqueEmail = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<HospitalDbContext>();

        servicios.AddScoped<IIngresoPersonal, IngresoPersonalIdentity>();
        servicios.AddScoped<SembradorPersonal>();
        // Configure con la sección (no BindConfiguration): así funciona también sin IConfiguration en el
        // contenedor (pruebas de Aplicacion) y lee los valores recién al resolverse.
        servicios.Configure<OpcionesAdministradorInicial>(configuracion.GetSection(OpcionesAdministradorInicial.Seccion));
        return servicios;
    }
}
