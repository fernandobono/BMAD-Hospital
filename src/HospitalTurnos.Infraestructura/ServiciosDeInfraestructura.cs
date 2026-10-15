using HospitalTurnos.Infraestructura.Persistencia;
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
