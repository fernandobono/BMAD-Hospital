using Bunit;
using Bunit.TestDoubles;
using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Catalogo;
using HospitalTurnos.Aplicacion.Comun;
using HospitalTurnos.Dominio.Auditoria;
using HospitalTurnos.Dominio.Excepciones;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Infraestructura.Persistencia;
using HospitalTurnos.Web.Components.Comunes;
using HospitalTurnos.Web.Components.Personal.Administracion;
using HospitalTurnos.Web.Personal;
using HospitalTurnos.Web.Tests.Ayudas;
using HospitalTurnos.Web.Tests.Personal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalTurnos.Web.Tests.Catalogo;

// A2 con el estándar de la guía (story 3.3, sección G) como componente interactivo, contra los casos de uso reales
// de la 3.1 (Testcontainers). Reemplaza a las pruebas HTTP de los formularios SSR (AC 92): cada comportamiento de la
// 3.1 sigue probado acá, con su marca. La base es compartida: cada prueba usa nombres con su propia marca.
[Collection("Postgres")]
public class EspecialidadesInteractivaTests(PostgresFixture fixture) : BunitContext
{
    private const string A2 = "/personal/admin/especialidades";

    private static string Marca() => Guid.NewGuid().ToString("N")[..8];

    private static string Texto(AngleSharp.Dom.IElement e) => System.Text.RegularExpressions.Regex.Replace(e.TextContent, @"\s+", " ").Trim();

    private async Task<IRenderedComponent<Especialidades>> AbrirAsync(string? consulta = null)
    {
        if (consulta is not null)
            Services.GetRequiredService<BunitNavigationManager>().NavigateTo($"{A2}?{consulta}");
        var pagina = Render<Especialidades>();
        await pagina.WaitForAssertionAsync(() => Assert.Empty(pagina.FindAll("tbody tr.esqueleto")));
        return pagina;
    }

    // Los títulos de las columnas, sin el indicador de orden (▲▼), que es decorativo.
    private static IEnumerable<string> Encabezados(IRenderedComponent<Especialidades> pagina) =>
        pagina.FindAll("table.tabla-abm thead th").Select(th => Texto(th).Replace("▲", "").Replace("▼", "").Trim());

    private static AngleSharp.Dom.IElement FilaDe(IRenderedComponent<Especialidades> pagina, string nombre) =>
        pagina.FindAll("table.tabla-abm tbody tr").Single(f => f.QuerySelector("td")!.TextContent.Trim() == nombre);

    private static async Task ElegirAsync(IRenderedComponent<Especialidades> pagina, string nombre, string opcion)
    {
        // La tabla se recarga sola después de una acción: si el clic cae en la recarga, se vuelve a abrir el menú.
        await pagina.WaitForAssertionAsync(() => Assert.NotNull(FilaDe(pagina, nombre)));
        await pagina.InvokeAsync(() => FilaDe(pagina, nombre).QuerySelector(".menu-de-acciones__boton")!
            .TriggerEventAsync("onclick", new Microsoft.AspNetCore.Components.Web.MouseEventArgs()));
        await pagina.WaitForAssertionAsync(() => Assert.Contains(pagina.FindAll("[role=menuitem]"), o => Texto(o) == opcion));
        await pagina.PulsarAsync("[role=menuitem]", o => Texto(o) == opcion);
    }

    private static async Task EscribirAsync(IRenderedComponent<Especialidades> pagina, string nombre)
    {
        await pagina.CambiarAsync("[role=dialog] input#nombre", nombre);
        await pagina.EnviarAsync("[role=dialog] form");
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "47")]
    [Trait("AC", "48")]
    [Trait("AC", "84")]
    [Trait("Story", "3.1")]
    [Trait("AC", "5")]
    [Trait("AC", "6")]
    [Trait("AC", "8")]
    public async Task Especialidades_AltaEnModal_ConLasValidacionesDeHoy()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        using var alcance = this.ConLaApp(app);
        var actor = await this.ComoAdmin1Async(app);
        var marca = Marca();
        var catalogo = alcance.ServiceProvider.GetRequiredService<ICatalogoEspecialidades>();
        await catalogo.CrearAsync(actor, $"Repetida {marca}", CancellationToken.None);
        var desactivada = await catalogo.CrearAsync(actor, $"Apagada {marca}", CancellationToken.None);
        await catalogo.DesactivarAsync(actor, desactivada, CancellationToken.None);

        var pagina = await AbrirAsync();

        Assert.Matches(@"^Especialidades · \d+$", Texto((await pagina.WaitForElementAsync(".page-header h1"))));
        Assert.Equal("Nueva especialidad", Texto(Assert.Single(pagina.FindAll(".page-header .btn-primary"))));
        Assert.Empty(pagina.FindAll("form"));   // ningún formulario fijo arriba de la tabla (B-2.2)
        await pagina.PulsarAsync(".page-header .btn-primary");
        var dialogo = (await pagina.WaitForElementAsync("[role=dialog]"));
        Assert.Equal("Nueva especialidad", Texto(dialogo.QuerySelector(".dialogo__titulo")!));
        Assert.Equal("Nombre *", Texto(dialogo.QuerySelector("label[for=nombre]")!));
        Assert.Equal(["Guardar", "Cancelar"], dialogo.QuerySelectorAll(".formulario-abm__botones button").Select(Texto));

        await EscribirAsync(pagina, "   ");
        Assert.Contains("Ingresá el nombre de la especialidad.", (await pagina.WaitForElementAsync("#nombre-error")).TextContent);
        await EscribirAsync(pagina, new string('x', 81));
        Assert.Contains("El nombre de la especialidad tiene como máximo 80 caracteres.", (await pagina.WaitForElementAsync("#nombre-error")).TextContent);
        await EscribirAsync(pagina, $"repetida {marca}");
        await pagina.WaitForAssertionAsync(() => Assert.Equal($"Ya existe la especialidad Repetida {marca}.", Texto(pagina.Find("[role=dialog] .resumen-de-errores"))));
        Assert.Equal($"repetida {marca}", (await pagina.WaitForElementAsync("[role=dialog] input#nombre")).GetAttribute("value"));   // conserva lo tipeado
        await EscribirAsync(pagina, $"Apagada {marca}");
        await pagina.WaitForAssertionAsync(() => Assert.Equal($"Ya existe la especialidad Apagada {marca}, desactivada. Reactivala desde la lista.",
            Texto(pagina.Find("[role=dialog] .resumen-de-errores"))));

        await EscribirAsync(pagina, $"  Nueva   {marca} ");
        await pagina.WaitForAssertionAsync(() => Assert.Empty(pagina.FindAll("[role=dialog]")));
        Assert.Contains("Especialidad creada", (await pagina.WaitForElementAsync(".zona-de-notificaciones")).TextContent);

        using var lectura = app.Services.CreateScope();
        var contexto = lectura.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var creada = await contexto.Especialidades.SingleAsync(e => e.Nombre == $"Nueva {marca}");
        var auditoria = await contexto.RegistrosAuditoria.SingleAsync(r => r.Accion == AccionesAuditoria.EspecialidadCreada && r.EntidadId == creada.Id.ToString());
        Assert.Equal(actor.UsuarioId, auditoria.UsuarioId);
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "49")]
    [Trait("AC", "50")]
    [Trait("AC", "55")]
    [Trait("Story", "3.1")]
    [Trait("AC", "15")]
    public async Task Especialidades_FiltrosColumnas_YEstadoSoloConTodas()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        using var alcance = this.ConLaApp(app);
        await this.ComoAdmin1Async(app);
        var marca = Marca();
        var activa = await app.CrearEspecialidadAsync(nombre: $"Fil {marca} Activa");
        await app.CrearEspecialidadAsync(activa: false, nombre: $"Fil {marca} Inactiva");
        for (var i = 0; i < 2; i++)
            await app.CrearMedicoAsync("Ana", $"Fil{marca}", $"T-{Marca().ToUpperInvariant()}", activa);

        var pagina = await AbrirAsync($"buscar={marca}");

        Assert.Equal("Buscar", Texto((await pagina.WaitForElementAsync("label[for=buscar]"))));
        Assert.Equal("Buscar por nombre", (await pagina.WaitForElementAsync("input#buscar")).GetAttribute("placeholder"));
        Assert.Equal(marca, (await pagina.WaitForElementAsync("input#buscar")).GetAttribute("value"));
        Assert.Equal(["Activas", "Inactivas", "Todas"], pagina.FindAll("select#estado option").Select(Texto));
        Assert.Equal(["Nombre", "Médicos asociados", "Acciones"], Encabezados(pagina));
        var fila = Assert.Single(pagina.FindAll("table.tabla-abm tbody tr"));
        Assert.Equal($"Fil {marca} Activa", Texto(fila.QuerySelector("td")!));
        Assert.Equal("2", Texto(fila.QuerySelector("td.numero")!));
        Assert.Equal("Especialidades · 1", Texto((await pagina.WaitForElementAsync(".page-header h1"))));
        Assert.Contains($"Búsqueda: {marca}", (await pagina.WaitForElementAsync(".filtros-activos")).TextContent);

        await pagina.CambiarAsync("select#estado", "todos");

        await pagina.WaitForAssertionAsync(() => Assert.Equal(2, pagina.FindAll("table.tabla-abm tbody tr:not(.esqueleto)").Count));
        Assert.EndsWith($"{A2}?buscar={marca}&estado=todos", Services.GetRequiredService<BunitNavigationManager>().Uri);
        Assert.Equal(["Nombre", "Médicos asociados", "Estado", "Acciones"], Encabezados(pagina));
        Assert.Equal(["Activa", "Inactiva"], pagina.FindAll("table.tabla-abm tbody .badge-estado").Select(Texto));
        Assert.Equal(2, pagina.FindAll(".tarjetas-abm .tarjeta-abm").Count);
    }

    // Doble: la regla de turnos futuros que trae la 5.3 rechaza la desactivación (AC 85).
    private sealed class CatalogoQueRechaza : ICatalogoEspecialidades
    {
        public List<string> Llamadas { get; } = [];

        public Task<IReadOnlyList<EspecialidadResumen>> ListarAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<EspecialidadResumen>>([]);
        public Task<IReadOnlyList<EspecialidadResumen>> ListarActivasAsync(CancellationToken ct) => ListarAsync(ct);
        public Task<EspecialidadResumen?> BuscarAsync(long id, CancellationToken ct) => Task.FromResult<EspecialidadResumen?>(null);

        public Task<PaginaDeResultados<EspecialidadFila>> ListarPaginadoAsync(ConsultaDeListado consulta, CancellationToken ct) =>
            Task.FromResult(new PaginaDeResultados<EspecialidadFila>([new(1, "Cardiología", true, 12)], 1));

        public Task<int> ContarMedicosAsync(long id, CancellationToken ct) => Task.FromResult(12);
        public Task<long> CrearAsync(ActorAuditoria actor, string nombre, CancellationToken ct) => throw new InvalidOperationException("No se usa.");

        public Task DesactivarAsync(ActorAuditoria actor, long id, CancellationToken ct)
        {
            Llamadas.Add("desactivar");
            throw new ConflictoException("No se puede desactivar: hay turnos futuros asignados.");
        }

        public Task ReactivarAsync(ActorAuditoria actor, long id, CancellationToken ct) => Task.CompletedTask;
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "51")]
    [Trait("AC", "52")]
    [Trait("AC", "53")]
    [Trait("AC", "85")]
    [Trait("Story", "3.1")]
    [Trait("AC", "9")]
    [Trait("AC", "11")]
    public async Task Especialidades_DesactivarYReactivar_ConfirmanConCantidad()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        using (var alcance = this.ConLaApp(app))
        {
            await this.ComoAdmin1Async(app);
            var marca = Marca();
            var cardiologia = await app.CrearEspecialidadAsync(nombre: $"Cardiología {marca}");
            var vacia = await app.CrearEspecialidadAsync(nombre: $"Sin médicos {marca}");
            var pediatria = await app.CrearEspecialidadAsync(activa: false, nombre: $"Pediatría {marca}");
            for (var i = 0; i < 12; i++)
                await app.CrearMedicoAsync("Ana", $"Car{marca}{i}", $"T-{Marca().ToUpperInvariant()}", cardiologia);

            var pagina = await AbrirAsync($"buscar={marca}&estado=todos");
            Assert.Empty(pagina.FindAll("table.tabla-abm tbody .btn-danger, table.tabla-abm tbody .btn-outline-danger"));
            Assert.Equal($"Más acciones para Cardiología {marca}",
                FilaDe(pagina, $"Cardiología {marca}").QuerySelector(".menu-de-acciones__boton")!.GetAttribute("aria-label"));

            await ElegirAsync(pagina, $"Sin médicos {marca}", "Desactivar");
            await pagina.WaitForAssertionAsync(() => Assert.Contains("Ningún médico la tiene asignada; no podrá elegirse en nuevos médicos.",
                pagina.Find("[role=dialog]").TextContent));
            await pagina.PulsarAsync("[role=dialog] button", b => Texto(b) == "Cancelar");
            await pagina.WaitForAssertionAsync(() => Assert.Empty(pagina.FindAll("[role=dialog]")));

            await ElegirAsync(pagina, $"Cardiología {marca}", "Desactivar");
            await pagina.WaitForAssertionAsync(() => Assert.Equal($"¿Desactivar Cardiología {marca}?", Texto(pagina.Find("[role=dialog] .dialogo__titulo"))));
            Assert.Contains("12 médicos la tienen asignada; no podrá elegirse en nuevos médicos.", (await pagina.WaitForElementAsync("[role=dialog]")).TextContent);
            Assert.Equal(["Cancelar", "Desactivar especialidad"], pagina.FindAll("[role=dialog] .dialogo__botones button").Select(Texto));
            await pagina.PulsarAsync("[role=dialog] button", b => Texto(b) == "Desactivar especialidad");
            await pagina.WaitForAssertionAsync(() => Assert.Contains("Especialidad desactivada", pagina.Find(".zona-de-notificaciones").TextContent));
            await pagina.WaitForAssertionAsync(() => Assert.Equal("Inactiva", Texto(FilaDe(pagina, $"Cardiología {marca}").QuerySelector(".badge-estado")!)));

            await ElegirAsync(pagina, $"Pediatría {marca}", "Reactivar");
            await pagina.WaitForAssertionAsync(() => Assert.Equal($"¿Reactivar Pediatría {marca}?", Texto(pagina.Find("[role=dialog] .dialogo__titulo"))));
            Assert.Contains("Vuelve a ofrecerse al dar de alta o editar un médico.", (await pagina.WaitForElementAsync("[role=dialog]")).TextContent);
            await pagina.PulsarAsync("[role=dialog] button", b => Texto(b) == "Reactivar especialidad");
            await pagina.WaitForAssertionAsync(() => Assert.Contains("Especialidad reactivada", pagina.Find(".zona-de-notificaciones").TextContent));

            using var lectura = app.Services.CreateScope();
            var contexto = lectura.ServiceProvider.GetRequiredService<HospitalDbContext>();
            Assert.False((await contexto.Especialidades.SingleAsync(e => e.Id == cardiologia)).Activa);
            Assert.True((await contexto.Especialidades.SingleAsync(e => e.Id == pediatria)).Activa);
            Assert.True((await contexto.Especialidades.SingleAsync(e => e.Id == vacia)).Activa);
        }
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "85")]
    [Trait("Story", "3.1")]
    [Trait("AC", "3")]
    public async Task Especialidades_RechazoDelDominio_PasaANoSePuede()
    {
        var catalogo = new CatalogoQueRechaza();
        Services.AddSingleton<ICatalogoEspecialidades>(catalogo);
        Services.AddSingleton<AlcancePorOperacion>();
        Services.AddSingleton(TimeProvider.System);
        Services.AddScoped<Notificaciones>();
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.Como(new ActorAuditoria(Guid.NewGuid().ToString(), "Lucía Pérez", Roles.Administrador, CanalAuditoria.Personal));

        var pagina = await AbrirAsync();
        await ElegirAsync(pagina, "Cardiología", "Desactivar");
        await pagina.PulsarAsync("[role=dialog] button", b => Texto(b) == "Desactivar especialidad");

        await pagina.WaitForAssertionAsync(() => Assert.Contains("No se puede desactivar: hay turnos futuros asignados.", pagina.Find("[role=dialog]").TextContent));
        Assert.Equal(["Entendido"], pagina.FindAll("[role=dialog] .dialogo__botones button").Select(Texto));
        Assert.Equal(["desactivar"], catalogo.Llamadas);
        Assert.Empty(pagina.FindAll(".zona-de-notificaciones .notificacion"));
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "54")]
    public async Task Especialidades_VacioYSinResultados()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        using var alcance = this.ConLaApp(app);
        await this.ComoAdmin1Async(app);
        var buscado = $"zzz{Marca()}";

        var pagina = await AbrirAsync($"buscar={buscado}");

        Assert.Equal($"No hay especialidades que coincidan con '{buscado}'", Texto((await pagina.WaitForElementAsync(".estado-vacio p"))));
        await pagina.PulsarAsync(".estado-vacio button", b => Texto(b) == "Limpiar filtros");
        Assert.EndsWith(A2, Services.GetRequiredService<BunitNavigationManager>().Uri);
    }
}
