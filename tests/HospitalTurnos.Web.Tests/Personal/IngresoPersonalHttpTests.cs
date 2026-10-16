using System.Net;
using HospitalTurnos.Aplicacion.Personal;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Web.Errores;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using static HospitalTurnos.Web.Tests.Personal.FormularioHttp;

namespace HospitalTurnos.Web.Tests.Personal;

// La pantalla S0 por HTTP, sobre la aplicación real y su base: lo mismo que hace un navegador.
[Collection("Postgres")]
public class IngresoPersonalHttpTests(PostgresFixture fixture)
{
    private const string ClavePropia = "Clave-Propia-2026";
    private const string AcceptNavegador = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";

    private async Task<long> FilasDeAuditoriaAsync(Guid id)
    {
        await using var conexion = new NpgsqlConnection(fixture.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand("SELECT count(*) FROM registros_auditoria WHERE entidad_id = @id", conexion);
        comando.Parameters.AddWithValue("id", id.ToString());
        return (long)(await comando.ExecuteScalarAsync())!;
    }

    public static TheoryData<string, string> InicioPorRol => new()
    {
        { "recepcion1", "/personal/recepcion/turnos" },
        { "medico1", "/personal/medico/mi-agenda" },
        { "admin1", "/personal/admin/medicos" },
    };

    [Theory]
    [Trait("Story", "2.1")]
    [Trait("AC", "1")]
    [Trait("AC", "13")]
    [Trait("AC", "26")]
    [MemberData(nameof(InicioPorRol))]
    public async Task PostIngreso_CredencialesCorrectas_EmiteCookieYRedirigeAlInicioDelRol(string usuario, string inicio)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var fijo = PersonalWebApplicationFactory.Fijos.Single(f => f.Usuario == usuario);
        var cliente = Cliente(app);

        var respuesta = await IngresarAsync(cliente, fijo.Usuario, fijo.Contrasena);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(inicio, Destino(respuesta));
        var cookie = Assert.Single(CookiesPersonal(respuesta)).ToLowerInvariant();
        Assert.Contains("httponly", cookie);
        Assert.Contains("samesite=strict", cookie);
        Assert.Contains("secure", cookie);
        Assert.DoesNotContain("expires", cookie);   // cookie de sesión: se va al cerrar el navegador
    }

    public static TheoryData<string> CasosConMensajeGenerico => new() { "contraseña mala", "quinto intento", "ya bloqueado" };

    [Theory]
    [Trait("Story", "2.1")]
    [Trait("AC", "2")]
    [Trait("AC", "3")]
    [MemberData(nameof(CasosConMensajeGenerico))]
    public async Task PostIngreso_CredencialesIncorrectasOBloqueado_MuestraElMismoMensajeSinCookie(string caso)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var usuario = await CrearUsuarioAsync(app, [Roles.Recepcionista], ClavePropia);
        var clave = "Mala-Clave-1";
        if (caso == "quinto intento")
            await FallarAsync(app, usuario.Id, 4);
        if (caso == "ya bloqueado")
        {
            await FallarAsync(app, usuario.Id, 5);
            clave = ClavePropia;
        }

        var respuesta = await IngresarAsync(Cliente(app), usuario.UserName!, clave);

        var html = await respuesta.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(MensajeGenerico, Alerta(html));
        Assert.DoesNotContain("bloque", WebUtility.HtmlDecode(html), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(usuario.UserName, ValorDe(html, "Input.Usuario"));
        Assert.True(string.IsNullOrEmpty(ValorDe(html, "Input.Contrasena")), "la contraseña tiene que volver vacía");
        Assert.Empty(CookiesPersonal(respuesta));
    }

    [Theory]
    [Trait("Story", "2.1")]
    [Trait("AC", "6")]
    [InlineData("vacíos")]
    [InlineData("sin contraseña")]
    [InlineData("usuario largo")]
    public async Task PostIngreso_CamposVaciosOLargos_MuestraMensajePorCampoSinAuditar(string caso)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var propio = await CrearUsuarioAsync(app, [Roles.Recepcionista], ClavePropia);
        var (usuario, clave) = caso switch
        {
            "vacíos" => ("", ""),
            "sin contraseña" => (propio.UserName!, ""),
            _ => (new string('a', 101), "Cualquier-1"),
        };

        var respuesta = await IngresarAsync(Cliente(app), usuario, clave);

        var html = WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Empty(CookiesPersonal(respuesta));
        switch (caso)
        {
            case "vacíos":
                Assert.Contains("Ingresá tu usuario.", html);
                Assert.Contains("Ingresá tu contraseña.", html);
                break;
            case "sin contraseña":
                Assert.Contains("Ingresá tu contraseña.", html);
                Assert.DoesNotContain("Ingresá tu usuario.", html);
                Assert.Equal(0, await FilasDeAuditoriaAsync(propio.Id));
                Assert.Equal(0, (await RecargarAsync(app, propio.Id)).AccessFailedCount);
                break;
            default:
                Assert.Contains("El usuario tiene como máximo 100 caracteres.", html);
                break;
        }
    }

    [Theory]
    [Trait("Story", "2.1")]
    [Trait("AC", "7")]
    [InlineData("/personal/agendas", "/personal/agendas")]
    [InlineData("https://malo.example", "/personal/recepcion/turnos")]
    [InlineData("//malo.example", "/personal/recepcion/turnos")]
    [InlineData("/otra-cosa", "/personal/recepcion/turnos")]
    public async Task PostIngreso_ReturnUrl_SoloRedirigeARutasDelPersonal(string returnUrl, string destino)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var fijo = PersonalWebApplicationFactory.Recepcion;

        var respuesta = await IngresarAsync(Cliente(app), fijo.Usuario, fijo.Contrasena, returnUrl);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(destino, Destino(respuesta));
    }

    [Theory]
    [Trait("Story", "2.1")]
    [Trait("AC", "8")]
    [Trait("AC", "15")]
    [InlineData("ingreso")]
    [InlineData("salir")]
    public async Task Post_SinTokenAntiforgery_Devuelve400(string formulario)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var cliente = Cliente(app);

        if (formulario == "ingreso")
        {
            var propio = await CrearUsuarioAsync(app, [Roles.Recepcionista], ClavePropia);
            var respuesta = await IngresarAsync(cliente, propio.UserName!, "Mala-Clave-1", conToken: false);

            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
            Assert.Equal(0, await FilasDeAuditoriaAsync(propio.Id));
            Assert.Equal(0, (await RecargarAsync(app, propio.Id)).AccessFailedCount);
        }
        else
        {
            var fijo = PersonalWebApplicationFactory.Recepcion;
            await IngresarAsync(cliente, fijo.Usuario, fijo.Contrasena);
            var respuesta = await SalirAsync(cliente, "/personal/recepcion/turnos", conToken: false);

            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/personal/recepcion/turnos")).StatusCode);
        }
    }

    // Doble de la base que no responde: el ingreso lanza lo mismo que lanzaría Npgsql.
    private sealed class IngresoSinBase : IIngresoPersonal
    {
        public Task<ResultadoIngreso> ValidarAsync(string usuario, string contrasena, CancellationToken ct) =>
            throw new NpgsqlException("detalle de Npgsql: la base no responde");

        public Task<bool> SesionVigenteAsync(string usuarioId, string? selloDeSeguridad, CancellationToken ct) =>
            throw new NpgsqlException("detalle de Npgsql: la base no responde");
    }

    [Fact]
    [Trait("Story", "2.1")]
    [Trait("AC", "9")]
    [Trait("AC", "23")]
    public async Task PostIngreso_FallaLaBase_MuestraPaginaDeErrorGenericaSinCookie()
    {
        await using var fabrica = new PersonalWebApplicationFactory(fixture);
        await using var app = fabrica.WithWebHostBuilder(b =>
            b.ConfigureTestServices(s => s.AddScoped<IIngresoPersonal, IngresoSinBase>()));
        var cliente = Cliente(app);
        var html = await (await cliente.GetAsync(RutaIngreso)).Content.ReadAsStringAsync();
        var pedido = new HttpRequestMessage(HttpMethod.Post, RutaIngreso)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["_handler"] = ValorDe(html, "_handler")!,
                ["__RequestVerificationToken"] = ValorDe(html, "__RequestVerificationToken")!,
                ["Input.Usuario"] = "recepcion1",
                ["Input.Contrasena"] = "Recepcion-2026",
            }),
        };
        // Sin este encabezado el manejador de 1.2 contesta problem+json y la prueba miraría otra respuesta.
        pedido.Headers.Accept.ParseAdd(AcceptNavegador);

        var respuesta = await cliente.SendAsync(pedido);

        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        Assert.Contains(ManejadorGlobalDeErrores.MensajeGenerico, WebUtility.HtmlDecode(cuerpo));
        Assert.DoesNotContain("Npgsql", cuerpo);
        Assert.Empty(CookiesPersonal(respuesta));
        Assert.Contains(fabrica.Registro.Entradas, e =>
            e.Categoria == typeof(ManejadorGlobalDeErrores).FullName && e.Excepcion is NpgsqlException);
    }

    [Fact]
    [Trait("Story", "2.1")]
    [Trait("AC", "2")]
    [Trait("AC", "3")]
    public async Task PostIngreso_UsuarioInexistenteSeisVeces_MismaRespuestaQueUnUsuarioBloqueado()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var cliente = Cliente(app);
        var inexistente = $"noexiste-{Guid.NewGuid():N}";
        var respuestas = new List<HttpResponseMessage>();
        for (var i = 0; i < 6; i++)
            respuestas.Add(await IngresarAsync(cliente, inexistente, "Cualquier-Clave-1"));
        var bloqueado = await CrearUsuarioAsync(app, [Roles.Recepcionista], ClavePropia);
        await FallarAsync(app, bloqueado.Id, 5);
        respuestas.Add(await IngresarAsync(cliente, bloqueado.UserName!, ClavePropia));

        foreach (var respuesta in respuestas)
        {
            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.Equal(MensajeGenerico, Alerta(await respuesta.Content.ReadAsStringAsync()));
            Assert.Empty(CookiesPersonal(respuesta));
        }
    }

    [Theory]
    [Trait("Story", "2.1")]
    [Trait("AC", "26")]
    [InlineData("Testing", true)]
    [InlineData("Development", false)]
    public async Task CookiePersonal_SegunEntorno_SecureSoloFueraDeDevelopment(string entorno, bool conSecure)
    {
        await using var fabrica = new PersonalWebApplicationFactory(fixture);
        await using var app = fabrica.WithWebHostBuilder(b => b.UseEnvironment(entorno));
        var fijo = PersonalWebApplicationFactory.Recepcion;

        var respuesta = await IngresarAsync(Cliente(app, "http://localhost"), fijo.Usuario, fijo.Contrasena);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        var cookie = Assert.Single(CookiesPersonal(respuesta)).ToLowerInvariant();
        Assert.Equal(conSecure, cookie.Contains("secure"));
    }
}
