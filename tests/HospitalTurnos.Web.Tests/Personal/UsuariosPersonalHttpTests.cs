using System.Net;
using HospitalTurnos.Aplicacion.Personal;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Web.Tests.Catalogo;
using Microsoft.Extensions.DependencyInjection;
using static HospitalTurnos.Web.Tests.Personal.FormularioHttp;

namespace HospitalTurnos.Web.Tests.Personal;

// A4 por HTTP: lo que se conserva es lo que decide el pedido, no la pantalla. Desactivar o restablecer a alguien corta
// su sesión abierta en la próxima revalidación (2.1 AC 25). Story 3.3 (AC 92): la acción ya no viaja en un formulario
// SSR; se hace por el caso de uso, como la hace la pantalla interactiva (UsuariosInteractivaTests). Los permisos de
// A4 están en MedicosHttpTests (T58).
[Collection("Postgres")]
public class UsuariosPersonalHttpTests(PostgresFixture fixture)
{
    private const string ClavePropia = "Clave-Propia-26";

    private static async Task<HttpClient> IngresadoAsync(PersonalWebApplicationFactory app, string usuario, string clave)
    {
        var cliente = Cliente(app);
        Assert.NotEmpty(CookiesPersonal(await IngresarAsync(cliente, usuario, clave)));
        return cliente;
    }

    private static async Task ConGestionAsync(PersonalWebApplicationFactory app, Func<IGestionUsuariosPersonal, Task> accion)
    {
        using var alcance = app.Services.CreateScope();
        await accion(alcance.ServiceProvider.GetRequiredService<IGestionUsuariosPersonal>());
    }

    [Fact]
    [Trait("Story", "2.2")]
    [Trait("AC", "8")]
    [Trait("AC", "26")]
    public async Task Desactivar_SesionAbiertaDelUsuario_SeCortaAlRevalidar()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var propio = await CrearUsuarioAsync(app, [Roles.Recepcionista], ClavePropia);
        var clienteB = await IngresadoAsync(app, propio.UserName!, ClavePropia);
        var actor = await app.ActorDeAdmin1Async();

        await ConGestionAsync(app, g => g.DesactivarAsync(propio.Id, actor, CancellationToken.None));
        app.Reloj.Advance(TimeSpan.FromMinutes(6));   // pasa la revalidación de 5 minutos (2.1 AC 25)
        var pedido = await clienteB.GetAsync("/personal/recepcion/turnos");

        Assert.Equal(HttpStatusCode.Redirect, pedido.StatusCode);
        Assert.Equal("/personal/ingreso?ReturnUrl=%2Fpersonal%2Frecepcion%2Fturnos", Destino(pedido));
    }

    [Fact]
    [Trait("Story", "2.3")]
    [Trait("AC", "3")]
    public async Task Restablecer_SesionAbiertaDelUsuario_SeCortaAlRevalidar()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var propio = await CrearUsuarioAsync(app, [Roles.Recepcionista], ClavePropia);
        var clienteB = await IngresadoAsync(app, propio.UserName!, ClavePropia);
        // Control: otra persona con su sesión abierta, a la que no se le genera nada, sigue adentro.
        var otro = await CrearUsuarioAsync(app, [Roles.Recepcionista], ClavePropia);
        var clienteC = await IngresadoAsync(app, otro.UserName!, ClavePropia);
        var actor = await app.ActorDeAdmin1Async();

        await ConGestionAsync(app, g => g.RestablecerContrasenaAsync(propio.Id, actor, CancellationToken.None));
        app.Reloj.Advance(TimeSpan.FromMinutes(6));   // pasa la revalidación de 5 minutos (2.1 AC 25)
        var pedido = await clienteB.GetAsync("/personal/recepcion/turnos");
        var control = await clienteC.GetAsync("/personal/recepcion/turnos");

        Assert.Equal(HttpStatusCode.Redirect, pedido.StatusCode);
        Assert.Equal("/personal/ingreso?ReturnUrl=%2Fpersonal%2Frecepcion%2Fturnos", Destino(pedido));
        Assert.Equal(HttpStatusCode.OK, control.StatusCode);
    }
}
