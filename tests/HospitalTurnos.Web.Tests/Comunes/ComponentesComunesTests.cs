using System.ComponentModel.DataAnnotations;
using Bunit;
using Bunit.TestDoubles;
using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Comun;
using HospitalTurnos.Web.Components.Comunes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace HospitalTurnos.Web.Tests.Comunes;

// Story 3.3, sección E: los componentes comunes se prueban solos, antes de migrar ninguna pantalla. El tiempo
// (notificación de 4 s, espera de 300 ms del buscador) se prueba SIEMPRE con el reloj falso, nunca esperando.
public class ComponentesComunesTests : BunitContext
{
    private readonly FakeTimeProvider reloj = new(new DateTimeOffset(2026, 10, 17, 15, 0, 0, TimeSpan.Zero));

    public ComponentesComunesTests()
    {
        Services.AddSingleton<TimeProvider>(reloj);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static string Texto(AngleSharp.Dom.IElement elemento) =>
        System.Text.RegularExpressions.Regex.Replace(elemento.TextContent, @"\s+", " ").Trim();

    // ---------- D. Notificación (AC 20) ----------

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "20")]
    public void Notificacion_SeAnuncia_YDesapareceALos4Segundos()
    {
        Services.AddScoped<Notificaciones>();
        var zona = Render<ZonaDeNotificaciones>();
        var notificaciones = Services.GetRequiredService<Notificaciones>();

        zona.InvokeAsync(() => notificaciones.Mostrar("Especialidad creada"));
        reloj.Advance(TimeSpan.FromMilliseconds(3900));
        zona.WaitForAssertion(() => Assert.Contains("Especialidad creada", zona.Find("[aria-live=polite]").TextContent));
        reloj.Advance(TimeSpan.FromMilliseconds(200));

        zona.WaitForAssertion(() => Assert.DoesNotContain("Especialidad creada", zona.Markup));
        Assert.NotNull(zona.Find("[aria-live=polite]"));   // la región queda: así el lector anuncia el próximo
    }

    // ---------- E. Página, filtros y URL ----------

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "22")]
    public void PaginaAbm_TieneUnUnicoPrimario_YElTotal()
    {
        Services.AddScoped<Notificaciones>();
        AddAuthorization().SetAuthorized("lucia");
        var nuevo = 0;

        var pagina = Render<PaginaAbm>(p => p
            .Add(x => x.Titulo, "Especialidades")
            .Add(x => x.Total, 18)
            .Add(x => x.Migas, [new Miga("Catálogos", "/personal/admin/medicos"), new Miga("Especialidades", null)])
            .Add(x => x.TextoNuevo, "Nueva especialidad")
            .Add(x => x.AlNuevo, () => nuevo++)
            .AddChildContent("<p>contenido</p>"));

        Assert.Equal("Especialidades · 18", Texto(pagina.Find(".page-header h1")));
        var primario = Assert.Single(pagina.FindAll(".btn-primary"));
        Assert.Equal("Nueva especialidad", Texto(primario));
        Assert.Contains("page-header__accion", primario.ClassList);   // a la derecha del título
        primario.Click();
        Assert.Equal(1, nuevo);
        Assert.NotNull(pagina.Find("nav.migas"));
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "23")]
    public void Buscador_FiltraA300ms_YConEnter()
    {
        var buscados = new List<string?>();
        var barra = Render<BarraDeFiltros>(p => p
            .Add(x => x.Placeholder, "Buscar por apellido, nombre o matrícula")
            .Add(x => x.BuscarCambiado, (string? t) => buscados.Add(t)));

        var buscador = barra.Find("input#buscar");
        Assert.Equal("Buscar", Texto(barra.Find("label[for=buscar]")));
        Assert.Equal("Buscar por apellido, nombre o matrícula", buscador.GetAttribute("placeholder"));

        buscador.Input("ríos");
        reloj.Advance(TimeSpan.FromMilliseconds(299));
        Assert.Empty(buscados);
        reloj.Advance(TimeSpan.FromMilliseconds(1));
        barra.WaitForAssertion(() => Assert.Equal(["ríos"], buscados));

        buscador.Input("pereyra");
        buscador.KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(["ríos", "pereyra"], buscados);
        reloj.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(["ríos", "pereyra"], buscados);   // el Enter cancela la espera: no filtra dos veces
    }

    private static readonly ReglasDeListado ReglasDeMedicos = new("medico", ["medico", "matricula", "especialidad"],
        Agendas: ["vigente", "programada", "pendiente", "sin-agenda"]);

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "24")]
    [Trait("AC", "25")]
    public void LimpiarFiltros_VuelveALosValoresPorDefecto()
    {
        var estado = EstadoDeListado.PorDefecto(ReglasDeMedicos) with { Buscar = "ríos", Estado = "todos", Pagina = 2 };
        var limpiado = 0;

        var barra = Render<BarraDeFiltros>(p => p
            .Add(x => x.Buscar, estado.Buscar)
            .Add(x => x.FiltrosActivos, estado.FiltrosActivos(_ => null))
            .Add(x => x.AlLimpiar, () => limpiado++)
            .AddChildContent<FiltroDeEstado>(f => f.Add(x => x.Valor, estado.Estado)));

        Assert.Equal(["Búsqueda: ríos", "Estado: Todos"], barra.FindAll(".filtros-activos__filtro").Select(Texto));
        barra.FindAll("button").Single(b => Texto(b) == "Limpiar filtros").Click();
        Assert.Equal(1, limpiado);
        Assert.Equal(EstadoDeListado.PorDefecto(ReglasDeMedicos), estado.Limpio(ReglasDeMedicos));
        Assert.Equal("activos", estado.Limpio(ReglasDeMedicos).Estado);

        var filtro = Render<FiltroDeEstado>();
        Assert.Equal(["Activos", "Inactivos", "Todos"], filtro.FindAll("option").Select(Texto));
        Assert.Equal("activos", filtro.Find("select").GetAttribute("value"));
        var femenino = Render<FiltroDeEstado>(p => p.Add(x => x.Femenino, true));
        Assert.Equal(["Activas", "Inactivas", "Todas"], femenino.FindAll("option").Select(Texto));
        Assert.Empty(Render<BarraDeFiltros>().FindAll(".filtros-activos"));
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "26")]
    public void EstadoDelListado_SeLeeYEscribeEnLaUrl()
    {
        var completo = new EstadoDeListado
        {
            Buscar = "ríos pablo", Estado = "todos", Especialidad = 7, Agenda = "vigente", Rol = null,
            Orden = "-matricula", Pagina = 3, Tamano = 50,
        };

        var url = completo.Url("/personal/admin/medicos", ReglasDeMedicos);
        var leido = EstadoDeListado.Leer($"https://localhost{url}", ReglasDeMedicos);

        Assert.Equal(completo, leido);
        Assert.Equal("/personal/admin/medicos?buscar=r%C3%ADos%20pablo&estado=todos&especialidad=7&agenda=vigente&orden=-matricula&pagina=3&tamano=50", url);
        Assert.Equal("/personal/admin/medicos", EstadoDeListado.PorDefecto(ReglasDeMedicos).Url("/personal/admin/medicos", ReglasDeMedicos));
    }

    [Theory]
    [Trait("Story", "3.3")]
    [Trait("AC", "80")]
    [InlineData("orden=xyz")]
    [InlineData("pagina=-3")]
    [InlineData("pagina=abc")]
    [InlineData("tamano=7")]
    [InlineData("estado=otro")]
    [InlineData("agenda=quizas")]
    [InlineData("especialidad=-1")]
    [InlineData("orden=-")]
    public void ValorDesconocidoEnLaUrl_VuelveAlDefecto(string consulta)
    {
        var leido = EstadoDeListado.Leer($"https://localhost/personal/admin/medicos?{consulta}", ReglasDeMedicos);

        Assert.Equal(EstadoDeListado.PorDefecto(ReglasDeMedicos), leido);
    }

    // ---------- E. Tabla ----------

    private sealed record Fila(long Id, string Nombre, int Medicos);

    private static readonly IReadOnlyList<ColumnaAbm<Fila>> Columnas =
    [
        new("Médico", f => b => b.AddContent(0, f.Nombre), Orden: "medico"),
        new("Médicos asociados", f => b => b.AddContent(0, f.Medicos), Numero: true),
    ];

    private IRenderedComponent<TablaAbm<Fila>> Tabla(Func<CancellationToken, Task<PaginaDeResultados<Fila>>> consultar,
        Action<ComponentParameterCollectionBuilder<TablaAbm<Fila>>>? mas = null) =>
        Render<TablaAbm<Fila>>(p =>
        {
            p.Add(x => x.Columnas, Columnas)
             .Add(x => x.Consultar, consultar)
             .Add(x => x.Entidad, "médicos")
             .Add(x => x.TextoVacio, "Todavía no hay médicos. Creá el primero.")
             .Add(x => x.Acciones, (RenderFragment<Fila>)(f => b => b.AddContent(0, "⋯")))
             .Add(x => x.Tarjeta, (RenderFragment<Fila>)(f => b => b.AddContent(0, f.Nombre)));
            mas?.Invoke(p);
        });

    private static Task<PaginaDeResultados<Fila>> Con(params Fila[] filas) =>
        Task.FromResult(new PaginaDeResultados<Fila>(filas, filas.Length));

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "27")]
    public void Tabla_OrdenaConIndicadorYAriaSort()
    {
        var pedidos = new List<string>();
        var tabla = Tabla(_ => Con(new Fila(1, "Ríos, Pablo", 3)), p => p
            .Add(x => x.Orden, "medico")
            .Add(x => x.OrdenCambiado, (string o) => pedidos.Add(o)));

        var encabezado = tabla.Find("th[aria-sort]");
        Assert.Equal("ascending", encabezado.GetAttribute("aria-sort"));
        Assert.Contains("▲", encabezado.TextContent);
        encabezado.QuerySelector("button")!.Click();
        Assert.Equal(["-medico"], pedidos);
        tabla.Render(p => p.Add(x => x.Orden, "-medico"));
        encabezado = tabla.Find("th[aria-sort]");
        Assert.Equal("descending", encabezado.GetAttribute("aria-sort"));
        Assert.Contains("▼", encabezado.TextContent);
        encabezado.QuerySelector("button")!.Click();
        Assert.Equal(["-medico", "medico"], pedidos);

        Assert.Contains("numero", tabla.FindAll("tbody td")[1].ClassList);
        Assert.Contains("table-actions", tabla.FindAll("tbody td")[^1].ClassList);
        Assert.Contains("table-actions", tabla.FindAll("thead th")[^1].ClassList);
    }

    [Theory]
    [Trait("Story", "3.3")]
    [Trait("AC", "28")]
    [InlineData("cargando")]
    [InlineData("vacio")]
    [InlineData("sin-resultados")]
    public void Tabla_DistingueCargaVacioYSinResultados(string caso)
    {
        var pendiente = new TaskCompletionSource<PaginaDeResultados<Fila>>();
        var limpiar = 0;
        var tabla = Tabla(_ => caso == "cargando" ? pendiente.Task : Con(), p => p
            .Add(x => x.Buscado, caso == "sin-resultados" ? "Pereira" : null)
            .Add(x => x.HayFiltros, caso == "sin-resultados")
            .Add(x => x.AlLimpiar, () => limpiar++)
            .Add(x => x.AccionVacio, (RenderFragment)(b => b.AddMarkupContent(0, "<button class=\"btn btn-primary\">Nuevo médico</button>"))));

        switch (caso)
        {
            case "cargando":
                Assert.NotEmpty(tabla.FindAll("tbody tr.esqueleto"));
                Assert.Equal("true", tabla.Find("table").GetAttribute("aria-busy"));
                break;
            case "vacio":
                tabla.WaitForAssertion(() => Assert.Equal("Todavía no hay médicos. Creá el primero.", Texto(tabla.Find(".estado-vacio p"))));
                Assert.Equal("Nuevo médico", Texto(tabla.Find(".estado-vacio button")));
                Assert.NotNull(tabla.Find(".estado-vacio__icono"));
                break;
            default:
                tabla.WaitForAssertion(() => Assert.Equal("No hay médicos que coincidan con 'Pereira'", Texto(tabla.Find(".estado-vacio p"))));
                tabla.FindAll(".estado-vacio button").Single(b => Texto(b) == "Limpiar filtros").Click();
                Assert.Equal(1, limpiar);
                break;
        }
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "83")]
    public void Tabla_ErrorDeCarga_MuestraReintentar()
    {
        var consultas = 0;
        var tabla = Tabla(_ =>
        {
            consultas++;
            return consultas == 1
                ? Task.FromException<PaginaDeResultados<Fila>>(new InvalidOperationException("base caída: detalle técnico"))
                : Con(new Fila(1, "Ríos, Pablo", 3));
        });

        tabla.WaitForAssertion(() => Assert.Equal("No pudimos cargar la lista.", Texto(tabla.Find("[role=alert] p"))));
        Assert.DoesNotContain("detalle técnico", tabla.Markup);
        tabla.FindAll("button").Single(b => Texto(b) == "Reintentar").Click();

        tabla.WaitForAssertion(() => Assert.Contains("Ríos, Pablo", tabla.Find("tbody").TextContent));
        Assert.Equal(2, consultas);
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "29")]
    public void Tabla_EnCelular_DibujaTarjetas()
    {
        Services.AddScoped<Notificaciones>();
        AddAuthorization().SetAuthorized("lucia");
        var tabla = Tabla(_ => Con(new Fila(1, "Ríos, Pablo", 3), new Fila(2, "Castro, Carla", 0)));

        tabla.WaitForAssertion(() => Assert.Equal(2, tabla.FindAll(".tarjetas-abm .tarjeta-abm").Count));
        Assert.All(tabla.FindAll(".tarjeta-abm"), t => Assert.Contains("⋯", t.TextContent));
        Assert.Contains("Ríos, Pablo", tabla.Find(".tarjeta-abm").TextContent);

        var pagina = Render<PaginaAbm>(p => p.Add(x => x.Titulo, "Médicos").Add(x => x.Migas, [new Miga("Médicos", null)])
            .Add(x => x.TextoNuevo, "Nuevo médico"));
        Assert.Contains("boton-alta-fijo", pagina.Find(".page-header .btn-primary").ClassList);
    }

    // ---------- E. Menú de acciones, paginador, insignia ----------

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "30")]
    public void MenuDeAcciones_TieneNombreAccesible_YTeclado()
    {
        var elegidas = new List<string>();
        var menu = Render<MenuDeAcciones>(p => p
            .Add(x => x.Registro, "Ríos, Pablo")
            .Add(x => x.Opciones,
            [
                new MenuDeAcciones.Opcion("Vincular usuario", EventCallback.Factory.Create(this, () => elegidas.Add("vincular"))),
                new MenuDeAcciones.Opcion("Desactivar", EventCallback.Factory.Create(this, () => elegidas.Add("desactivar"))),
            ]));

        var boton = menu.Find("button.menu-de-acciones__boton");
        var idBoton = Referencia(boton);
        Assert.Equal("Más acciones para Ríos, Pablo", boton.GetAttribute("aria-label"));
        Assert.Equal("⋯", Texto(boton));
        Assert.Empty(menu.FindAll("[role=menu]"));

        boton.KeyDown(new KeyboardEventArgs { Key = "Enter" });
        var opciones = menu.FindAll("[role=menuitem]");
        var ids = opciones.Select(Referencia).ToList();   // bUnit sólo los muestra en el dibujo que los crea
        Assert.Equal(["Vincular usuario", "Desactivar"], opciones.Select(Texto));
        Assert.Equal("true", menu.Find("button.menu-de-acciones__boton").GetAttribute("aria-expanded"));
        Assert.Equal(ids[0], UltimoFoco());
        // Un segundo Enter antes de que el foco llegue (lo encontró la prueba en vivo): sigue abierto y enfoca lo mismo.
        menu.Find("button.menu-de-acciones__boton").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(ids[0], UltimoFoco());

        menu.Find("[role=menu]").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Equal(ids[1], UltimoFoco());
        menu.Find("[role=menu]").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Equal(ids[0], UltimoFoco());   // vuelve al primero

        menu.Find("[role=menu]").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Empty(menu.FindAll("[role=menu]"));
        Assert.Equal(idBoton, UltimoFoco());

        menu.Find("button.menu-de-acciones__boton").Click();
        menu.FindAll("[role=menuitem]")[1].Click();
        Assert.Equal(["desactivar"], elegidas);
        Assert.Empty(menu.FindAll("[role=menu]"));
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "30")]
    public void MenuDeAcciones_AbrirUnoCierraElOtro()
    {
        Services.AddScoped<MenuDeAcciones.UnoAbierto>();
        MenuDeAcciones.Opcion[] opciones = [new("Desactivar", EventCallback.Empty)];
        var primero = Render<MenuDeAcciones>(p => p.Add(x => x.Registro, "Cardiología").Add(x => x.Opciones, opciones));
        var segundo = Render<MenuDeAcciones>(p => p.Add(x => x.Registro, "Pediatría").Add(x => x.Opciones, opciones));

        primero.Find("button.menu-de-acciones__boton").Click();
        Assert.NotEmpty(primero.FindAll("[role=menu]"));
        segundo.Find("button.menu-de-acciones__boton").Click();

        Assert.NotEmpty(segundo.FindAll("[role=menu]"));
        primero.WaitForAssertion(() => Assert.Empty(primero.FindAll("[role=menu]")));   // uno solo por pantalla
    }

    // El último FocusAsync fue a ese elemento (verificación de bUnit, sin leer atributos internos).
    private void Enfocado(AngleSharp.Dom.IElement elemento) =>
        JSInterop.Invocations["Blazor._internal.domWrapper.focus"][^1].Arguments[0].ShouldBeElementReferenceTo(elemento);

    private static string Referencia(AngleSharp.Dom.IElement elemento) =>
        elemento.GetAttribute("blazor:elementReference") is { Length: > 0 } id
            ? id
            : throw new InvalidOperationException("El elemento no muestra su referencia en este dibujo.");

    private string UltimoFoco() =>
        ((ElementReference)JSInterop.Invocations["Blazor._internal.domWrapper.focus"][^1].Arguments[0]!).Id;

    [Theory]
    [Trait("Story", "3.3")]
    [Trait("AC", "31")]
    [InlineData(25, null)]
    [InlineData(26, "Mostrando 1–25 de 26")]
    [InlineData(124, "Mostrando 1–25 de 124")]
    public void Paginador_ApareceDesde26_YDiceMostrando(int total, string? texto)
    {
        var paginas = new List<int>();
        var tamanos = new List<int>();
        var paginador = Render<Paginador>(p => p
            .Add(x => x.Total, total).Add(x => x.Pagina, 1).Add(x => x.Tamano, 25)
            .Add(x => x.PaginaCambiada, (int n) => paginas.Add(n))
            .Add(x => x.TamanoCambiado, (int n) => tamanos.Add(n)));

        if (texto is null)
        {
            Assert.Empty(paginador.FindAll(".paginador"));
            return;
        }
        Assert.Equal(texto, Texto(paginador.Find(".paginador__texto")));
        Assert.Equal(["25", "50", "100"], paginador.FindAll(".paginador__tamano option").Select(o => o.GetAttribute("value")));
        paginador.FindAll("button").Single(b => Texto(b) == "Siguiente").Click();
        Assert.Equal([2], paginas);
        paginador.Find(".paginador__tamano select").Change("50");
        Assert.Equal([50], tamanos);
        Assert.True(paginador.FindAll("button").Single(b => Texto(b) == "Anterior").HasAttribute("disabled"));
    }

    [Theory]
    [Trait("Story", "3.3")]
    [Trait("AC", "32")]
    [InlineData(InsigniaDeEstado.Tono.Vigente, "vigente")]
    [InlineData(InsigniaDeEstado.Tono.Programada, "programada")]
    [InlineData(InsigniaDeEstado.Tono.Pendiente, "pendiente")]
    [InlineData(InsigniaDeEstado.Tono.Inactivo, "inactivo")]
    [InlineData(InsigniaDeEstado.Tono.Activo, "activo")]
    [InlineData(InsigniaDeEstado.Tono.Info, "info")]
    public void Insignia_SiempreTieneTexto(InsigniaDeEstado.Tono tono, string clase)
    {
        var insignia = Render<InsigniaDeEstado>(p => p.Add(x => x.Texto, "Estado").Add(x => x.Variante, tono));

        var span = insignia.Find("span.badge-estado");
        Assert.Equal("Estado", Texto(span));
        Assert.Contains($"badge-estado--{clase}", span.ClassList);
        Assert.Throws<ArgumentException>(() => Render<InsigniaDeEstado>(p => p.Add(x => x.Texto, " ").Add(x => x.Variante, tono)));
    }

    // ---------- E. Formulario ----------

    private sealed class Modelo
    {
        [Required(ErrorMessage = "Ingresá el nombre.")]
        public string? Nombre { get; set; }

        [Required(ErrorMessage = "Ingresá el apellido.")]
        public string? Apellido { get; set; }

        public string? Usuario { get; set; }
    }

    private IRenderedComponent<FormularioAbm> Formulario(Modelo modelo, Func<Task> alGuardar, Action? alCancelar = null) =>
        Render<FormularioAbm>(p => p
            .Add(x => x.Model, modelo)
            .Add(x => x.AlGuardar, alGuardar)
            .Add(x => x.AlCancelar, () => alCancelar?.Invoke())
            .Add(x => x.ChildContent, (RenderFragment)(b =>
            {
                b.OpenComponent<CampoDeFormulario<string?>>(0);
                b.AddComponentParameter(1, "Etiqueta", "Nombre");
                b.AddComponentParameter(2, "Id", "nombre");
                b.AddComponentParameter(3, "Obligatorio", true);
                b.AddComponentParameter(4, "Valor", modelo.Nombre);
                b.AddComponentParameter(5, "ValorChanged", EventCallback.Factory.Create<string?>(this, v => modelo.Nombre = v));
                b.AddComponentParameter(6, "ValorExpression", (System.Linq.Expressions.Expression<Func<string?>>)(() => modelo.Nombre));
                b.CloseComponent();
                b.OpenComponent<CampoDeFormulario<string?>>(10);
                b.AddComponentParameter(11, "Etiqueta", "Apellido");
                b.AddComponentParameter(12, "Id", "apellido");
                b.AddComponentParameter(13, "Obligatorio", true);
                b.AddComponentParameter(14, "Valor", modelo.Apellido);
                b.AddComponentParameter(15, "ValorChanged", EventCallback.Factory.Create<string?>(this, v => modelo.Apellido = v));
                b.AddComponentParameter(16, "ValorExpression", (System.Linq.Expressions.Expression<Func<string?>>)(() => modelo.Apellido));
                b.CloseComponent();
                b.OpenComponent<CampoDeFormulario<string?>>(20);
                b.AddComponentParameter(21, "Etiqueta", "Usuario del sistema");
                b.AddComponentParameter(22, "Id", "usuario");
                b.AddComponentParameter(23, "Ayuda", "Vinculalo para que el médico pueda ver y gestionar su agenda.");
                b.AddComponentParameter(24, "Valor", modelo.Usuario);
                b.AddComponentParameter(25, "ValorChanged", EventCallback.Factory.Create<string?>(this, v => modelo.Usuario = v));
                b.AddComponentParameter(26, "ValorExpression", (System.Linq.Expressions.Expression<Func<string?>>)(() => modelo.Usuario));
                b.CloseComponent();
            })));

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "33")]
    public void Formulario_MarcaObligatorios_YEnlazaErrores()
    {
        var formulario = Formulario(new Modelo(), () => Task.CompletedTask);

        Assert.Equal("* Obligatorio", Texto(formulario.Find(".formulario-abm__nota")));
        Assert.Contains("*", formulario.Find("label[for=nombre]").TextContent);
        Assert.Contains("(opcional)", formulario.Find("label[for=usuario]").TextContent);
        Assert.Equal("usuario-ayuda", formulario.Find("input#usuario").GetAttribute("aria-describedby"));
        Assert.Equal("Vinculalo para que el médico pueda ver y gestionar su agenda.", Texto(formulario.Find("#usuario-ayuda")));
        var botones = formulario.FindAll(".formulario-abm__botones button").Select(Texto).ToList();
        Assert.Equal(["Guardar", "Cancelar"], botones);
        Assert.Contains("btn-primary", formulario.FindAll(".formulario-abm__botones button")[0].ClassList);
        Assert.Contains("btn-outline-secondary", formulario.FindAll(".formulario-abm__botones button")[1].ClassList);
        Enfocado(formulario.Find("input#nombre"));   // foco en el primer campo

        formulario.Find("form").Submit();
        var error = formulario.Find("#nombre-error");
        Assert.Contains("Ingresá el nombre.", error.TextContent);
        Assert.Contains("campo__error", error.ClassList);
        Assert.NotNull(error.QuerySelector("[aria-hidden=true]"));   // el ícono
        Assert.Contains("nombre-error", formulario.Find("input#nombre").GetAttribute("aria-describedby"));
        Assert.Equal("true", formulario.Find("input#nombre").GetAttribute("aria-invalid"));
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "34")]
    public void Formulario_ValidaAlSalirDelCampo_YAlEnviar()
    {
        var guardados = 0;
        var formulario = Formulario(new Modelo(), () => { guardados++; return Task.CompletedTask; });
        var idNombre = Referencia(formulario.Find("input#nombre"));

        formulario.Find("input#apellido").Blur();
        Assert.Contains("Ingresá el apellido.", formulario.Find("#apellido-error").TextContent);
        Assert.Empty(formulario.FindAll("#nombre-error"));   // sólo el campo que dejó

        formulario.Find("form").Submit();

        Assert.Contains("Ingresá el nombre.", formulario.Find("#nombre-error").TextContent);
        Assert.Contains("Ingresá el apellido.", formulario.Find("#apellido-error").TextContent);
        Assert.Equal(idNombre, UltimoFoco());   // el primero con error
        Assert.Equal(0, guardados);
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "35")]
    public void Guardar_SeDeshabilita_YDiceGuardando()
    {
        var pendiente = new TaskCompletionSource();
        var llamadas = 0;
        var formulario = Formulario(new Modelo { Nombre = "Pablo", Apellido = "Ríos" }, () => { llamadas++; return pendiente.Task; });

        formulario.Find("form").Submit();
        formulario.Find("form").Submit();

        var guardar = formulario.FindAll(".formulario-abm__botones button")[0];
        Assert.True(guardar.HasAttribute("disabled"));
        Assert.Equal("Guardando…", Texto(guardar));
        Assert.Equal(1, llamadas);
        pendiente.SetResult();
        formulario.WaitForAssertion(() => Assert.Equal("Guardar", Texto(formulario.FindAll(".formulario-abm__botones button")[0])));
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "3")]
    [Trait("AC", "35")]
    public void ErrorDelDominioAlGuardar_VaAlResumen_ConElFoco()
    {
        // "Guardando…" deshabilita el botón y el navegador saca el foco: el error del dominio lo trae de vuelta al resumen.
        var formulario = Formulario(new Modelo { Nombre = "Pablo", Apellido = "Ríos" },
            () => throw new HospitalTurnos.Dominio.Excepciones.ConflictoException("Ya existe la especialidad Cardiología."));
        var idNombre = Referencia(formulario.Find("input#nombre"));
        var focosAntes = JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count;

        formulario.Find("form").Submit();

        var resumen = formulario.Find(".resumen-de-errores");
        Assert.Equal("Ya existe la especialidad Cardiología.", Texto(resumen));
        Assert.Equal("-1", resumen.GetAttribute("tabindex"));   // enfocable por código, fuera del orden de Tab
        // bUnit no muestra la referencia del resumen después de redibujar: se verifica que hubo un foco nuevo y que no
        // fue al campo (el único otro destino posible del formulario).
        Assert.Equal(focosAntes + 1, JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count);
        Assert.NotEqual(idNombre, UltimoFoco());
        Assert.Equal("Guardar", Texto(formulario.FindAll(".formulario-abm__botones button")[0]));   // se puede volver a intentar
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "36")]
    public void SalirConCambios_PideConfirmacion()
    {
        var cancelados = 0;
        var formulario = Formulario(new Modelo(), () => Task.CompletedTask, () => cancelados++);
        Assert.False(formulario.FindComponent<NavigationLock>().Instance.ConfirmExternalNavigation);
        formulario.Find("input#nombre").Change("Pablo");
        Assert.True(formulario.FindComponent<NavigationLock>().Instance.ConfirmExternalNavigation);

        var navegacion = Services.GetRequiredService<BunitNavigationManager>();
        formulario.InvokeAsync(() => navegacion.NavigateTo("/personal/admin/especialidades"));

        var dialogo = formulario.Find("[role=dialog]");
        Assert.Equal("Tenés cambios sin guardar. ¿Salir igual?", Texto(dialogo.QuerySelector(".dialogo__titulo")!));
        Assert.DoesNotContain("especialidades", navegacion.Uri);
        formulario.FindAll("[role=dialog] button").Single(b => Texto(b) == "Seguir editando").Click();
        Assert.Empty(formulario.FindAll("[role=dialog]"));

        formulario.FindAll(".formulario-abm__botones button").Single(b => Texto(b) == "Cancelar").Click();
        formulario.FindAll("[role=dialog] button").Single(b => Texto(b) == "Salir sin guardar").Click();
        Assert.Equal(1, cancelados);
    }

    [Theory]
    [Trait("Story", "3.3")]
    [Trait("AC", "37")]
    [InlineData(10, false)]
    [InlineData(11, true)]
    public void SelectConBusqueda_Desde11Opciones(int cantidad, bool conBuscador)
    {
        var opciones = Enumerable.Range(1, cantidad)
            .Select(i => new SelectConBusqueda.Opcion(i.ToString(), i == 11 ? "Traumatología" : $"Especialidad {i}")).ToList();
        string? elegido = null;

        var select = Render<SelectConBusqueda>(p => p
            .Add(x => x.Id, "especialidad").Add(x => x.Opciones, opciones).Add(x => x.TextoVacio, "Elegí una especialidad")
            .Add(x => x.ValorCambiado, (string? v) => elegido = v));

        Assert.Equal(cantidad + 1, select.FindAll("select#especialidad option").Count);
        if (!conBuscador)
        {
            Assert.Empty(select.FindAll("input"));
            return;
        }
        var buscador = select.Find("input#especialidad-buscar");
        Assert.Equal("Escribí para filtrar", buscador.GetAttribute("placeholder"));
        buscador.Input("trauma");
        Assert.Equal(["Elegí una especialidad", "Traumatología"], select.FindAll("select option").Select(Texto));
        select.Find("select").Change("11");
        Assert.Equal("11", elegido);
    }

    // ---------- E. Diálogos ----------

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "38")]
    public async Task Dialogo_AtrapaElFoco_CierraConEsc_YLoDevuelve()
    {
        var cerrados = 0;
        ElementReference origen = default;
        var abridor = Render(b =>
        {
            b.OpenElement(0, "button");
            b.AddAttribute(1, "id", "abrir");
            b.AddElementReferenceCapture(2, r => origen = r);
            b.CloseElement();
        });
        var dialogo = Render<DialogoModal>(p => p
            .Add(x => x.Titulo, "Nueva especialidad")
            .Add(x => x.Origen, origen)
            .Add(x => x.AlCerrar, () => cerrados++)
            .AddChildContent("<input id=\"campo\" />"));

        var panel = dialogo.Find("[role=dialog]");
        Assert.Equal("true", panel.GetAttribute("aria-modal"));
        Assert.Equal("Nueva especialidad", Texto(dialogo.Find($"#{panel.GetAttribute("aria-labelledby")}")));

        var centinelas = dialogo.FindAll(".dialogo__centinela");
        centinelas[^1].Focus();   // Tab desde el último control
        Enfocado(dialogo.Find(".dialogo__contenido"));
        centinelas[0].Focus();    // Mayús+Tab desde el primero
        Enfocado(dialogo.Find(".dialogo__fin"));

        panel.KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(1, cerrados);
        await DisposeComponentsAsync();
        Assert.Equal(origen.Id, UltimoFoco());   // el foco vuelve al botón que lo abrió
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "38")]
    public async Task Dialogo_AlCerrar_SinElControlQueLoAbrio_NoRompeLaPantalla()
    {
        // El "⋯" de la fila se redibujó (la tabla se recargó) y el navegador no lo puede enfocar: el cierre lo tolera.
        JSInterop.Mode = JSRuntimeMode.Strict;
        JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true)
            .SetException(new Microsoft.JSInterop.JSException("Unable to focus an invalid element."));
        ElementReference origen = default;
        Render(b =>
        {
            b.OpenElement(0, "button");
            b.AddElementReferenceCapture(1, r => origen = r);
            b.CloseElement();
        });
        Render<DialogoModal>(p => p.Add(x => x.Titulo, "¿Desactivar Cardiología?").Add(x => x.Origen, origen));

        var cierre = await Record.ExceptionAsync(DisposeComponentsAsync);

        Assert.Null(cierre);
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "39")]
    public void Confirmacion_TieneVerboConsecuencia_YFocoEnCancelar()
    {
        var dialogo = Render<DialogoDeConfirmacion>(p => p
            .Add(x => x.Titulo, "¿Desactivar Cardiología?")
            .Add(x => x.Consecuencia, "12 médicos la tienen asignada; no podrá elegirse en nuevos médicos.")
            .Add(x => x.TextoConfirmar, "Desactivar especialidad")
            .Add(x => x.AlConfirmar, (Func<Task>)(() => throw new HospitalTurnos.Dominio.Excepciones.ReglaDeNegocioException(
                "No se puede desactivar: hay turnos futuros asignados."))));

        Assert.Equal("¿Desactivar Cardiología?", Texto(dialogo.Find(".dialogo__titulo")));
        Assert.Contains("12 médicos la tienen asignada", dialogo.Markup);
        var botones = dialogo.FindAll(".dialogo__botones button");
        Assert.Equal(["Cancelar", "Desactivar especialidad"], botones.Select(Texto));
        Assert.Contains("btn-outline-secondary", botones[0].ClassList);
        Assert.Contains("btn-danger", botones[1].ClassList);
        Assert.Single(dialogo.FindAll(".btn-danger"));
        Enfocado(botones[0]);

        botones[1].Click();

        Assert.Contains("No se puede desactivar: hay turnos futuros asignados.", dialogo.Find(".dialogo").TextContent);
        Assert.Equal(["Entendido"], dialogo.FindAll(".dialogo__botones button").Select(Texto));
        Assert.Empty(dialogo.FindAll(".btn-danger"));
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "40")]
    public void Detalle_AbreEnLectura_YPieDeTrazabilidad()
    {
        var zona = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Cordoba");
        reloj.SetLocalTimeZone(zona);
        var creado = new MarcaDeTrazabilidad("Lucía Pérez", new DateTimeOffset(2026, 11, 16, 11, 5, 0, TimeSpan.Zero));
        var editado = new MarcaDeTrazabilidad("Diego Fernández", new DateTimeOffset(2026, 11, 20, 12, 30, 0, TimeSpan.Zero));
        var editar = 0;

        var detalle = Render<VistaDeDetalle>(p => p.Add(x => x.AlEditar, () => editar++).AddChildContent("<dl><dt>Matrícula</dt><dd>MP 1234</dd></dl>"));
        var solo = Render<PieDeTrazabilidad>(p => p.Add(x => x.Traza, new Trazabilidad(creado, null)));
        var ambos = Render<PieDeTrazabilidad>(p => p.Add(x => x.Traza, new Trazabilidad(creado, editado)));

        Assert.Empty(detalle.FindAll("input"));
        detalle.FindAll("button").Single(b => Texto(b) == "Editar").Click();
        Assert.Equal(1, editar);
        Assert.Equal("Creado por Lucía Pérez el 16/11/2026 08:05", Texto(solo.Find(".pie-de-trazabilidad")));
        Assert.Equal("Creado por Lucía Pérez el 16/11/2026 08:05 · Modificado por Diego Fernández el 20/11/2026 09:30",
            Texto(ambos.Find(".pie-de-trazabilidad")));
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "41")]
    public void Formatos_DeFechaHoraYPersona()
    {
        var zona = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Cordoba");
        var instante = new DateTimeOffset(2026, 11, 16, 8, 5, 0, TimeSpan.Zero);

        Assert.Equal("16/11/2026", Formatos.Fecha(instante, zona));
        Assert.Equal("05:05", Formatos.Hora(instante, zona));
        Assert.Equal("16/11/2026", Formatos.Fecha(new DateOnly(2026, 11, 16)));
        Assert.Equal("Ríos, Pablo", Formatos.Persona("Ríos", "Pablo"));
    }
}
