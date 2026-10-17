using HospitalTurnos.Aplicacion.Agendas;
using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Catalogo;
using HospitalTurnos.Infraestructura.Agendas;
using HospitalTurnos.Infraestructura.Auditoria;
using HospitalTurnos.Infraestructura.Catalogo;
using HospitalTurnos.Infraestructura.Persistencia;
using HospitalTurnos.Infraestructura.Tiempo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalTurnos.Infraestructura;

public static class ServiciosDeInfraestructura
{
    /// <summary>
    /// Registra la persistencia con el rol de la aplicación (turnos_app). La cadena se lee DIFERIDA,
    /// dentro del lambda, al resolver el contexto: así ve también la configuración que agregan las
    /// pruebas después de registrar los servicios.
    /// </summary>
    public static IServiceCollection AgregarInfraestructura(this IServiceCollection servicios, IConfiguration configuracion)
    {
        servicios.AddDbContext<HospitalDbContext>((_, opciones) =>
            opciones.UseNpgsql(CadenaDeConexion(configuracion, "App")));
        // Un solo reloj para toda la app (NFR-06): singleton, no tiene estado.
        servicios.AddSingleton<TimeProvider>(new RelojHospital());
        // Scoped como el contexto: el registro viaja en el mismo SaveChangesAsync que el cambio.
        servicios.AddScoped<IAuditoria, AuditoriaEnBase>();
        // Story 3.1: mismo alcance que el contexto y la auditoría (necesita el IActorVigente de la identidad).
        servicios.AddScoped<ICatalogoEspecialidades, CatalogoEspecialidadesEnBase>();
        // Story 3.2: igual que la 3.1; además usa el catálogo para validar la especialidad elegida.
        servicios.AddScoped<IGestionMedicos, GestionMedicosEnBase>();
        // Story 4.1: lectura de agendas; la usa GestionMedicosEnBase para el estado de A1.
        servicios.AddScoped<IConsultaAgendas, ConsultaAgendasEnBase>();
        return servicios;
    }

    /// <summary>
    /// Devuelve la cadena o corta con un mensaje que la nombra. Nunca hay un valor por defecto
    /// escondido: sin cadena la app no arranca (AC 7).
    /// </summary>
    public static string CadenaDeConexion(IConfiguration configuracion, string nombre)
    {
        var cadena = configuracion.GetConnectionString(nombre);
        if (string.IsNullOrWhiteSpace(cadena))
            throw new InvalidOperationException($"Falta la cadena de conexión '{nombre}'");
        return cadena;
    }
}
