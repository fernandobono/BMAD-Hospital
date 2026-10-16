using HospitalTurnos.Dominio.Excepciones;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace HospitalTurnos.Web.Tests.Ayudas;

/// <summary>
/// Agrega, SÓLO en las pruebas, dos rutas que lanzan excepciones. Van después de next(app): así
/// quedan dentro del pipeline de la app, debajo de UseExceptionHandler, como cualquier página real.
/// El código de producción no tiene ninguna ruta que falle a propósito.
/// </summary>
public sealed class FiltroRutaQueFalla : IStartupFilter
{
    public const string RutaInesperada = "/prueba/falla";
    public const string RutaRegla = "/prueba/falla-regla";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        next(app);
        app.Map(RutaInesperada, a => a.Run(_ => throw new InvalidOperationException("detalle secreto")));
        app.Map(RutaRegla, a => a.Run(_ => throw new ReglaDeNegocioException("No hay turnos ese día.")));
    };
}
