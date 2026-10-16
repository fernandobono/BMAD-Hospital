using System.Net;
using System.Text.RegularExpressions;
using HospitalTurnos.Dominio.Personal;
using static HospitalTurnos.Web.Tests.Personal.FormularioHttp;

namespace HospitalTurnos.Web.Tests.Personal;

// El primer ingreso con la contraseña temporal (S-30): la sesión queda obligada a cambiarla antes de usar
// el sistema. Cada prueba da de alta su propio usuario por A4, como lo haría Lucía.
[Collection("Postgres")]
public class CambioContrasenaHttpTests(PostgresFixture fixture)
{
    private const string Cambio = "/personal/cambiar-contrasena";
    private const string Turnos = "/personal/recepcion/turnos";
    private const string Nueva = "Nueva-Clave-2026";

    private static async Task<(string Usuario, string Temporal)> AltaPorA4Async(PersonalWebApplicationFactory app)
    {
        var admin = Cliente(app);
        await IngresarAsync(admin, PersonalWebApplicationFactory.Admin.Usuario, PersonalWebApplicationFactory.Admin.Contrasena);
        var usuario = $"p-{Guid.NewGuid().ToString("N")[..8]}";
        var alta = await EnviarAsync(admin, "/personal/admin/usuarios", "alta-usuario", new Dictionary<string, string>
        {
            ["Alta.NombreCompleto"] = "Persona Nueva",
            ["Alta.Usuario"] = usuario,
            ["Alta.Rol"] = Roles.Recepcionista,
        });
        Assert.Equal(HttpStatusCode.OK, alta.StatusCode);
        var html = await alta.Content.ReadAsStringAsync();
        var temporal = WebUtility.HtmlDecode(Regex.Match(html, "<code id=\"contrasena-temporal\">(.*?)</code>").Groups[1].Value);
        Assert.Equal(12, temporal.Length);
        return (usuario, temporal);
    }

    private static Task<HttpResponseMessage> CambiarAsync(HttpClient cliente, string ruta, string actual, string nueva, string repetida) =>
        EnviarAsync(cliente, ruta, "cambio-contrasena", new Dictionary<string, string>
        {
            ["Input.Actual"] = actual,
            ["Input.Nueva"] = nueva,
            ["Input.Repetida"] = repetida,
        });

    [Theory]
    [Trait("Story", "2.2")]
    [Trait("AC", "19")]
    [Trait("AC", "20")]
    [Trait("AC", "21")]
    [InlineData(null)]
    [InlineData("/personal/agendas")]
    public async Task PrimerIngreso_ContrasenaTemporal_ObligaACambiarlaAntesDeEntrar(string? returnUrl)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var (usuario, temporal) = await AltaPorA4Async(app);
        var cliente = Cliente(app);
        var pantallaDeCambio = returnUrl is null ? Cambio : $"{Cambio}?ReturnUrl={Uri.EscapeDataString(returnUrl)}";
        var destino = returnUrl ?? Turnos;

        var ingreso = await IngresarAsync(cliente, usuario, temporal, returnUrl);
        Assert.Equal(HttpStatusCode.Redirect, ingreso.StatusCode);
        Assert.Equal(pantallaDeCambio, Destino(ingreso));
        Assert.NotEmpty(CookiesPersonal(ingreso));

        var seccion = await cliente.GetAsync(Turnos);
        Assert.Equal(HttpStatusCode.Redirect, seccion.StatusCode);
        Assert.Equal(Cambio, Destino(seccion));
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/salud")).StatusCode);   // fuera de /personal/
        // Sin menú (layout de ingreso): la pantalla trae su propio botón Salir.
        Assert.Contains("action=\"/personal/salir\"", await (await cliente.GetAsync(Cambio)).Content.ReadAsStringAsync());

        var cambio = await CambiarAsync(cliente, pantallaDeCambio, temporal, Nueva, Nueva);
        Assert.Equal(HttpStatusCode.Redirect, cambio.StatusCode);
        Assert.Equal(destino, Destino(cambio));
        Assert.NotEmpty(CookiesPersonal(cambio));

        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(destino)).StatusCode);
        var salida = await SalirAsync(cliente, destino);
        Assert.Equal(HttpStatusCode.Redirect, salida.StatusCode);

        var conTemporal = await IngresarAsync(Cliente(app), usuario, temporal);
        Assert.Equal(HttpStatusCode.OK, conTemporal.StatusCode);
        Assert.Equal(MensajeGenerico, Alerta(await conTemporal.Content.ReadAsStringAsync()));
        var conNueva = await IngresarAsync(Cliente(app), usuario, Nueva);
        Assert.Equal(HttpStatusCode.Redirect, conNueva.StatusCode);
        Assert.Equal(Turnos, Destino(conNueva));
    }

    public static TheoryData<string, string, string, string[]> DatosQueNoSirven => new()
    {
        // actual ({t} = la temporal), nueva, repetida, mensajes
        { "{t}", Nueva, "Otra-Clave-2026", ["Las contraseñas nuevas no coinciden."] },
        { "Mala-Clave-99", Nueva, Nueva, ["La contraseña actual no es correcta."] },
        { "{t}", "corta", "corta",
            [
                "La contraseña nueva no sirve:",
                "La contraseña tiene que tener al menos 10 caracteres.",
                "La contraseña tiene que tener al menos una mayúscula.",
                "La contraseña tiene que tener al menos un número.",
                "La contraseña tiene que tener al menos un símbolo, como - o #.",
            ] },
        { "", "", "", ["Ingresá tu contraseña actual.", "Ingresá la contraseña nueva.", "Repetí la contraseña nueva."] },
    };

    [Theory]
    [Trait("Story", "2.2")]
    [Trait("AC", "22")]
    [MemberData(nameof(DatosQueNoSirven))]
    public async Task PostCambiarContrasena_Errores_MuestraMensajesYSigueObligado(string actual, string nueva, string repetida, string[] mensajes)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var (usuario, temporal) = await AltaPorA4Async(app);
        var cliente = Cliente(app);
        await IngresarAsync(cliente, usuario, temporal);

        var respuesta = await CambiarAsync(cliente, Cambio, actual.Replace("{t}", temporal), nueva, repetida);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var alerta = Alerta(await respuesta.Content.ReadAsStringAsync());
        Assert.NotNull(alerta);
        foreach (var mensaje in mensajes)
            Assert.Contains(mensaje, alerta);
        var seccion = await cliente.GetAsync(Turnos);
        Assert.Equal(HttpStatusCode.Redirect, seccion.StatusCode);
        Assert.Equal(Cambio, Destino(seccion));
    }
}
