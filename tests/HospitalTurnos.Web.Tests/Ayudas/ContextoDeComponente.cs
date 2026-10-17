using System.Security.Claims;
using Bunit;
using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Personal;
using HospitalTurnos.Web.Personal;
using HospitalTurnos.Web.Tests.Catalogo;
using HospitalTurnos.Web.Tests.Personal;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalTurnos.Web.Tests.Ayudas;

/// <summary>
/// Story 3.3: las pantallas interactivas se prueban como componentes (bUnit) contra los casos de uso REALES de
/// la app, con su PostgreSQL de Testcontainers. Los servicios con alcance salen de un alcance propio de la app.
/// </summary>
public static class ContextoDeComponente
{
    /// <summary>Los servicios de la app detrás de los de bUnit, y el reloj de la app como TimeProvider.</summary>
    public static IServiceScope ConLaApp(this BunitContext contexto, PersonalWebApplicationFactory app)
    {
        // Contra la base real la primera consulta de cada corrida tarda más que el segundo de espera de bUnit.
        BunitContext.DefaultWaitTimeout = TimeSpan.FromSeconds(15);
        var alcance = app.Services.CreateScope();
        contexto.Services.AddSingleton<TimeProvider>(app.Reloj);
        // Las páginas piden cada caso de uso a un alcance propio de la app, como en producción (AC 94).
        contexto.Services.AddSingleton(new AlcancePorOperacion(app.Services.GetRequiredService<IServiceScopeFactory>()));
        contexto.Services.AddScoped<HospitalTurnos.Web.Components.Comunes.MenuDeAcciones.UnoAbierto>();
        contexto.Services.AddFallbackServiceProvider(alcance.ServiceProvider);
        contexto.JSInterop.Mode = JSRuntimeMode.Loose;
        return alcance;
    }

    /// <summary>El AuthenticationState en cascada con los mismos claims que la cookie del personal.</summary>
    public static ActorAuditoria Como(this BunitContext contexto, ActorAuditoria actor)
    {
        var autorizacion = contexto.AddAuthorization();
        autorizacion.SetAuthorized(actor.Quien);
        autorizacion.SetRoles(actor.Rol);
        autorizacion.SetClaims(
            new Claim(ClaimTypes.NameIdentifier, actor.UsuarioId!),
            new Claim(ClaimsPersonal.NombreCompleto, actor.Quien));
        return actor;
    }

    public static async Task<ActorAuditoria> ComoAdmin1Async(this BunitContext contexto, PersonalWebApplicationFactory app) =>
        contexto.Como(await app.ActorDeAdmin1Async());

    // Con la base real, las consultas terminan en segundo plano y redibujan la página. Buscar un elemento y dispararle
    // un evento por separado puede caer en el medio de un redibujo (el elemento ya es otro). Estas ayudas esperan a
    // que exista y disparan el evento en el mismo turno de la página en que lo encuentran.
    private static async Task DispararAsync<T>(IRenderedComponent<T> pagina, string selector,
        Func<AngleSharp.Dom.IElement, bool>? cual, Func<AngleSharp.Dom.IElement, Task> evento) where T : IComponent
    {
        AngleSharp.Dom.IElement? Buscar() => pagina.FindAll(selector).Where(cual ?? (_ => true)).SingleOrDefault();
        await pagina.WaitForAssertionAsync(() => Assert.NotNull(Buscar()));
        await pagina.InvokeAsync(() => evento(Buscar()!));
    }

    public static Task PulsarAsync<T>(this IRenderedComponent<T> pagina, string selector,
        Func<AngleSharp.Dom.IElement, bool>? cual = null) where T : IComponent =>
        DispararAsync(pagina, selector, cual, e => e.TriggerEventAsync("onclick", new MouseEventArgs()));

    public static Task CambiarAsync<T>(this IRenderedComponent<T> pagina, string selector, string? valor) where T : IComponent =>
        DispararAsync(pagina, selector, null, e => e.TriggerEventAsync("onchange", new ChangeEventArgs { Value = valor }));

    public static Task EnviarAsync<T>(this IRenderedComponent<T> pagina, string selector) where T : IComponent =>
        DispararAsync(pagina, selector, null, e => e.TriggerEventAsync("onsubmit", EventArgs.Empty));
}
