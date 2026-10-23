using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using Bunit;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Web.Components.Personal;
using HospitalTurnos.Web.Components.Personal.Administracion;
using HospitalTurnos.Web.Personal;
using HospitalTurnos.Web.Tests.Ayudas;
using HospitalTurnos.Web.Tests.Catalogo;
using HospitalTurnos.Web.Tests.Personal;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using static HospitalTurnos.Web.Tests.Personal.FormularioHttp;

namespace HospitalTurnos.Web.Tests.Comunes;

// Story 3.3, sección C (guía "Navegación y menú principal"): el marco es estático (SSR) y se mira por HTTP; las
// migas viven en las páginas interactivas y se miran como componentes.
[Collection("Postgres")]
public class MarcoPersonalTests(PostgresFixture fixture) : BunitContext
{
    private static async Task<HttpClient> Admin1Async(PersonalWebApplicationFactory app)
    {
        var cliente = Cliente(app);
        var ingreso = await IngresarAsync(cliente, PersonalWebApplicationFactory.Admin.Usuario,
            PersonalWebApplicationFactory.Admin.Contrasena);
        Assert.NotEmpty(CookiesPersonal(ingreso));
        return cliente;
    }

    private static async Task<AngleSharp.Html.Dom.IHtmlDocument> DocumentoAsync(HttpClient cliente, string ruta)
    {
        var respuesta = await cliente.GetAsync(ruta);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return new HtmlParser().ParseDocument(await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "11")]
    [Trait("AC", "12")]
    [Trait("AC", "36")]
    public async Task Marco_MuestraBarraSuperiorYMenuDeUsuario()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var cliente = await Admin1Async(app);

        var html = await DocumentoAsync(cliente, RutasPersonal.Especialidades);

        var barra = html.QuerySelector("header.barra-superior")!;
        Assert.Equal("Las Acacias", barra.QuerySelector(".barra-superior__marca")!.TextContent.Trim());
        var menu = barra.QuerySelector("details.menu-de-usuario")!;
        Assert.Equal("Lucía Pérez ▾", Regex.Replace(menu.QuerySelector("summary")!.TextContent, @"\s+", " ").Trim());
        Assert.Equal(RutasPersonal.CambiarContrasena,
            menu.QuerySelectorAll("a").Single(a => a.TextContent.Trim() == "Cambiar contraseña").GetAttribute("href"));
        // AC 36: los enlaces del marco recargan la página (el navegador avisa si hay cambios sin guardar).
        Assert.All(html.QuerySelectorAll("header.barra-superior a, aside.menu-lateral-fijo a"),
            a => Assert.Equal("false", a.GetAttribute("data-enhance-nav")));
        var salir = menu.QuerySelector($"form[action='{RutasPersonal.Salir}']")!;
        Assert.Equal("post", salir.GetAttribute("method"), ignoreCase: true);
        Assert.NotNull(salir.QuerySelector("input[name=__RequestVerificationToken]"));
        Assert.Equal("Cerrar sesión", salir.QuerySelector("button")!.TextContent.Trim());
        // El botón "Salir" suelto ya no existe (B-1.2).
        Assert.DoesNotContain(html.QuerySelectorAll("button"), b => b.TextContent.Trim() == "Salir");
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "12")]
    public async Task CerrarSesion_SigueSiendoPostConAntifalsificacion()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var cliente = await Admin1Async(app);

        var sinToken = await SalirAsync(cliente, RutasPersonal.Especialidades, conToken: false);
        var conToken = await SalirAsync(cliente, RutasPersonal.Especialidades);

        Assert.Equal(HttpStatusCode.BadRequest, sinToken.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, conToken.StatusCode);
        Assert.StartsWith(RutasPersonal.Ingreso, Destino(conToken));
        Assert.Contains(CookiesPersonal(conToken), c => c.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase));
    }

    public static TheoryData<string, string[]> GruposPorRol => new()
    {
        {
            Roles.Administrador,
            [
                "Agenda: Agendas · Propuestas de agenda · Bloqueos de agenda · Pacientes para avisar",
                "Catálogos: Médicos · Especialidades",
                "Administración: Usuarios del personal · Auditoría",
            ]
        },
        { Roles.Recepcionista, ["Recepción: Turnos", "Agenda: Agendas · Pacientes para avisar"] },
        { Roles.Medico, ["Mi agenda: Mi agenda · Mi propuesta de agenda"] },
    };

    [Theory]
    [Trait("Story", "3.3")]
    [Trait("AC", "13")]
    [MemberData(nameof(GruposPorRol))]
    public void Menu_AgrupaPorRol_EnElOrdenDefinido(string rol, string[] esperado)
    {
        var grupos = RutasPersonal.GruposDe(rol)
            .Select(g => $"{g.Rotulo}: {string.Join(" · ", g.Secciones.Select(s => s.Titulo))}");

        Assert.Equal(esperado, grupos);
    }

    [Theory]
    [Trait("Story", "3.3")]
    [Trait("AC", "14")]
    [InlineData(Roles.Administrador, "/personal/admin/medicos")]
    [InlineData(Roles.Recepcionista, "/personal/recepcion/turnos")]
    [InlineData(Roles.Medico, "/personal/medico/mi-agenda")]
    [InlineData("Otro", "/personal/ingreso")]
    public void InicioDespuesDeIngresar_NoCambia(string rol, string inicio)
    {
        Assert.Equal(inicio, RutasPersonal.InicioDe(rol));
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "15")]
    public async Task OpcionActiva_TieneAriaCurrent_TambienEnSubrutas()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var especialidad = await app.CrearEspecialidadAsync();
        var medico = await app.CrearMedicoAsync("Pablo", "Ríos", $"MP {Random.Shared.Next(100000, 999999)}", especialidad);
        var cliente = await Admin1Async(app);

        var html = await DocumentoAsync(cliente, RutasPersonal.FichaMedico(medico));

        var actuales = html.QuerySelectorAll("nav [aria-current=page]");
        Assert.NotEmpty(actuales);
        Assert.All(actuales, a =>
        {
            Assert.Equal("Médicos", a.TextContent.Trim());
            Assert.Contains("active", a.ClassList);
        });
        Assert.All(html.QuerySelectorAll("nav .active"), a => Assert.Equal("Médicos", a.TextContent.Trim()));
    }

    private sealed class TokenDePrueba : AntiforgeryStateProvider
    {
        public override AntiforgeryRequestToken? GetAntiforgeryToken() => new("token-de-prueba", "__RequestVerificationToken");
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "16")]
    [Trait("AC", "17")]
    public void ItemDeMenu_AceptaContador_YHoyNoMuestraNinguno()
    {
        Services.AddSingleton<AntiforgeryStateProvider, TokenDePrueba>();
        var autorizacion = AddAuthorization();
        autorizacion.SetAuthorized("lucia");
        autorizacion.SetRoles(Roles.Administrador);
        autorizacion.SetClaims(new Claim("nombre_completo", "Lucía Pérez"));

        var dePrueba = Render<MenuLateral>(p => p.Add(m => m.Grupos,
            [new RutasPersonal.GrupoDeMenu("Agenda", [new RutasPersonal.Seccion(RutasPersonal.Propuestas, "Propuestas de agenda", 3)])]));
        var real = Render<MenuLateral>();

        Assert.Equal("3", dePrueba.Find(".menu-lateral__contador").TextContent.Trim());
        Assert.Empty(real.FindAll(".menu-lateral__contador"));
        Assert.All(real.FindAll("nav a"), a =>
        {
            Assert.Contains("menu-lateral__item", a.ClassList);
            Assert.Null(a.GetAttribute("style"));
        });
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "18")]
    public async Task PantallaChica_MuestraBotonMenu()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var cliente = await Admin1Async(app);

        var html = await DocumentoAsync(cliente, RutasPersonal.Especialidades);

        var compacto = html.QuerySelector("details.menu-compacto")!;
        Assert.Equal("☰ Menú", compacto.QuerySelector("summary")!.TextContent.Trim());
        Assert.NotEmpty(compacto.QuerySelectorAll(".menu-compacto__panel nav a"));
        Assert.NotNull(html.QuerySelector("aside.menu-lateral-fijo nav"));
    }

    public static TheoryData<string, string[], string> MigasPorPantalla => new()
    {
        { "especialidades", ["Catálogos", "Especialidades"], "Especialidades" },
        { "medicos", ["Catálogos", "Médicos"], "Médicos" },
        { "nuevo", ["Catálogos", "Médicos", "Nuevo médico"], "Nuevo médico" },
        { "ficha", ["Catálogos", "Médicos", "Ríos, Pablo"], "Ríos, Pablo" },
        { "usuarios", ["Administración", "Usuarios del personal"], "Usuarios del personal" },
    };

    [Theory]
    [Trait("Story", "3.3")]
    [Trait("AC", "19")]
    [MemberData(nameof(MigasPorPantalla))]
    public async Task Migas_YTitulo_CoincidenConElMenu(string pantalla, string[] migas, string titulo)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        using var alcance = this.ConLaApp(app);
        await this.ComoAdmin1Async(app);
        var especialidad = await app.CrearEspecialidadAsync();
        var medico = await app.CrearMedicoAsync("Pablo", "Ríos", $"MP {Random.Shared.Next(100000, 999999)}", especialidad);

        var tipo = pantalla switch
        {
            "especialidades" => typeof(Especialidades),
            "medicos" => typeof(Medicos),
            "nuevo" => typeof(MedicoNuevo),
            "ficha" => typeof(FichaMedico),
            _ => typeof(UsuariosPersonal),
        };
        var pagina = Render(b =>
        {
            b.OpenComponent(0, tipo);
            if (tipo == typeof(FichaMedico))
                b.AddAttribute(1, nameof(FichaMedico.Id), medico);
            b.CloseComponent();
        });

        await pagina.WaitForAssertionAsync(() => Assert.NotEmpty(pagina.FindAll("nav.migas li")));
        var niveles = pagina.FindAll("nav.migas li");
        Assert.Equal(migas, niveles.Select(n => n.TextContent.Trim()));
        Assert.All(niveles.Take(niveles.Count - 1), n => Assert.NotNull(n.QuerySelector("a")));
        // AC 36 (decisión del docente): recargan la página, así el navegador avisa si hay cambios sin guardar.
        Assert.All(niveles.Take(niveles.Count - 1), n => Assert.Equal("false", n.QuerySelector("a")!.GetAttribute("data-enhance-nav")));
        Assert.Null(niveles[^1].QuerySelector("a"));
        Assert.Equal(titulo, (await pagina.WaitForElementAsync("h1")).TextContent.Trim().Split('·')[0].Trim());
        Assert.DoesNotContain("Volver a Médicos", pagina.Markup);
    }
}
