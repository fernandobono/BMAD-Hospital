using Bunit;
using Bunit.TestDoubles;
using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Catalogo;
using HospitalTurnos.Aplicacion.Comun;
using HospitalTurnos.Dominio.Auditoria;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Infraestructura.Persistencia;
using HospitalTurnos.Web.Components.Comunes;
using HospitalTurnos.Web.Components.Personal.Administracion;
using HospitalTurnos.Web.Personal;
using HospitalTurnos.Web.Tests.Ayudas;
using HospitalTurnos.Web.Tests.Personal;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalTurnos.Web.Tests.Catalogo;

// A1 con el estándar de la guía (story 3.3, secciones H e I) como componentes interactivos, contra los casos de uso
// reales de la 3.2 (Testcontainers). Reemplaza a las pruebas HTTP de los formularios SSR de la 3.2 (AC 92): cada
// comportamiento sigue probado acá, con su marca. Base compartida: cada prueba usa su propia marca.
[Collection("Postgres")]
public class MedicosInteractivaTests(PostgresFixture fixture) : BunitContext
{
    private static string Marca() => Guid.NewGuid().ToString("N")[..8];

    private static string Matricula() => $"MP {Random.Shared.Next(10_000, 99_999_999)}";

    private static string Texto(AngleSharp.Dom.IElement e) => System.Text.RegularExpressions.Regex.Replace(e.TextContent, @"\s+", " ").Trim();

    private BunitNavigationManager Navegacion => Services.GetRequiredService<BunitNavigationManager>();

    private async Task<IRenderedComponent<T>> AbrirAsync<T>(string url, Action<ComponentParameterCollectionBuilder<T>>? parametros = null)
        where T : IComponent
    {
        Navegacion.NavigateTo(url);
        var pagina = Render(parametros);
        await pagina.WaitForAssertionAsync(() => Assert.Empty(pagina.FindAll("tbody tr.esqueleto, .cargando")));
        return pagina;
    }

    private static async Task<long> MedicoConAuditoriaAsync(PersonalWebApplicationFactory app, ActorAuditoria actor, string nombre,
        string apellido, long especialidad, string? matricula = null)
    {
        using var alcance = app.Services.CreateScope();
        return await alcance.ServiceProvider.GetRequiredService<IGestionMedicos>()
            .CrearAsync(new DatosMedico(nombre, apellido, matricula ?? Matricula(), especialidad, null), actor, CancellationToken.None);
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "56")]
    [Trait("AC", "57")]
    [Trait("AC", "58")]
    [Trait("AC", "59")]
    [Trait("AC", "60")]
    [Trait("AC", "63")]
    [Trait("AC", "81")]
    [Trait("AC", "82")]
    [Trait("Story", "3.2")]
    [Trait("AC", "1")]
    [Trait("AC", "2")]
    [Trait("AC", "3")]
    public async Task Medicos_ListadoConFiltrosOrdenYPaginacion()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        using var alcance = this.ConLaApp(app);
        await this.ComoAdmin1Async(app);
        var marca = Marca();
        var especialidad = await app.CrearEspecialidadAsync(nombre: $"Lis {marca}");
        for (var i = 0; i < 30; i++)
            await app.CrearMedicoAsync($"Nombre{i:00}", $"Lis{marca}{i:00}", Matricula(), especialidad);

        var pagina = await AbrirAsync<Medicos>($"{RutasPersonal.Medicos}?especialidad={especialidad}");

        Assert.Equal("Médicos · 30", Texto((await pagina.WaitForElementAsync(".page-header h1"))));
        Assert.Equal("Nuevo médico", Texto(Assert.Single(pagina.FindAll(".page-header .btn-primary"))));
        Assert.Equal("Buscar", Texto((await pagina.WaitForElementAsync("label[for=buscar]"))));
        Assert.Equal("Buscar por apellido, nombre o matrícula", (await pagina.WaitForElementAsync("input#buscar")).GetAttribute("placeholder"));
        await pagina.WaitForAssertionAsync(() => Assert.Contains($"Lis {marca}", pagina.FindAll("select#especialidad option").Select(Texto)));
        Assert.Equal(["Todos", "Vigente", "Programada", "Pendiente de aprobación", "Sin agenda"],
            pagina.FindAll("select#agenda option").Select(Texto));
        Assert.Contains($"Especialidad: Lis {marca}", (await pagina.WaitForElementAsync(".filtros-activos")).TextContent);
        var ordenada = (await pagina.WaitForElementAsync("th[aria-sort]"));
        Assert.Equal("ascending", ordenada.GetAttribute("aria-sort"));
        Assert.StartsWith("Médico", Texto(ordenada));
        Assert.Equal($"Lis{marca}00, Nombre00", Texto((await pagina.WaitForElementAsync("table.tabla-abm tbody tr td"))));
        Assert.Equal("Mostrando 1–25 de 30", Texto((await pagina.WaitForElementAsync(".paginador__texto"))));

        await pagina.PulsarAsync(".paginador button", b => Texto(b) == "Siguiente");
        await pagina.WaitForAssertionAsync(() => Assert.Equal(5, pagina.FindAll("table.tabla-abm tbody tr:not(.esqueleto)").Count));
        Assert.EndsWith($"?especialidad={especialidad}&pagina=2", Navegacion.Uri);

        await pagina.PulsarAsync("th button", b => Texto(b).StartsWith("Matrícula"));
        await pagina.WaitForAssertionAsync(() => Assert.Contains("orden=matricula", Navegacion.Uri));
        Assert.DoesNotContain("pagina=", Navegacion.Uri);   // cambiar el orden vuelve a la primera página

        (await pagina.WaitForElementAsync("input#buscar")).Input("zzzz" + marca);
        (await pagina.WaitForElementAsync("input#buscar")).KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });
        await pagina.WaitForAssertionAsync(() => Assert.Equal($"No hay médicos que coincidan con 'zzzz{marca}'", Texto(pagina.Find(".estado-vacio p"))));
        Assert.NotNull(pagina.FindAll(".estado-vacio button").Single(b => Texto(b) == "Limpiar filtros"));

        var larga = new string('x', 101);
        (await pagina.WaitForElementAsync("input#buscar")).Input(larga);
        (await pagina.WaitForElementAsync("input#buscar")).KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });
        await pagina.WaitForAssertionAsync(() => Assert.Equal("⚠ La búsqueda tiene como máximo 100 caracteres.", Texto(pagina.Find("#buscar-error"))));
        await pagina.WaitForAssertionAsync(() => Assert.Equal("Médicos · 30", Texto(pagina.Find(".page-header h1"))));   // no filtró

        // AC 81: una página mayor que la última muestra la última con datos.
        var lejos = await AbrirAsync<Medicos>($"{RutasPersonal.Medicos}?especialidad={especialidad}&pagina=9");
        await lejos.WaitForAssertionAsync(() => Assert.Equal(5, lejos.FindAll("table.tabla-abm tbody tr:not(.esqueleto)").Count));
        Assert.Equal("Mostrando 26–30 de 30", Texto(lejos.Find(".paginador__texto")));
    }

    // Sin la app: los casos de uso son dobles (los vacíos no se ven en una base compartida).
    private void ConDobles(ICatalogoEspecialidades catalogo, IGestionMedicos? gestion = null)
    {
        Services.AddSingleton(catalogo);
        Services.AddSingleton(gestion ?? new GestionSinMedicos());
        Services.AddSingleton<AlcancePorOperacion>();
        Services.AddScoped<Notificaciones>();
        Services.AddSingleton(TimeProvider.System);
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.Como(new ActorAuditoria(Guid.NewGuid().ToString(), "Lucía Pérez", Roles.Administrador, CanalAuditoria.Personal));
    }

    // Dobles de la base caída: cada lectura lanza lo que lanzaría Npgsql (con un detalle que no debe verse).
    private sealed class CatalogoCaido : ICatalogoEspecialidades
    {
        private static Exception Caida() => new Npgsql.NpgsqlException("detalle secreto de la base");
        public Task<IReadOnlyList<EspecialidadResumen>> ListarAsync(CancellationToken ct) => throw Caida();
        public Task<IReadOnlyList<EspecialidadResumen>> ListarActivasAsync(CancellationToken ct) => throw Caida();
        public Task<EspecialidadResumen?> BuscarAsync(long id, CancellationToken ct) => throw Caida();
        public Task<PaginaDeResultados<EspecialidadFila>> ListarPaginadoAsync(ConsultaDeListado consulta, CancellationToken ct) => throw Caida();
        public Task<int> ContarMedicosAsync(long id, CancellationToken ct) => throw Caida();
        public Task<long> CrearAsync(ActorAuditoria actor, string nombre, CancellationToken ct) => throw Caida();
        public Task DesactivarAsync(ActorAuditoria actor, long id, CancellationToken ct) => throw Caida();
        public Task ReactivarAsync(ActorAuditoria actor, long id, CancellationToken ct) => throw Caida();
    }

    private sealed class GestionCaida : IGestionMedicos
    {
        private static Exception Caida() => new Npgsql.NpgsqlException("detalle secreto de la base");
        public Task<IReadOnlyList<MedicoListado>> ListarAsync(string? buscar, CancellationToken ct) => throw Caida();
        public Task<PaginaDeResultados<MedicoListado>> ListarPaginadoAsync(ConsultaDeListado consulta, CancellationToken ct) => throw Caida();
        public Task<DatosFichaMedico?> ObtenerAsync(long id, CancellationToken ct) => throw Caida();
        public Task<IReadOnlyList<OpcionUsuarioMedico>> UsuariosParaVincularAsync(long? medicoId, CancellationToken ct) => throw Caida();
        public Task<IReadOnlyDictionary<Guid, string>> MedicosVinculadosAsync(CancellationToken ct) => throw Caida();
        public Task<long> CrearAsync(DatosMedico datos, ActorAuditoria actor, CancellationToken ct) => throw Caida();
        public Task<bool> EditarAsync(long id, DatosMedico datos, ActorAuditoria actor, CancellationToken ct) => throw Caida();
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "83")]
    [Trait("AC", "4")]
    public async Task BaseCaida_LasPaginasDeMedicos_AvisanSinQuedarEnBlanco()
    {
        ConDobles(new CatalogoCaido(), new GestionCaida());

        // A1: el filtro de especialidades no se pudo cargar y la tabla avisa con "Reintentar" (AC 83).
        var listado = await AbrirAsync<Medicos>(RutasPersonal.Medicos);
        await listado.WaitForAssertionAsync(() => Assert.Equal("No pudimos cargar la lista.", Texto(listado.Find("[role=alert] p"))));
        Assert.Equal(["Todas"], listado.FindAll("select#especialidad option").Select(Texto));
        Assert.DoesNotContain("secreto", listado.Markup);

        // Alta y ficha: el mensaje genérico (AC 4), nunca una pantalla en blanco ni el detalle.
        var nuevo = Render<MedicoNuevo>();
        await nuevo.WaitForAssertionAsync(() => Assert.Contains("Algo falló. Probá de nuevo en unos minutos.", nuevo.Markup));
        var ficha = Render<FichaMedico>(p => p.Add(f => f.Id, 1L));
        await ficha.WaitForAssertionAsync(() => Assert.Contains("Algo falló. Probá de nuevo en unos minutos.", ficha.Markup));
        Assert.DoesNotContain("secreto", nuevo.Markup + ficha.Markup);
        Assert.NotEmpty(ficha.FindAll("nav.migas li"));
    }

    // Doble: un plantel sin médicos (el vacío inicial no se ve en una base compartida).
    private sealed class GestionSinMedicos : IGestionMedicos
    {
        public Task<IReadOnlyList<MedicoListado>> ListarAsync(string? buscar, CancellationToken ct) => Task.FromResult<IReadOnlyList<MedicoListado>>([]);
        public Task<PaginaDeResultados<MedicoListado>> ListarPaginadoAsync(ConsultaDeListado consulta, CancellationToken ct) =>
            Task.FromResult(new PaginaDeResultados<MedicoListado>([], 0));
        public Task<DatosFichaMedico?> ObtenerAsync(long id, CancellationToken ct) => Task.FromResult<DatosFichaMedico?>(null);
        public Task<IReadOnlyList<OpcionUsuarioMedico>> UsuariosParaVincularAsync(long? medicoId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<OpcionUsuarioMedico>>([]);
        public Task<IReadOnlyDictionary<Guid, string>> MedicosVinculadosAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());
        public Task<long> CrearAsync(DatosMedico datos, ActorAuditoria actor, CancellationToken ct) => throw new InvalidOperationException("No se usa.");
        public Task<bool> EditarAsync(long id, DatosMedico datos, ActorAuditoria actor, CancellationToken ct) => throw new InvalidOperationException("No se usa.");
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "63")]
    public async Task Medicos_SinMedicos_MuestraElVacioInicial()
    {
        ConDobles(new CatalogoSinActivas());

        var pagina = await AbrirAsync<Medicos>(RutasPersonal.Medicos);

        Assert.Equal("Todavía no hay médicos. Creá el primero.", Texto((await pagina.WaitForElementAsync(".estado-vacio p"))));
        await pagina.PulsarAsync(".estado-vacio button");
        Assert.EndsWith(RutasPersonal.NuevoMedico, Navegacion.Uri);
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "62")]
    [Trait("AC", "29")]
    public async Task Medicos_AccionesEditarYVincularUsuario()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        using var alcance = this.ConLaApp(app);
        await this.ComoAdmin1Async(app);
        var marca = Marca();
        var especialidad = await app.CrearEspecialidadAsync(nombre: $"Acc {marca}");
        var id = await app.CrearMedicoAsync("Pablo", $"Ríos{marca}", Matricula(), especialidad);

        var pagina = await AbrirAsync<Medicos>($"{RutasPersonal.Medicos}?especialidad={especialidad}");

        var acciones = (await pagina.WaitForElementAsync("table.tabla-abm tbody tr td.table-actions"));
        var editar = acciones.QuerySelector("a")!;
        Assert.Equal("Editar", Texto(editar));
        Assert.Equal($"{RutasPersonal.FichaMedico(id)}?modo=editar", editar.GetAttribute("href"));
        Assert.Equal($"Más acciones para Ríos{marca}, Pablo", acciones.QuerySelector(".menu-de-acciones__boton")!.GetAttribute("aria-label"));
        var tarjeta = (await pagina.WaitForElementAsync(".tarjetas-abm .tarjeta-abm"));
        Assert.Contains($"Ríos{marca}, Pablo", tarjeta.TextContent);
        Assert.NotNull(tarjeta.QuerySelector(".badge-estado"));
        Assert.NotNull(tarjeta.QuerySelector(".menu-de-acciones__boton"));

        // La carga del filtro de especialidades puede redibujar la tabla justo en el clic: se abre hasta que queda abierto.
        await pagina.PulsarAsync("table.tabla-abm tbody tr td.table-actions .menu-de-acciones__boton");
        await pagina.WaitForAssertionAsync(() => Assert.NotEmpty(pagina.FindAll("[role=menuitem]")));
        Assert.Equal(["Vincular usuario"], pagina.FindAll("[role=menuitem]").Select(Texto));   // ni "Ver agenda" ni "Desactivar"
        await pagina.PulsarAsync("[role=menuitem]");
        Assert.EndsWith($"{RutasPersonal.FichaMedico(id)}?modo=vincular", Navegacion.Uri);
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "67")]
    [Trait("AC", "70")]
    [Trait("AC", "71")]
    [Trait("AC", "19")]
    [Trait("Story", "3.2")]
    [Trait("AC", "5")]
    [Trait("AC", "6")]
    [Trait("AC", "7")]
    public async Task Alta_CamposAyudasYGuardado_PasaALaFichaConMedicoCreado()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        using var alcance = this.ConLaApp(app);
        await this.ComoAdmin1Async(app);
        var marca = Marca();
        var especialidad = await app.CrearEspecialidadAsync(nombre: $"Alta {marca}");

        var pagina = await AbrirAsync<MedicoNuevo>(RutasPersonal.NuevoMedico);

        Assert.Equal(["Nombre *", "Apellido *", "Matrícula *", "Especialidad *", "Usuario del sistema (opcional)"],
            pagina.FindAll(".campo__etiqueta").Select(Texto));
        Assert.Equal("Ejemplo: MP 1234", Texto((await pagina.WaitForElementAsync("#matricula-ayuda"))));
        Assert.Equal("Vinculalo para que el médico pueda ver y gestionar su agenda.", Texto((await pagina.WaitForElementAsync("#usuario-ayuda"))));
        Assert.Equal(["Guardar", "Cancelar"], pagina.FindAll(".formulario-abm__botones button").Select(Texto));
        Assert.DoesNotContain("Volver a Médicos", pagina.Markup);
        Assert.DoesNotContain("Dar de alta", pagina.Markup);

        await pagina.EnviarAsync("form");   // vacío: los mensajes de la 3.2, debajo de cada campo
        Assert.Contains("Ingresá el nombre.", (await pagina.WaitForElementAsync("#nombre-error")).TextContent);
        Assert.Contains("Ingresá la matrícula.", (await pagina.WaitForElementAsync("#matricula-error")).TextContent);
        Assert.Contains("Elegí una especialidad.", (await pagina.WaitForElementAsync("#especialidad-error")).TextContent);

        await pagina.CambiarAsync("input#nombre", "Pablo");
        await pagina.CambiarAsync("input#apellido", $"Alta{marca}");
        await pagina.CambiarAsync("input#matricula", "MP 1234!");
        Assert.Contains("La matrícula tiene hasta 20 caracteres", (await pagina.WaitForElementAsync("#matricula-error")).TextContent);
        var matricula = Matricula();
        await pagina.CambiarAsync("input#matricula", matricula);
        await pagina.CambiarAsync("select#especialidad", especialidad.ToString());
        await pagina.EnviarAsync("form");

        await pagina.WaitForAssertionAsync(() => Assert.Matches(@"/personal/admin/medicos/\d+$", Navegacion.Uri));
        Assert.Contains("Médico creado", Services.GetRequiredService<Notificaciones>().Visibles.Select(a => a.Texto));
        Assert.DoesNotContain("resultado=", Navegacion.Uri);
        using var lectura = app.Services.CreateScope();
        Assert.True(await lectura.ServiceProvider.GetRequiredService<HospitalDbContext>().Medicos.AnyAsync(m => m.Matricula == matricula));
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "36")]
    [Trait("AC", "70")]
    public async Task Alta_Cancelar_VuelveAlListado()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        using var alcance = this.ConLaApp(app);
        await this.ComoAdmin1Async(app);
        await app.CrearEspecialidadAsync();

        var pagina = await AbrirAsync<MedicoNuevo>(RutasPersonal.NuevoMedico);
        await pagina.PulsarAsync(".formulario-abm__botones button", b => Texto(b) == "Cancelar");

        Assert.EndsWith(RutasPersonal.Medicos, Navegacion.Uri);
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "64")]
    [Trait("AC", "65")]
    [Trait("AC", "66")]
    [Trait("AC", "68")]
    [Trait("AC", "70")]
    [Trait("AC", "88")]
    [Trait("Story", "3.2")]
    [Trait("AC", "8")]
    [Trait("AC", "15")]
    [Trait("AC", "16")]
    public async Task Ficha_LecturaEdicion_GuardarCancelar_YMensajes()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        using var alcance = this.ConLaApp(app);
        var actor = await this.ComoAdmin1Async(app);
        var marca = Marca();
        var especialidad = await app.CrearEspecialidadAsync(nombre: $"Ficha {marca}");
        var ocupada = Matricula();
        await MedicoConAuditoriaAsync(app, actor, "Otro", $"Otro{marca}", especialidad, ocupada);
        var id = await MedicoConAuditoriaAsync(app, actor, "Pablo", $"Ríos{marca}", especialidad);
        var medicoUsuario = await FormularioHttp.CrearUsuarioAsync(app, [Roles.Medico], "Clave-Medico-2026", nombre: $"Usuario {marca}");

        var ficha = await AbrirAsync<FichaMedico>(RutasPersonal.FichaMedico(id), p => p.Add(f => f.Id, id));

        // Lectura (B-4.1 a B-4.4)
        Assert.Equal($"Ríos{marca}, Pablo", Texto((await ficha.WaitForElementAsync(".page-header h1"))));
        Assert.Equal("Sin agenda", Texto((await ficha.WaitForElementAsync(".page-header .badge-estado"))));
        Assert.Equal(["Datos", "Usuario del sistema"], ficha.FindAll(".vista-de-detalle h2").Select(Texto));
        Assert.Empty(ficha.FindAll("input"));
        Assert.Matches(@"^Creado por Lucía Pérez el \d{2}/\d{2}/\d{4} \d{2}:\d{2}$", Texto((await ficha.WaitForElementAsync(".pie-de-trazabilidad"))));
        Assert.Equal(["Catálogos", "Médicos", $"Ríos{marca}, Pablo"], ficha.FindAll("nav.migas li").Select(Texto));

        // Editar y Cancelar sin cambios: vuelve a la lectura
        await ficha.PulsarAsync("button", b => Texto(b) == "Editar");
        Assert.Equal(["Guardar", "Cancelar"], ficha.FindAll(".formulario-abm__botones button").Select(Texto));
        await ficha.PulsarAsync(".formulario-abm__botones button", b => Texto(b) == "Cancelar");
        Assert.NotNull(ficha.FindAll("button").Single(b => Texto(b) == "Editar"));

        // Sin cambios: "No había cambios para guardar"
        await ficha.PulsarAsync("button", b => Texto(b) == "Editar");
        await ficha.EnviarAsync("form");
        await ficha.WaitForAssertionAsync(() => Assert.Contains("No había cambios para guardar", Services.GetRequiredService<Notificaciones>().Visibles.Select(a => a.Texto)));
        await ficha.WaitForAssertionAsync(() => Assert.Empty(ficha.FindAll("form")));

        // Matrícula de otro: el mensaje de hoy, debajo del campo, conservando lo cargado (B-4.12, B-4.13)
        await ficha.PulsarAsync("button", b => Texto(b) == "Editar");
        await ficha.CambiarAsync("input#matricula", ocupada.ToLowerInvariant());
        await ficha.EnviarAsync("form");
        await ficha.WaitForAssertionAsync(() => Assert.Contains($"Ya hay un médico con la matrícula {ocupada}.", ficha.Find("#matricula-error").TextContent));
        Assert.Contains("matricula-error", (await ficha.WaitForElementAsync("input#matricula")).GetAttribute("aria-describedby"));
        Assert.Equal(ocupada.ToLowerInvariant(), (await ficha.WaitForElementAsync("input#matricula")).GetAttribute("value"));

        // Otro administrador vinculó el mismo usuario a otro médico mientras se editaba (AC 88): al resumen
        await ficha.CambiarAsync("input#matricula", Matricula());
        await ficha.CambiarAsync("select#usuario", medicoUsuario.Id.ToString());
        using (var otro = app.Services.CreateScope())
        {
            var otroMedico = await otro.ServiceProvider.GetRequiredService<HospitalDbContext>().Medicos.SingleAsync(m => m.Apellido == $"Otro{marca}");
            await otro.ServiceProvider.GetRequiredService<IGestionMedicos>().EditarAsync(otroMedico.Id,
                new DatosMedico("Otro", $"Otro{marca}", ocupada, especialidad, medicoUsuario.Id), actor, CancellationToken.None);
        }
        await ficha.EnviarAsync("form");
        await ficha.WaitForAssertionAsync(() => Assert.Equal($"El usuario elegido ya está vinculado al médico Otro{marca}, Otro.",
            Texto(ficha.Find(".resumen-de-errores"))));

        // Con cambios válidos: "Médico actualizado", lectura y "Modificado por"
        await ficha.CambiarAsync("select#usuario", "");
        await ficha.CambiarAsync("input#nombre", "Pablo José");
        await ficha.EnviarAsync("form");
        await ficha.WaitForAssertionAsync(() => Assert.Contains("Médico actualizado", Services.GetRequiredService<Notificaciones>().Visibles.Select(a => a.Texto)));
        await ficha.WaitForAssertionAsync(() => Assert.Equal($"Ríos{marca}, Pablo José", Texto(ficha.Find(".page-header h1"))));
        Assert.Contains("· Modificado por Lucía Pérez el", Texto((await ficha.WaitForElementAsync(".pie-de-trazabilidad"))));
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "62")]
    public async Task Ficha_ModoVincular_AbreEnEdicionConElFocoEnUsuario()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        using var alcance = this.ConLaApp(app);
        await this.ComoAdmin1Async(app);
        var id = await app.CrearMedicoAsync("Pablo", $"Vin{Marca()}", Matricula(), await app.CrearEspecialidadAsync());

        var ficha = await AbrirAsync<FichaMedico>($"{RutasPersonal.FichaMedico(id)}?modo=vincular", p => p.Add(f => f.Id, id));

        Assert.NotNull((await ficha.WaitForElementAsync("form select#usuario")));
        // El foco se pide después de dibujar: se espera a que llegue, no se supone.
        await ficha.WaitForAssertionAsync(() => Assert.NotEmpty(JSInterop.Invocations["Blazor._internal.domWrapper.focus"]));
        JSInterop.Invocations["Blazor._internal.domWrapper.focus"][^1].Arguments[0].ShouldBeElementReferenceTo((await ficha.WaitForElementAsync("select#usuario")));
    }

    // Doble: un catálogo sin especialidades activas.
    private sealed class CatalogoSinActivas : ICatalogoEspecialidades
    {
        public Task<IReadOnlyList<EspecialidadResumen>> ListarAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<EspecialidadResumen>>([]);
        public Task<IReadOnlyList<EspecialidadResumen>> ListarActivasAsync(CancellationToken ct) => ListarAsync(ct);
        public Task<EspecialidadResumen?> BuscarAsync(long id, CancellationToken ct) => Task.FromResult<EspecialidadResumen?>(null);
        public Task<PaginaDeResultados<EspecialidadFila>> ListarPaginadoAsync(ConsultaDeListado consulta, CancellationToken ct) =>
            Task.FromResult(new PaginaDeResultados<EspecialidadFila>([], 0));
        public Task<int> ContarMedicosAsync(long id, CancellationToken ct) => Task.FromResult(0);
        public Task<long> CrearAsync(ActorAuditoria actor, string nombre, CancellationToken ct) => throw new InvalidOperationException("No se usa.");
        public Task DesactivarAsync(ActorAuditoria actor, long id, CancellationToken ct) => throw new InvalidOperationException("No se usa.");
        public Task ReactivarAsync(ActorAuditoria actor, long id, CancellationToken ct) => throw new InvalidOperationException("No se usa.");
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "86")]
    [Trait("AC", "87")]
    [Trait("Story", "3.2")]
    [Trait("AC", "10")]
    [Trait("AC", "18")]
    public async Task Nuevo_SinEspecialidades_YFichaInexistente_MuestranSuVacio()
    {
        ConDobles(new CatalogoSinActivas());

        var nuevo = await AbrirAsync<MedicoNuevo>(RutasPersonal.NuevoMedico);
        Assert.Equal("Primero creá una especialidad.", Texto((await nuevo.WaitForElementAsync(".estado-vacio p"))));
        Assert.Equal(RutasPersonal.Especialidades, nuevo.FindAll(".estado-vacio a").Single(a => Texto(a) == "Ir a Especialidades").GetAttribute("href"));
        Assert.Empty(nuevo.FindAll("form"));

        var ficha = await AbrirAsync<FichaMedico>(RutasPersonal.FichaMedico(-1), p => p.Add(f => f.Id, -1L));
        Assert.Contains("No existe el médico pedido.", (await ficha.WaitForElementAsync(".estado-vacio")).TextContent);
        Assert.Equal(RutasPersonal.Medicos, ficha.FindAll(".estado-vacio a").Single(a => Texto(a) == "Ir a Médicos").GetAttribute("href"));
        Assert.NotEmpty(ficha.FindAll("nav.migas li"));
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "69")]
    public async Task Alta_AvisaHomonimo_SinBloquear()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        using var alcance = this.ConLaApp(app);
        await this.ComoAdmin1Async(app);
        var marca = Marca();
        var especialidad = await app.CrearEspecialidadAsync(nombre: $"Hom {marca}");
        var existente = Matricula();
        await app.CrearMedicoAsync("Martín José", $"Pereyra{marca}", existente, especialidad);

        var pagina = await AbrirAsync<MedicoNuevo>(RutasPersonal.NuevoMedico);
        await pagina.CambiarAsync("input#nombre", "martin jose");
        await pagina.CambiarAsync("input#apellido", $"PEREYRA{marca}");

        // Con la MISMA matrícula no es un homónimo: no hay aviso y sale la regla del dominio debajo del campo.
        await pagina.CambiarAsync("input#matricula", existente.ToLowerInvariant());
        await pagina.CambiarAsync("select#especialidad", especialidad.ToString());
        await pagina.EnviarAsync("form");
        await pagina.WaitForAssertionAsync(() =>
            Assert.Contains($"Ya hay un médico con la matrícula {existente}.", pagina.Find("#matricula-error").TextContent));
        Assert.Empty(pagina.FindAll("[role=dialog]"));

        var nueva = Matricula();
        await pagina.CambiarAsync("input#matricula", nueva);
        await pagina.CambiarAsync("select#especialidad", especialidad.ToString());
        await pagina.EnviarAsync("form");

        await pagina.WaitForAssertionAsync(() => Assert.Equal($"Ya hay un médico Pereyra{marca}, Martín José ({existente}). ¿Es otra persona?",
            Texto(pagina.Find("[role=dialog] .dialogo__titulo"))));
        Assert.Equal(["Cancelar", "Guardar igual"], pagina.FindAll("[role=dialog] .dialogo__botones button").Select(Texto));
        using (var lectura = app.Services.CreateScope())
            Assert.False(await lectura.ServiceProvider.GetRequiredService<HospitalDbContext>().Medicos.AnyAsync(m => m.Matricula == nueva));

        await pagina.PulsarAsync("[role=dialog] button", b => Texto(b) == "Guardar igual");

        await pagina.WaitForAssertionAsync(() => Assert.Matches(@"/personal/admin/medicos/\d+$", Navegacion.Uri));
        using var despues = app.Services.CreateScope();
        Assert.True(await despues.ServiceProvider.GetRequiredService<HospitalDbContext>().Medicos.AnyAsync(m => m.Matricula == nueva));
    }
}
