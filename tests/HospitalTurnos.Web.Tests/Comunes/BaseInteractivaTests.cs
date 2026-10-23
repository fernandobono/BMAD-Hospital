using System.Reflection;
using System.Security.Claims;
using Bunit;
using Bunit.TestDoubles;
using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Catalogo;
using HospitalTurnos.Aplicacion.Comun;
using HospitalTurnos.Aplicacion.Personal;
using HospitalTurnos.Dominio.Auditoria;
using HospitalTurnos.Dominio.Excepciones;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Infraestructura.Persistencia;
using HospitalTurnos.Web.Components.Comunes;
using HospitalTurnos.Web.Components.Personal;
using HospitalTurnos.Web.Components.Personal.Administracion;
using HospitalTurnos.Web.Personal;
using HospitalTurnos.Web.Tests.Ayudas;
using HospitalTurnos.Web.Tests.Catalogo;
using HospitalTurnos.Web.Tests.Personal;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HospitalTurnos.Web.Tests.Comunes;

// Story 3.3, sección A (ADR-08): las ABM pasan a ser interactivas en el servidor sin prerender; el ingreso, el
// cambio de contraseña y Salir siguen estáticos porque escriben la cookie en la respuesta HTTP (2.1).
[Collection("Postgres")]
public class BaseInteractivaTests(PostgresFixture fixture) : BunitContext
{
    public static TheoryData<Type, bool> Paginas => new()
    {
        { typeof(Especialidades), true },
        { typeof(Medicos), true },
        { typeof(MedicoNuevo), true },
        { typeof(FichaMedico), true },
        { typeof(UsuariosPersonal), true },
        { typeof(Ingreso), false },
        { typeof(CambiarContrasena), false },
    };

    [Theory]
    [Trait("Story", "3.3")]
    [Trait("AC", "1")]
    [MemberData(nameof(Paginas))]
    public void PaginasAbm_DeclaranInteractivoSinPrerender_YLasDeCookieNo(Type pagina, bool interactiva)
    {
        var modo = pagina.GetCustomAttributes<RenderModeAttribute>(inherit: true).SingleOrDefault()?.Mode;

        if (interactiva)
        {
            var servidor = Assert.IsType<InteractiveServerRenderMode>(modo);
            Assert.False(servidor.Prerender);
        }
        else
        {
            Assert.Null(modo);
        }
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "2")]
    public async Task AccionEnCircuito_AuditaConElActorDelEstadoDeAutenticacion()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        using var alcance = this.ConLaApp(app);
        var actor = await this.ComoAdmin1Async(app);
        var nombre = $"Circuito{Guid.NewGuid().ToString("N")[..8]}";

        var pagina = Render<Especialidades>();
        await pagina.WaitForAssertionAsync(() =>
        {
            Assert.NotNull(pagina.Find(".page-header .btn-primary"));
            Assert.Empty(pagina.FindAll("tbody tr.esqueleto"));
        });
        await pagina.PulsarAsync(".page-header .btn-primary");
        await pagina.CambiarAsync("[role=dialog] input", nombre);
        await pagina.EnviarAsync("[role=dialog] form");
        await pagina.WaitForAssertionAsync(() => Assert.Contains("Especialidad creada", pagina.Markup));

        using var lectura = app.Services.CreateScope();
        var contexto = lectura.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var id = await contexto.Especialidades.Where(e => e.Nombre == nombre).Select(e => e.Id).SingleAsync();
        var fila = await contexto.RegistrosAuditoria
            .SingleAsync(r => r.Accion == AccionesAuditoria.EspecialidadCreada && r.EntidadId == id.ToString());
        Assert.Equal(actor.UsuarioId, fila.UsuarioId);
        Assert.Equal(actor.Quien, fila.Quien);
        Assert.Equal(Roles.Administrador, fila.Rol);
        Assert.Equal(CanalAuditoria.Personal, fila.Canal);
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "3")]
    public async Task ErrorDelDominio_SeMuestraEnLaPagina_ConElMismoTexto()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        using var alcance = this.ConLaApp(app);
        var actor = await this.ComoAdmin1Async(app);
        var nombre = $"Repetida{Guid.NewGuid().ToString("N")[..8]}";
        await alcance.ServiceProvider.GetRequiredService<ICatalogoEspecialidades>().CrearAsync(actor, nombre, CancellationToken.None);

        var pagina = Render<Especialidades>();
        await pagina.WaitForAssertionAsync(() =>
        {
            Assert.NotNull(pagina.Find(".page-header .btn-primary"));
            Assert.Empty(pagina.FindAll("tbody tr.esqueleto"));
        });
        await pagina.PulsarAsync(".page-header .btn-primary");
        await pagina.CambiarAsync("[role=dialog] input", nombre);
        await pagina.EnviarAsync("[role=dialog] form");

        await pagina.WaitForAssertionAsync(() =>
            Assert.Contains($"Ya existe la especialidad {nombre}.", pagina.Find("[role=dialog] .resumen-de-errores").TextContent));
        Assert.Equal(nombre, (await pagina.WaitForElementAsync("[role=dialog] input")).GetAttribute("value"));
    }

    // Doble: el catálogo que falla de forma inesperada al crear (no es una regla del dominio).
    private sealed class CatalogoQueFalla : ICatalogoEspecialidades
    {
        public Task<IReadOnlyList<EspecialidadResumen>> ListarAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<EspecialidadResumen>>([]);

        public Task<IReadOnlyList<EspecialidadResumen>> ListarActivasAsync(CancellationToken ct) => ListarAsync(ct);

        public Task<EspecialidadResumen?> BuscarAsync(long id, CancellationToken ct) => Task.FromResult<EspecialidadResumen?>(null);

        public Task<PaginaDeResultados<EspecialidadFila>> ListarPaginadoAsync(ConsultaDeListado consulta, CancellationToken ct) =>
            Task.FromResult(new PaginaDeResultados<EspecialidadFila>([], 0));

        public Task<int> ContarMedicosAsync(long id, CancellationToken ct) => Task.FromResult(0);

        public Task<long> CrearAsync(ActorAuditoria actor, string nombre, CancellationToken ct) =>
            throw new InvalidOperationException("Conexión rota en la sonda");

        public Task DesactivarAsync(ActorAuditoria actor, long id, CancellationToken ct) => Task.CompletedTask;

        public Task ReactivarAsync(ActorAuditoria actor, long id, CancellationToken ct) => Task.CompletedTask;
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "4")]
    public async Task ErrorInesperado_MuestraElMensajeGenerico_YRegistraElDetalle()
    {
        var registro = new RegistroEnMemoria<LimiteDeErrores>();
        Services.AddSingleton<ILogger<LimiteDeErrores>>(registro);
        Services.AddSingleton<ICatalogoEspecialidades, CatalogoQueFalla>();
        Services.AddSingleton<AlcancePorOperacion>();
        Services.AddScoped<Notificaciones>();
        Services.AddSingleton(TimeProvider.System);
        JSInterop.Mode = JSRuntimeMode.Loose;
        Como(new ActorAuditoria(Guid.NewGuid().ToString(), "Lucía Pérez", Roles.Administrador, CanalAuditoria.Personal));

        var pagina = Render<Especialidades>();
        await pagina.WaitForAssertionAsync(() =>
        {
            Assert.NotNull(pagina.Find(".page-header .btn-primary"));
            Assert.Empty(pagina.FindAll("tbody tr.esqueleto"));
        });
        await pagina.PulsarAsync(".page-header .btn-primary");
        await pagina.CambiarAsync("[role=dialog] input", "Neumonología");
        await pagina.EnviarAsync("[role=dialog] form");

        await pagina.WaitForAssertionAsync(() => Assert.Contains("Algo falló. Probá de nuevo en unos minutos.", pagina.Markup));
        Assert.DoesNotContain("Conexión rota", pagina.Markup);
        var entrada = Assert.Single(registro.Entradas, e => e.Nivel == LogLevel.Error);
        Assert.Equal("Conexión rota en la sonda", entrada.Excepcion!.Message);
    }

    private void Como(ActorAuditoria actor) => ContextoDeComponente.Como(this, actor);

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "94")]
    public async Task OperacionesDelCircuito_UsanUnAlcanceCadaUna_YUnRechazoNoSeCuela()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var operaciones = new AlcancePorOperacion(app.Services.GetRequiredService<IServiceScopeFactory>());
        var actor = await app.ActorDeAdmin1Async();
        var marca = Guid.NewGuid().ToString("N")[..8];
        var especialidad = await app.CrearEspecialidadAsync();
        var desactivada = await app.CrearEspecialidadAsync(activa: false);
        var ana = await app.CrearMedicoAsync("Ana", $"Alc{marca}A", $"T-A{marca}", especialidad);
        var bruno = await app.CrearMedicoAsync("Bruno", $"Alc{marca}B", $"T-B{marca}", especialidad);

        // 1) La tabla y un filtro a la vez: cada una con su contexto de la base.
        HospitalDbContext? primero = null, segundo = null;
        var segundoEmpezo = new TaskCompletionSource();
        var una = operaciones.EjecutarAsync<HospitalDbContext>(async c => { primero = c; await segundoEmpezo.Task; });
        var otra = operaciones.EjecutarAsync<HospitalDbContext>(c => { segundo = c; segundoEmpezo.SetResult(); return Task.CompletedTask; });
        await Task.WhenAll(una, otra);
        Assert.NotNull(primero);
        Assert.NotSame(primero, segundo);

        // 2) El dominio rechaza la edición de Ana (especialidad desactivada) después de cambiarla en memoria; la edición
        //    siguiente, de Bruno, no la arrastra: el contexto del rechazo ya se descartó.
        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => operaciones.EjecutarAsync<IGestionMedicos>(g =>
            g.EditarAsync(ana, new DatosMedico("Cambiada", $"Alc{marca}A", $"T-A{marca}", desactivada, null), actor, CancellationToken.None)));
        await operaciones.EjecutarAsync<IGestionMedicos>(g =>
            g.EditarAsync(bruno, new DatosMedico("Bruno José", $"Alc{marca}B", $"T-B{marca}", especialidad, null), actor, CancellationToken.None));

        using var lectura = app.Services.CreateScope();
        var contexto = lectura.ServiceProvider.GetRequiredService<HospitalDbContext>();
        Assert.Equal("Ana", (await contexto.Medicos.SingleAsync(m => m.Id == ana)).Nombre);
        Assert.Equal("Bruno José", (await contexto.Medicos.SingleAsync(m => m.Id == bruno)).Nombre);
    }

    // Doble: la sesión ya no vale (usuario desactivado o sello cambiado).
    private sealed class IngresoConSesionVencida : IIngresoPersonal
    {
        public List<(string Id, string? Sello)> Consultas { get; } = [];

        public Task<ResultadoIngreso> ValidarAsync(string usuario, string contrasena, CancellationToken ct) =>
            throw new InvalidOperationException("No se usa en esta prueba.");

        public Task<bool> SesionVigenteAsync(string usuarioId, string? selloDeSeguridad, CancellationToken ct)
        {
            Consultas.Add((usuarioId, selloDeSeguridad));
            return Task.FromResult(false);
        }
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "5")]
    public async Task CircuitoConUsuarioDesactivado_NavegaAlIngreso()
    {
        // 1) El proveedor del circuito pregunta a la base con el id y el sello de la sesión, cada 5 minutos.
        var ingreso = new IngresoConSesionVencida();
        var servicios = new ServiceCollection().AddScoped<IIngresoPersonal>(_ => ingreso).BuildServiceProvider();
        var opciones = Options.Create(new IdentityOptions());
        var revalidacion = new RevalidacionDeCircuito(NullLoggerFactory.Instance,
            servicios.GetRequiredService<IServiceScopeFactory>(), opciones);
        var usuario = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "u-1"),
            new Claim(opciones.Value.ClaimsIdentity.SecurityStampClaimType, "sello-viejo"),
        ], "Personal"));

        var intervalo = (TimeSpan)typeof(RevalidacionDeCircuito)
            .GetProperty("RevalidationInterval", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(revalidacion)!;
        var vigente = await (Task<bool>)typeof(RevalidacionDeCircuito)
            .GetMethod("ValidateAuthenticationStateAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(revalidacion, [new AuthenticationState(usuario), CancellationToken.None])!;

        Assert.Equal(SesionPersonal.IntervaloRevalidacion, intervalo);
        Assert.False(vigente);
        Assert.Equal([("u-1", "sello-viejo")], ingreso.Consultas);

        // 2) Cuando el estado del circuito pasa a anónimo, la página va al ingreso con recarga completa.
        Services.AddSingleton(TimeProvider.System);
        Services.AddScoped<Notificaciones>();
        JSInterop.Mode = JSRuntimeMode.Loose;
        var autorizacion = AddAuthorization();
        autorizacion.SetAuthorized("Lucía Pérez");
        var pagina = Render<PaginaAbm>(p => p
            .Add(x => x.Titulo, "Especialidades")
            .Add(x => x.Migas, [new Miga("Catálogos", null), new Miga("Especialidades", null)]));

        autorizacion.SetNotAuthorized();

        var navegacion = Services.GetRequiredService<BunitNavigationManager>();
        await pagina.WaitForAssertionAsync(() =>
        {
            Assert.EndsWith(RutasPersonal.Ingreso, navegacion.Uri);
            Assert.True(navegacion.History.First().Options.ForceLoad);
        });
    }
}
