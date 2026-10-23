using Bunit;
using Bunit.TestDoubles;
using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Catalogo;
using HospitalTurnos.Aplicacion.Comun;
using HospitalTurnos.Aplicacion.Personal;
using HospitalTurnos.Dominio.Auditoria;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Web.Components.Comunes;
using HospitalTurnos.Web.Components.Personal.Administracion;
using HospitalTurnos.Web.Personal;
using HospitalTurnos.Web.Tests.Ayudas;
using HospitalTurnos.Web.Tests.Catalogo;
using HospitalTurnos.Web.Tests.Personal.Dobles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using static HospitalTurnos.Web.Tests.Personal.FormularioHttp;

namespace HospitalTurnos.Web.Tests.Personal;

// A4 con el estándar de la guía (story 3.3, sección J; decisión D4) como componente interactivo, contra los casos de
// uso reales de la 2.2 y la 2.3 (Testcontainers). Reemplaza a las pruebas HTTP de los formularios SSR (AC 92): cada
// comportamiento sigue probado acá, con su marca. admin1 actúa sobre usuarios propios de cada prueba.
[Collection("Postgres")]
public class UsuariosInteractivaTests(PostgresFixture fixture) : BunitContext
{
    private const string A4 = "/personal/admin/usuarios";
    private const string Clave = "Clave-Propia-26";

    private static string Marca() => Guid.NewGuid().ToString("N")[..8];

    private static string Texto(AngleSharp.Dom.IElement e) => System.Text.RegularExpressions.Regex.Replace(e.TextContent, @"\s+", " ").Trim();

    private async Task<IRenderedComponent<UsuariosPersonal>> AbrirAsync(string? consulta = null)
    {
        if (consulta is not null)
            Services.GetRequiredService<BunitNavigationManager>().NavigateTo($"{A4}?{consulta}");
        var pagina = Render<UsuariosPersonal>();
        await pagina.WaitForAssertionAsync(() => Assert.Empty(pagina.FindAll("tbody tr.esqueleto")));
        return pagina;
    }

    private static AngleSharp.Dom.IElement FilaDe(IRenderedComponent<UsuariosPersonal> pagina, string usuario) =>
        pagina.FindAll("table.tabla-abm tbody tr").Single(f => f.QuerySelectorAll("td")[1].TextContent.Trim() == usuario);

    private static async Task<IReadOnlyList<string>> OpcionesDeAsync(IRenderedComponent<UsuariosPersonal> pagina, string usuario)
    {
        await pagina.WaitForAssertionAsync(() => Assert.NotNull(FilaDe(pagina, usuario)));
        await pagina.InvokeAsync(() => FilaDe(pagina, usuario).QuerySelector(".menu-de-acciones__boton")!
            .TriggerEventAsync("onclick", new Microsoft.AspNetCore.Components.Web.MouseEventArgs()));
        await pagina.WaitForAssertionAsync(() => Assert.NotEmpty(pagina.FindAll("[role=menuitem]")));
        return [.. pagina.FindAll("[role=menuitem]").Select(Texto)];
    }

    private static async Task ElegirAsync(IRenderedComponent<UsuariosPersonal> pagina, string usuario, string opcion)
    {
        await OpcionesDeAsync(pagina, usuario);
        await pagina.PulsarAsync("[role=menuitem]", o => Texto(o) == opcion);
    }

    private async Task<IReadOnlyList<string>> AvisosAsync(IRenderedComponent<UsuariosPersonal> pagina, string esperado)
    {
        await pagina.WaitForAssertionAsync(() =>
            Assert.Contains(esperado, Services.GetRequiredService<Notificaciones>().Visibles.Select(a => a.Texto)));
        return [.. Services.GetRequiredService<Notificaciones>().Visibles.Select(a => a.Texto)];
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "72")]
    [Trait("AC", "73")]
    [Trait("AC", "74")]
    [Trait("AC", "75")]
    [Trait("AC", "76")]
    [Trait("AC", "77")]
    [Trait("AC", "78")]
    [Trait("AC", "79")]
    [Trait("Story", "2.2")]
    [Trait("AC", "1")]
    [Trait("AC", "8")]
    [Trait("AC", "11")]
    [Trait("AC", "13")]
    [Trait("Story", "2.3")]
    [Trait("AC", "1")]
    [Trait("AC", "2")]
    [Trait("Story", "3.2")]
    [Trait("AC", "14")]
    public async Task Usuarios_ModalBadgesMenuConfirmaciones_YContrasenaEnDialogo()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        using var alcance = this.ConLaApp(app);
        var actor = await this.ComoAdmin1Async(app);
        var marca = Marca();
        var ana = await CrearUsuarioAsync(app, [Roles.Recepcionista], Clave, nombre: $"Ana {marca}");
        var bruno = await CrearUsuarioAsync(app, [Roles.Medico], Clave, activo: false, nombre: $"Bruno {marca}");
        var carla = await CrearUsuarioAsync(app, [Roles.Recepcionista], Clave, nombre: $"Carla {marca}");
        await FallarAsync(app, carla.Id, 5);
        var gestion = alcance.ServiceProvider.GetRequiredService<IGestionUsuariosPersonal>();
        var diego = await gestion.CrearAsync(new NuevoUsuarioPersonal($"Diego {marca}", $"d-{marca}", Roles.Recepcionista), actor, CancellationToken.None);
        var elena = await CrearUsuarioAsync(app, [Roles.Medico], Clave, nombre: $"Elena {marca}");
        var fabio = await CrearUsuarioAsync(app, [Roles.Medico], Clave, nombre: $"Fabio {marca}");
        await app.CrearMedicoAsync("Ana", $"Vin{marca}", $"T-{marca}", await app.CrearEspecialidadAsync(), elena.Id);

        var pagina = await AbrirAsync($"buscar={marca}&estado=todos");

        // Encabezado, filtros y columnas (B-4bis.1 a B-4bis.3)
        Assert.Equal("Usuarios del personal · 6", Texto((await pagina.WaitForElementAsync(".page-header h1"))));
        Assert.Equal("Nuevo usuario", Texto(Assert.Single(pagina.FindAll(".page-header .btn-primary"))));
        Assert.Equal("Buscar por nombre o usuario", (await pagina.WaitForElementAsync("input#buscar")).GetAttribute("placeholder"));
        Assert.Equal(["Todos", "Recepcionista", "Médico", "Administrador"], pagina.FindAll("select#rol option").Select(Texto));
        Assert.Equal(["Activos", "Inactivos", "Todos"], pagina.FindAll("select#estado option").Select(Texto));
        Assert.Equal(["Nombre", "Usuario", "Rol", "Médico vinculado", "Estado", "Acciones"],
            pagina.FindAll("table.tabla-abm thead th").Select(th => Texto(th).Replace("▲", "").Replace("▼", "").Trim()));

        // Badges (B-4bis.4) y médico vinculado (3.2, AC 14)
        string[] Insignias(string usuario) => [.. FilaDe(pagina, usuario).QuerySelectorAll(".badge-estado").Select(Texto)];
        Assert.Equal(["Activo"], Insignias(ana.UserName!));
        Assert.Equal(["Inactivo"], Insignias(bruno.UserName!));
        Assert.Equal("Activo", Insignias(carla.UserName!)[0]);
        Assert.Matches(@"^Bloqueado hasta \d{2}:\d{2}$", Insignias(carla.UserName!)[1]);
        Assert.Equal(["Activo", "Contraseña temporal"], Insignias(diego.Usuario));
        Assert.Contains("badge-estado--pendiente", FilaDe(pagina, carla.UserName!).QuerySelectorAll(".badge-estado")[1].ClassList);
        Assert.Contains("badge-estado--info", FilaDe(pagina, diego.Usuario).QuerySelectorAll(".badge-estado")[1].ClassList);
        Assert.Equal($"Vin{marca}, Ana", Texto(FilaDe(pagina, elena.UserName!).QuerySelectorAll("td")[3]));
        Assert.Equal("Sin vincular", Texto(FilaDe(pagina, fabio.UserName!).QuerySelectorAll("td")[3]));
        Assert.Equal("Médico", Texto(FilaDe(pagina, fabio.UserName!).QuerySelectorAll("td")[2]));

        // Menú "⋯" según el estado (B-4bis.5)
        Assert.Equal(["Nueva contraseña temporal", "Desactivar"], await OpcionesDeAsync(pagina, ana.UserName!));
        Assert.Equal(["Reactivar"], await OpcionesDeAsync(pagina, bruno.UserName!));
        Assert.Equal(["Nueva contraseña temporal", "Desbloquear", "Desactivar"], await OpcionesDeAsync(pagina, carla.UserName!));

        // Desactivar con confirmación (B-4bis.6)
        await ElegirAsync(pagina, ana.UserName!, "Desactivar");
        await pagina.WaitForAssertionAsync(() => Assert.Equal($"¿Desactivar a {ana.UserName}?", Texto(pagina.Find("[role=dialog] .dialogo__titulo"))));
        Assert.Contains("No va a poder ingresar hasta que la reactives.", (await pagina.WaitForElementAsync("[role=dialog]")).TextContent);
        Assert.Equal(["Cancelar", "Desactivar usuario"], pagina.FindAll("[role=dialog] .dialogo__botones button").Select(Texto));
        await pagina.PulsarAsync("[role=dialog] button", b => Texto(b) == "Desactivar usuario");
        await AvisosAsync(pagina, "Usuario desactivado");
        Assert.False((await RecargarAsync(app, ana.Id)).Activo);

        // Reactivar y desbloquear: sin confirmación (UX §1.5)
        await ElegirAsync(pagina, bruno.UserName!, "Reactivar");
        await AvisosAsync(pagina, "Usuario reactivado");
        Assert.True((await RecargarAsync(app, bruno.Id)).Activo);
        await ElegirAsync(pagina, carla.UserName!, "Desbloquear");
        await AvisosAsync(pagina, "Usuario desbloqueado");

        // Nueva contraseña temporal: confirmación y la contraseña en su propio diálogo (B-4bis.8)
        await ElegirAsync(pagina, diego.Usuario, "Nueva contraseña temporal");
        await pagina.WaitForAssertionAsync(() =>
            Assert.Equal($"¿Generar otra contraseña temporal para {diego.Usuario}?", Texto(pagina.Find("[role=dialog] .dialogo__titulo"))));
        Assert.Contains("La contraseña actual deja de servir.", (await pagina.WaitForElementAsync("[role=dialog]")).TextContent);
        await pagina.PulsarAsync("[role=dialog] button", b => Texto(b) == "Generar contraseña");
        await pagina.WaitForAssertionAsync(() => Assert.NotNull(pagina.Find("[role=dialog] code#contrasena-temporal")));
        var temporal = Texto((await pagina.WaitForElementAsync("[role=dialog] code#contrasena-temporal")));
        Assert.Contains($"Nueva contraseña temporal para {diego.Usuario}:", (await pagina.WaitForElementAsync("[role=dialog]")).TextContent);
        Assert.Empty(pagina.FindAll(".zona-de-notificaciones code"));   // nunca en un toast
        await pagina.PulsarAsync("[role=dialog] button", b => Texto(b) == "Listo");
        await pagina.WaitForAssertionAsync(() => Assert.Empty(pagina.FindAll("[role=dialog]")));
        Assert.DoesNotContain(temporal, pagina.Markup);

        // Alta en un modal (B-4bis.1) y la contraseña del alta en su diálogo
        await pagina.PulsarAsync(".page-header .btn-primary");
        Assert.Equal(["Nombre completo *", "Usuario *", "Rol *"], pagina.FindAll("[role=dialog] .campo__etiqueta").Select(Texto));
        Assert.Equal("Por ejemplo: ana.ruiz. Letras sin acento, números, punto, guion o guion bajo.", Texto((await pagina.WaitForElementAsync("#alta-usuario-ayuda"))));
        await pagina.EnviarAsync("[role=dialog] form");
        Assert.Contains(ReglasUsuarioPersonal.NombreVacio, (await pagina.WaitForElementAsync("#alta-nombre-error")).TextContent);
        Assert.Contains(ReglasUsuarioPersonal.UsuarioVacio, (await pagina.WaitForElementAsync("#alta-usuario-error")).TextContent);
        Assert.Contains(ReglasUsuarioPersonal.RolInvalido, (await pagina.WaitForElementAsync("#alta-rol-error")).TextContent);
        await pagina.CambiarAsync("input#alta-nombre", $"Gabriela {marca}");
        await pagina.CambiarAsync("input#alta-usuario", $"g-{marca}");
        await pagina.CambiarAsync("select#alta-rol", Roles.Medico);
        await pagina.EnviarAsync("[role=dialog] form");
        await pagina.WaitForAssertionAsync(() => Assert.NotNull(pagina.Find("[role=dialog] code#contrasena-temporal")));
        Assert.Contains($"Creaste el usuario g-{marca} (Médico). Contraseña temporal:", Texto((await pagina.WaitForElementAsync("[role=dialog]"))));
        Assert.Contains("Usuario creado", await AvisosAsync(pagina, "Usuario creado"));
        var delAlta = Texto((await pagina.WaitForElementAsync("[role=dialog] code#contrasena-temporal")));
        await pagina.PulsarAsync("[role=dialog] button", b => Texto(b) == "Listo");
        await pagina.WaitForAssertionAsync(() => Assert.DoesNotContain(delAlta, pagina.Markup));
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "76")]
    [Trait("Story", "2.2")]
    [Trait("AC", "9")]
    [Trait("Story", "2.3")]
    [Trait("AC", "7")]
    public async Task Usuarios_FilaPropia_DiceSosVos_SinAccionesProhibidas()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        using var alcance = this.ConLaApp(app);
        await this.ComoAdmin1Async(app);

        var pagina = await AbrirAsync($"buscar={PersonalWebApplicationFactory.Admin.Usuario}");

        var propia = FilaDe(pagina, PersonalWebApplicationFactory.Admin.Usuario);
        Assert.Contains("(sos vos)", propia.TextContent);
        Assert.Null(propia.QuerySelector(".menu-de-acciones__boton"));   // ni desactivarse ni restablecerse
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "72")]
    [Trait("AC", "78")]
    [Trait("Story", "2.2")]
    [Trait("AC", "5")]
    [Trait("AC", "6")]
    [Trait("Story", "2.3")]
    [Trait("AC", "5")]
    public async Task Usuarios_AltaRepetidaOInvalida_YRestablecerABloqueado()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        using var alcance = this.ConLaApp(app);
        await this.ComoAdmin1Async(app);
        var marca = Marca();
        var bloqueada = await CrearUsuarioAsync(app, [Roles.Recepcionista], Clave, nombre: $"Hilda {marca}");
        await FallarAsync(app, bloqueada.Id, 5);

        var pagina = await AbrirAsync($"buscar={marca}");
        await pagina.PulsarAsync(".page-header .btn-primary");
        await pagina.CambiarAsync("input#alta-nombre", "Otra Persona");
        await pagina.CambiarAsync("input#alta-usuario", "ana ruiz");
        await pagina.EnviarAsync("[role=dialog] form");
        Assert.Contains(ReglasUsuarioPersonal.UsuarioInvalido, (await pagina.WaitForElementAsync("#alta-usuario-error")).TextContent);
        await pagina.CambiarAsync("input#alta-usuario", "Recepcion1");
        await pagina.CambiarAsync("select#alta-rol", Roles.Medico);
        await pagina.EnviarAsync("[role=dialog] form");
        await pagina.WaitForAssertionAsync(() => Assert.Equal("Ya existe un usuario con el nombre recepcion1. Elegí otro.",
            Texto(pagina.Find("[role=dialog] .resumen-de-errores"))));
        Assert.Equal("Recepcion1", (await pagina.WaitForElementAsync("input#alta-usuario")).GetAttribute("value"));
        await pagina.PulsarAsync("[role=dialog] .formulario-abm__botones button", b => Texto(b) == "Cancelar");
        await pagina.PulsarAsync("[role=dialog] button", b => Texto(b) == "Salir sin guardar");
        await pagina.WaitForAssertionAsync(() => Assert.Empty(pagina.FindAll("[role=dialog]")));

        await ElegirAsync(pagina, bloqueada.UserName!, "Nueva contraseña temporal");
        await pagina.PulsarAsync("[role=dialog] button", b => Texto(b) == "Generar contraseña");
        await pagina.WaitForAssertionAsync(() => Assert.Contains("Su usuario también quedó desbloqueado.", pagina.Find("[role=dialog]").TextContent));
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "3")]
    [Trait("AC", "77")]
    public async Task Usuarios_ReactivarRechazadoPorElDominio_AvisaArribaDeLaTabla()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        using var alcance = this.ConLaApp(app);
        var actor = await this.ComoAdmin1Async(app);
        var marca = Marca();
        var inactivo = await CrearUsuarioAsync(app, [Roles.Recepcionista], Clave, activo: false, nombre: $"Irene {marca}");

        var pagina = await AbrirAsync($"buscar={marca}&estado=todos");
        // Otro administrador lo reactiva mientras la pantalla está abierta: la fila todavía ofrece "Reactivar".
        await alcance.ServiceProvider.GetRequiredService<IGestionUsuariosPersonal>().ReactivarAsync(inactivo.Id, actor, CancellationToken.None);
        await ElegirAsync(pagina, inactivo.UserName!, "Reactivar");

        await pagina.WaitForAssertionAsync(() =>
            Assert.Equal("Ese usuario ya estaba activo.", Texto(pagina.Find(".alert-danger[role=alert]"))));
        Assert.Empty(pagina.FindAll("[role=dialog]"));   // sin confirmación (UX §1.5)
        Assert.DoesNotContain("Usuario reactivado", Services.GetRequiredService<Notificaciones>().Visibles.Select(a => a.Texto));
    }

    // Doble: los médicos vinculados (la columna de A4) sin base.
    private sealed class SinVinculos : IGestionMedicos
    {
        public Task<IReadOnlyDictionary<Guid, string>> MedicosVinculadosAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());
        public Task<IReadOnlyList<MedicoListado>> ListarAsync(string? buscar, CancellationToken ct) => throw new InvalidOperationException("No se usa.");
        public Task<PaginaDeResultados<MedicoListado>> ListarPaginadoAsync(ConsultaDeListado consulta, CancellationToken ct) => throw new InvalidOperationException("No se usa.");
        public Task<DatosFichaMedico?> ObtenerAsync(long id, CancellationToken ct) => throw new InvalidOperationException("No se usa.");
        public Task<IReadOnlyList<OpcionUsuarioMedico>> UsuariosParaVincularAsync(long? medicoId, CancellationToken ct) => throw new InvalidOperationException("No se usa.");
        public Task<long> CrearAsync(DatosMedico datos, ActorAuditoria actor, CancellationToken ct) => throw new InvalidOperationException("No se usa.");
        public Task<bool> EditarAsync(long id, DatosMedico datos, ActorAuditoria actor, CancellationToken ct) => throw new InvalidOperationException("No se usa.");
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "4")]
    [Trait("Story", "2.2")]
    [Trait("AC", "18")]
    public async Task Usuarios_FallaLaBaseAlCrear_MensajeGenericoSinContrasena()
    {
        var registro = new RegistroEnMemoria<LimiteDeErrores>();
        Services.AddSingleton<ILogger<LimiteDeErrores>>(registro);
        Services.AddSingleton<IGestionUsuariosPersonal, GestionUsuariosConBaseCaida>();
        Services.AddSingleton<IGestionMedicos, SinVinculos>();
        Services.AddSingleton<AlcancePorOperacion>();
        Services.AddScoped<Notificaciones>();
        Services.AddSingleton(TimeProvider.System);
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.Como(new ActorAuditoria(Guid.NewGuid().ToString(), "Lucía Pérez", Roles.Administrador, CanalAuditoria.Personal));

        var pagina = await AbrirAsync();
        await pagina.PulsarAsync(".page-header .btn-primary");
        await pagina.CambiarAsync("input#alta-nombre", "Persona Nueva");
        await pagina.CambiarAsync("input#alta-usuario", "persona.nueva");
        await pagina.CambiarAsync("select#alta-rol", Roles.Recepcionista);
        await pagina.EnviarAsync("[role=dialog] form");

        await pagina.WaitForAssertionAsync(() => Assert.Contains("Algo falló. Probá de nuevo en unos minutos.", pagina.Markup));
        Assert.DoesNotContain("secreto", pagina.Markup);
        Assert.Empty(pagina.FindAll("code#contrasena-temporal"));
        Assert.Contains(registro.Entradas, e => e.Nivel == LogLevel.Error && e.Excepcion is NpgsqlException);
    }
}
