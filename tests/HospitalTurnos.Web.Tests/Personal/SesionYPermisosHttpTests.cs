using System.Net;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Infraestructura.Identidad;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static HospitalTurnos.Web.Tests.Personal.FormularioHttp;

namespace HospitalTurnos.Web.Tests.Personal;

// Vencimiento y revalidación de la sesión, permisos por rol y salida, con el reloj de prueba.
[Collection("Postgres")]
public class SesionYPermisosHttpTests(PostgresFixture fixture)
{
    private const string Turnos = "/personal/recepcion/turnos";
    private const string IngresoConVueltaATurnos = "/personal/ingreso?ReturnUrl=%2Fpersonal%2Frecepcion%2Fturnos";
    private const string TextoEnConstruccion = "Esta sección se habilita en una próxima versión.";

    // Las secciones que ya dejaron de estar "en construcción" (las suma la story que las construye).
    private static readonly string[] RutasYaConstruidas =
        ["/personal/admin/usuarios", "/personal/admin/especialidades", "/personal/admin/medicos"];

    // La tabla del AC 13, escrita acá a mano (no se lee de RutasPersonal: la prueba no copia al código).
    // ruta, título, roles con permiso
    private static readonly (string Ruta, string Titulo, string[] Roles)[] Secciones =
    [
        ("/personal/recepcion/turnos", "Turnos", [Roles.Recepcionista]),
        ("/personal/agendas", "Agendas", [Roles.Recepcionista, Roles.Administrador]),
        ("/personal/para-avisar", "Pacientes para avisar", [Roles.Recepcionista, Roles.Administrador]),
        ("/personal/medico/mi-agenda", "Mi agenda", [Roles.Medico]),
        ("/personal/medico/mi-propuesta", "Mi propuesta de agenda", [Roles.Medico]),
        ("/personal/admin/medicos", "Médicos", [Roles.Administrador]),
        ("/personal/admin/propuestas", "Propuestas de agenda", [Roles.Administrador]),
        ("/personal/admin/especialidades", "Especialidades", [Roles.Administrador]),
        ("/personal/admin/bloqueos", "Bloqueos de agenda", [Roles.Administrador]),
        ("/personal/admin/usuarios", "Usuarios del personal", [Roles.Administrador]),
        ("/personal/admin/auditoria", "Auditoría", [Roles.Administrador]),
    ];

    private static readonly Dictionary<string, string> InicioDe = new()
    {
        [Roles.Recepcionista] = "/personal/recepcion/turnos",
        [Roles.Medico] = "/personal/medico/mi-agenda",
        [Roles.Administrador] = "/personal/admin/medicos",
    };

    private static bool VenceLaCookie(HttpResponseMessage respuesta) =>
        CookiesPersonal(respuesta).Any(c => c.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase));

    [Fact]
    [Trait("Story", "2.1")]
    [Trait("AC", "10")]
    public async Task PaginaProtegida_TreintaYUnMinutosSinUso_RedirigeAlIngresoYVuelve()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var cliente = Cliente(app);
        var fijo = PersonalWebApplicationFactory.Recepcion;
        await IngresarAsync(cliente, fijo.Usuario, fijo.Contrasena);

        app.Reloj.Advance(TimeSpan.FromMinutes(31));
        var vencida = await cliente.GetAsync(Turnos);
        var vuelta = await IngresarAsync(cliente, fijo.Usuario, fijo.Contrasena, returnUrl: Turnos);

        Assert.Equal(HttpStatusCode.Redirect, vencida.StatusCode);
        Assert.Equal(IngresoConVueltaATurnos, Destino(vencida));
        Assert.Equal(HttpStatusCode.Redirect, vuelta.StatusCode);
        Assert.Equal(Turnos, Destino(vuelta));
    }

    [Fact]
    [Trait("Story", "2.1")]
    [Trait("AC", "11")]
    [Trait("AC", "25")]
    public async Task PaginaProtegida_PedidosCadaVeinteMinutos_MantieneLaSesion()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var cliente = Cliente(app);
        var fijo = PersonalWebApplicationFactory.Recepcion;
        await IngresarAsync(cliente, fijo.Usuario, fijo.Contrasena);

        app.Reloj.Advance(TimeSpan.FromMinutes(20));
        var primero = await cliente.GetAsync(Turnos);
        app.Reloj.Advance(TimeSpan.FromMinutes(20));
        var segundo = await cliente.GetAsync(Turnos);

        Assert.Equal(HttpStatusCode.OK, primero.StatusCode);
        Assert.Equal(HttpStatusCode.OK, segundo.StatusCode);
    }

    public static TheoryData<string> RutasProtegidas => new(Secciones.Select(s => s.Ruta));

    [Theory]
    [Trait("Story", "2.1")]
    [Trait("AC", "12")]
    [MemberData(nameof(RutasProtegidas))]
    public async Task PaginaProtegida_SinSesion_RedirigeAlIngresoConReturnUrl(string ruta)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);

        var respuesta = await Cliente(app).GetAsync(ruta);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal($"/personal/ingreso?ReturnUrl={Uri.EscapeDataString(ruta)}", Destino(respuesta));
    }

    public static TheoryData<string, string> RolPorRuta
    {
        get
        {
            var casos = new TheoryData<string, string>();
            foreach (var fijo in PersonalWebApplicationFactory.Fijos)
                foreach (var seccion in Secciones)
                    casos.Add(fijo.Usuario, seccion.Ruta);
            return casos;   // 3 roles × 11 rutas = 33
        }
    }

    [Theory]
    [Trait("Story", "2.1")]
    [Trait("AC", "13")]
    [MemberData(nameof(RolPorRuta))]
    public async Task Secciones_SegunRol_PermiteLasSuyasYDevuelve403LegibleEnLasAjenas(string usuario, string ruta)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var fijo = PersonalWebApplicationFactory.Fijos.Single(f => f.Usuario == usuario);
        var seccion = Secciones.Single(s => s.Ruta == ruta);
        var cliente = Cliente(app, seguirRedirecciones: true);
        await IngresarAsync(cliente, fijo.Usuario, fijo.Contrasena);

        var respuesta = await cliente.GetAsync(ruta);

        var html = WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());
        if (seccion.Roles.Contains(fijo.Rol))
        {
            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.Contains($"<h1>{seccion.Titulo}</h1>", html);
            // Cada story que construye una sección suma su ruta a RutasYaConstruidas (story 2.2, AC 27).
            if (RutasYaConstruidas.Contains(ruta))
                Assert.DoesNotContain(TextoEnConstruccion, html);
            else
                Assert.Contains(TextoEnConstruccion, html);
        }
        else
        {
            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
            Assert.Contains("No tenés permiso para entrar a esta sección.", html);
            Assert.Contains($"href=\"{InicioDe[fijo.Rol]}\"", html);
        }
    }

    [Fact]
    [Trait("Story", "2.1")]
    [Trait("AC", "15")]
    public async Task PostSalir_ConSesion_BorraCookieYVuelveAlIngreso()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var cliente = Cliente(app);
        var fijo = PersonalWebApplicationFactory.Recepcion;
        await IngresarAsync(cliente, fijo.Usuario, fijo.Contrasena);

        var salida = await SalirAsync(cliente, "/personal/agendas");
        var despues = await cliente.GetAsync("/personal/agendas");

        Assert.Equal(HttpStatusCode.Redirect, salida.StatusCode);
        Assert.Equal(RutaIngreso, Destino(salida));
        Assert.True(VenceLaCookie(salida), "la salida tiene que vencer la cookie del personal");
        Assert.Equal(HttpStatusCode.Redirect, despues.StatusCode);
        Assert.StartsWith(RutaIngreso, Destino(despues));
    }

    [Theory]
    [Trait("Story", "2.1")]
    [Trait("AC", "25")]
    [InlineData("desactivado")]
    [InlineData("sello cambiado")]
    public async Task SesionPersonal_UsuarioDesactivadoOSelloCambiado_SeCortaAlRevalidar(string cambio)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        const string clave = "Clave-Propia-2026";
        var usuario = await CrearUsuarioAsync(app, [Roles.Recepcionista], clave);
        var cliente = Cliente(app);
        await IngresarAsync(cliente, usuario.UserName!, clave);
        using (var alcance = app.Services.CreateScope())
        {
            var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>();
            var cargado = (await usuarios.FindByIdAsync(usuario.Id.ToString()))!;
            if (cambio == "desactivado")
            {
                cargado.Activo = false;
                Assert.True((await usuarios.UpdateAsync(cargado)).Succeeded);
            }
            else
            {
                Assert.True((await usuarios.UpdateSecurityStampAsync(cargado)).Succeeded);
            }
        }

        app.Reloj.Advance(TimeSpan.FromMinutes(4));
        var antesDeRevalidar = await cliente.GetAsync(Turnos);
        app.Reloj.Advance(TimeSpan.FromMinutes(2));
        var alRevalidar = await cliente.GetAsync(Turnos);

        Assert.Equal(HttpStatusCode.OK, antesDeRevalidar.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, alRevalidar.StatusCode);
        Assert.Equal(IngresoConVueltaATurnos, Destino(alRevalidar));
        Assert.True(VenceLaCookie(alRevalidar), "la revalidación tiene que vencer la cookie del personal");
    }

    [Theory]
    [Trait("Story", "2.1")]
    [Trait("AC", "27")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ProteccionDeDatos_CarpetaCompartida_LaSesionSobreviveAOtraInstancia(bool mismaCarpeta)
    {
        var carpetaA = Directory.CreateTempSubdirectory("claves-a-");
        var carpetaB = mismaCarpeta ? carpetaA : Directory.CreateTempSubdirectory("claves-b-");
        try
        {
            string cookie;
            await using (var fabricaA = new PersonalWebApplicationFactory(fixture))
            await using (var appA = ConCarpeta(fabricaA, carpetaA))
            {
                var fijo = PersonalWebApplicationFactory.Recepcion;
                var ingreso = await IngresarAsync(Cliente(appA), fijo.Usuario, fijo.Contrasena);
                cookie = Assert.Single(CookiesPersonal(ingreso)).Split(';')[0];
            }

            await using var fabricaB = new PersonalWebApplicationFactory(fixture);
            await using var appB = ConCarpeta(fabricaB, carpetaB);
            var pedido = new HttpRequestMessage(HttpMethod.Get, Turnos);
            pedido.Headers.Add("Cookie", cookie);
            var respuesta = await Cliente(appB).SendAsync(pedido);

            if (mismaCarpeta)
            {
                Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
                Assert.NotEmpty(carpetaA.GetFiles("key-*.xml"));
            }
            else
            {
                Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
                Assert.StartsWith(RutaIngreso, Destino(respuesta));
            }
        }
        finally
        {
            carpetaA.Delete(recursive: true);
            if (!mismaCarpeta)
                carpetaB.Delete(recursive: true);
        }
    }

    private static Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> ConCarpeta(
        PersonalWebApplicationFactory fabrica, DirectoryInfo carpeta) =>
        fabrica.WithWebHostBuilder(b => b.ConfigureAppConfiguration(c => c.AddInMemoryCollection(
            new Dictionary<string, string?> { ["ProteccionDeDatos:Carpeta"] = carpeta.FullName })));
}
