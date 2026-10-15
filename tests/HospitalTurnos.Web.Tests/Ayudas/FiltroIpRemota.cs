using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace HospitalTurnos.Web.Tests.Ayudas;

/// <summary>
/// El servidor de pruebas no tiene IP remota. Este filtro la fija ANTES del resto del pipeline,
/// para que UseForwardedHeaders decida si la red es confiable como lo haría en producción.
/// </summary>
public sealed class FiltroIpRemota(IPAddress ip) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use((contexto, siguiente) =>
        {
            contexto.Connection.RemoteIpAddress = ip;
            return siguiente(contexto);
        });
        next(app);
    };
}
