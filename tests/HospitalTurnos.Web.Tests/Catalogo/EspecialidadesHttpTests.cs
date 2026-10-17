using System.Net;
using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Catalogo;
using HospitalTurnos.Dominio.Auditoria;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Infraestructura.Identidad;
using HospitalTurnos.Web.Tests.Personal;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using static HospitalTurnos.Web.Tests.Personal.FormularioHttp;

namespace HospitalTurnos.Web.Tests.Catalogo;

// A2 con HttpClient sobre la app real (token y cookies incluidos), como lo usa un navegador: es SSR sin
// JavaScript propio. admin1 actúa sobre especialidades propias de cada prueba (con su marca); su estado
// nunca cambia. Las especialidades no se borran: nadie cuenta filas sin la marca.
[Collection("Postgres")]
public class EspecialidadesHttpTests(PostgresFixture fixture)
{
    private const string A2 = "/personal/admin/especialidades";
    private const string SinPermiso = "Sólo el administrador puede modificar las especialidades.";

    private static string Marca() => Guid.NewGuid().ToString("N")[..8];

    private static async Task<HttpClient> IngresadoAsync(PersonalWebApplicationFactory app, string usuario, string clave)
    {
        var cliente = Cliente(app);
        Assert.NotEmpty(CookiesPersonal(await IngresarAsync(cliente, usuario, clave)));
        return cliente;
    }

    private static Task<HttpClient> Admin1Async(PersonalWebApplicationFactory app) =>
        IngresadoAsync(app, PersonalWebApplicationFactory.Admin.Usuario, PersonalWebApplicationFactory.Admin.Contrasena);

    private static Task<HttpResponseMessage> AltaAsync(HttpClient cliente, string nombre, bool conToken = true) =>
        EnviarAsync(cliente, A2, "alta-especialidad", new Dictionary<string, string> { ["Alta.Nombre"] = nombre }, conToken);

    private static Task<HttpResponseMessage> AccionAsync(HttpClient cliente, string accion, bool conToken = true) =>
        EnviarAsync(cliente, A2, "acciones-especialidades", new Dictionary<string, string> { ["Accion"] = accion }, conToken);

    private static async Task<string> HtmlAsync(HttpResponseMessage respuesta) =>
        WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());

    // La fila <tr> de la tabla que muestra esa especialidad (null si no está).
    private static string? FilaDe(string html, string nombre)
    {
        var celda = html.IndexOf($">{nombre}<", StringComparison.Ordinal);
        if (celda < 0)
            return null;
        var inicio = html.LastIndexOf("<tr", celda, StringComparison.Ordinal);
        var fin = html.IndexOf("</tr>", celda, StringComparison.Ordinal);
        return html[inicio..fin];
    }

    // Los datos previos se crean por el caso de uso, con el actor de admin1 (existe en la base: IActorVigente
    // lo acepta), no por formulario: es más rápido y no es lo que se prueba ahí.
    private static async Task<ActorAuditoria> ActorDeAdmin1Async(PersonalWebApplicationFactory app)
    {
        using var alcance = app.Services.CreateScope();
        var admin1 = (await alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>()
            .FindByNameAsync(PersonalWebApplicationFactory.Admin.Usuario))!;
        return new ActorAuditoria(admin1.Id.ToString(), admin1.NombreCompleto, Roles.Administrador, CanalAuditoria.Personal);
    }

    private static async Task<long> CrearPreviaAsync(PersonalWebApplicationFactory app, string nombre, bool activa = true)
    {
        var actor = await ActorDeAdmin1Async(app);
        using var alcance = app.Services.CreateScope();
        var catalogo = alcance.ServiceProvider.GetRequiredService<ICatalogoEspecialidades>();
        var id = await catalogo.CrearAsync(actor, nombre, CancellationToken.None);
        if (!activa)
            await catalogo.DesactivarAsync(actor, id, CancellationToken.None);
        return id;
    }

    private static async Task<EspecialidadResumen?> BuscarAsync(PersonalWebApplicationFactory app, long id)
    {
        using var alcance = app.Services.CreateScope();
        return await alcance.ServiceProvider.GetRequiredService<ICatalogoEspecialidades>().BuscarAsync(id, CancellationToken.None);
    }

    private async Task<long> ContarAsync(string sql, params (string Nombre, object Valor)[] parametros)
    {
        await using var conexion = new NpgsqlConnection(fixture.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        foreach (var (nombre, valor) in parametros)
            comando.Parameters.AddWithValue(nombre, valor);
        return (long)(await comando.ExecuteScalarAsync())!;
    }

    private Task<long> EspecialidadesConMarcaAsync(string marca) =>
        ContarAsync("SELECT count(*) FROM especialidades WHERE nombre LIKE @m", ("m", $"%{marca}%"));

    private Task<long> AuditadasConMarcaAsync(string marca) =>
        ContarAsync("SELECT count(*) FROM registros_auditoria WHERE entidad_tipo = 'Especialidad' AND detalle::text LIKE @m",
            ("m", $"%{marca}%"));

    private Task<long> AuditadasDeAsync(long id) =>
        ContarAsync("SELECT count(*) FROM registros_auditoria WHERE entidad_tipo = 'Especialidad' AND entidad_id = @id",
            ("id", id.ToString()));

    [Fact]
    [Trait("Story", "3.1")]
    [Trait("AC", "15")]
    public async Task GetEspecialidades_Administrador_MuestraTablaOrdenadaConEstadoYBoton()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var m = Marca();
        await CrearPreviaAsync(app, $"{m} B");
        await CrearPreviaAsync(app, $"{m} A", activa: false);
        var cliente = await Admin1Async(app);

        var respuesta = await cliente.GetAsync(A2);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var html = await HtmlAsync(respuesta);
        Assert.Contains("<h1>Especialidades</h1>", html);
        Assert.Contains("<label for=\"nombre\"", html);
        Assert.Contains(">Nombre</label>", html);
        Assert.Contains(">Agregar</button>", html);
        var filaA = FilaDe(html, $"{m} A");
        var filaB = FilaDe(html, $"{m} B");
        Assert.NotNull(filaA);
        Assert.NotNull(filaB);
        Assert.True(html.IndexOf(filaA, StringComparison.Ordinal) < html.IndexOf(filaB, StringComparison.Ordinal),
            "La tabla no está ordenada por nombre.");
        Assert.Contains(">Desactivada<", filaA);
        Assert.Contains(">Reactivar</button>", filaA);
        Assert.DoesNotContain(">Desactivar</button>", filaA);
        Assert.Contains(">Activa<", filaB);
        Assert.Contains(">Desactivar</button>", filaB);
        Assert.DoesNotContain(">Reactivar</button>", filaB);
    }

    [Fact]
    [Trait("Story", "3.1")]
    [Trait("AC", "5")]
    [Trait("AC", "15")]
    public async Task PostAlta_NombreNuevo_LaAgregaYAuditaConElAdministradorDeLaSesion()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var cliente = await Admin1Async(app);
        var actor = await ActorDeAdmin1Async(app);
        var nombre = $"{Marca()} Traumatología";

        var respuesta = await AltaAsync(cliente, nombre);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var html = await HtmlAsync(respuesta);
        Assert.Equal($"Especialidad {nombre} agregada.", Estado(html));
        Assert.Null(Alerta(html));
        Assert.NotNull(FilaDe(html, nombre));
        Assert.Equal("", ValorDe(html, "Alta.Nombre"));   // el campo está y quedó vacío
        Assert.Equal(1, await ContarAsync(
            """
            SELECT count(*) FROM registros_auditoria
            WHERE accion = @a AND usuario_id = @u AND quien = @q AND rol = @r AND detalle::text LIKE @m
            """,
            ("a", AccionesAuditoria.EspecialidadCreada), ("u", actor.UsuarioId!),
            ("q", PersonalWebApplicationFactory.Admin.Nombre), ("r", Roles.Administrador), ("m", $"%{nombre}%")));
    }

    [Theory]
    [Trait("Story", "3.1")]
    [Trait("AC", "8")]
    [InlineData(false, "Ingresá el nombre de la especialidad.")]
    [InlineData(true, "El nombre de la especialidad tiene como máximo 80 caracteres.")]
    public async Task PostAlta_NombreVacioOLargo_MuestraMensajeBajoElCampoSinGuardar(bool largo, string mensaje)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var cliente = await Admin1Async(app);
        var m = Marca();
        var tipeado = largo ? m + new string('x', 81 - m.Length) : "";

        var respuesta = await AltaAsync(cliente, tipeado);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var html = await HtmlAsync(respuesta);
        var campo = html.IndexOf("id=\"nombre\"", StringComparison.Ordinal);
        var mensajeEn = html.IndexOf(mensaje, StringComparison.Ordinal);
        Assert.True(campo >= 0 && mensajeEn > campo, "El mensaje no está debajo del campo Nombre.");
        Assert.Null(Estado(html));
        Assert.Equal(0, await EspecialidadesConMarcaAsync(m));
        Assert.Equal(0, await AuditadasConMarcaAsync(m));
    }

    [Fact]
    [Trait("Story", "3.1")]
    [Trait("AC", "6")]
    [Trait("AC", "15")]
    public async Task PostAlta_NombreRepetido_Devuelve409ConElCartelDeLaRegla()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var m = Marca();
        await CrearPreviaAsync(app, $"{m} Pediatría");
        var cliente = await Admin1Async(app);

        var respuesta = await AltaAsync(cliente, $"{m} pediatria");

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        var html = await HtmlAsync(respuesta);
        Assert.Equal($"Ya existe la especialidad {m} Pediatría.", Alerta(html));
        Assert.Equal($"{m} pediatria", ValorDe(html, "Alta.Nombre"));
        Assert.Equal(1, await EspecialidadesConMarcaAsync(m));
        Assert.Equal(1, html.Split($">{m} Pediatría<").Length - 1);
    }

    [Fact]
    [Trait("Story", "3.1")]
    [Trait("AC", "9")]
    [Trait("AC", "11")]
    [Trait("AC", "15")]
    public async Task PostCambioDeEstado_ActivaYDesactivada_MuestraElEstadoNuevo()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var nombre = $"{Marca()} Cardiología";
        var id = await CrearPreviaAsync(app, nombre);
        var cliente = await Admin1Async(app);

        var desactivar = await AccionAsync(cliente, $"desactivar:{id}");

        Assert.Equal(HttpStatusCode.OK, desactivar.StatusCode);
        var html = await HtmlAsync(desactivar);
        Assert.Equal($"Especialidad {nombre} desactivada.", Estado(html));
        var fila = FilaDe(html, nombre)!;
        Assert.Contains(">Desactivada<", fila);
        Assert.Contains(">Reactivar</button>", fila);

        var reactivar = await AccionAsync(cliente, $"reactivar:{id}");

        Assert.Equal(HttpStatusCode.OK, reactivar.StatusCode);
        html = await HtmlAsync(reactivar);
        Assert.Equal($"Especialidad {nombre} reactivada.", Estado(html));
        fila = FilaDe(html, nombre)!;
        Assert.Contains(">Activa<", fila);
        Assert.Contains(">Desactivar</button>", fila);
        Assert.True((await BuscarAsync(app, id))!.Activa);
    }

    [Theory]
    [Trait("Story", "3.1")]
    [Trait("AC", "3")]
    [Trait("AC", "12")]
    [Trait("AC", "13")]
    [Trait("AC", "15")]
    [InlineData("a-ya-desactivada", HttpStatusCode.Conflict)]
    [InlineData("b-inexistente", HttpStatusCode.NotFound)]
    [InlineData("c-accion-armada", HttpStatusCode.BadRequest)]
    [InlineData("d-actor-desactivado", HttpStatusCode.Forbidden)]
    public async Task PostCambioDeEstado_ErroresDeRegla_MuestraCartelConSuCodigo(string caso, HttpStatusCode codigo)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var nombre = $"{Marca()} Neurología";
        var id = await CrearPreviaAsync(app, nombre, activa: caso != "a-ya-desactivada");
        HttpClient cliente;
        if (caso == "d-actor-desactivado")
        {
            var propio = await CrearUsuarioAsync(app, [Roles.Administrador], "Admin-Propio-21");
            cliente = await IngresadoAsync(app, propio.UserName!, "Admin-Propio-21");
            // Lo desactivan en la base y, SIN adelantar el reloj, su sesión todavía no se revalidó.
            using var alcance = app.Services.CreateScope();
            var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>();
            var cargado = (await usuarios.FindByIdAsync(propio.Id.ToString()))!;
            cargado.Activo = false;
            Assert.True((await usuarios.UpdateAsync(cargado)).Succeeded);
        }
        else
            cliente = await Admin1Async(app);
        var (accion, mensaje) = caso switch
        {
            "a-ya-desactivada" => ($"desactivar:{id}", $"La especialidad {nombre} ya está desactivada."),
            "b-inexistente" => ("desactivar:9223372036854775807", "La especialidad ya no existe. Actualizá la lista."),
            "c-accion-armada" => ($"borrar:{id}", "No reconocemos esa acción. Recargá la página."),
            _ => ($"desactivar:{id}", SinPermiso),
        };
        var antes = (await BuscarAsync(app, id))!.Activa;
        var auditadasAntes = await AuditadasDeAsync(id);

        var respuesta = await AccionAsync(cliente, accion);

        Assert.Equal(codigo, respuesta.StatusCode);
        var html = await HtmlAsync(respuesta);
        Assert.Equal(mensaje, Alerta(html));
        Assert.Contains("<h1>Especialidades</h1>", html);   // la propia A2, nunca /Error
        Assert.NotNull(FilaDe(html, nombre));               // con la tabla al día
        Assert.Equal(antes, (await BuscarAsync(app, id))!.Activa);
        Assert.Equal(auditadasAntes, await AuditadasDeAsync(id));
    }

    [Theory]
    [Trait("Story", "3.1")]
    [Trait("AC", "14")]
    [InlineData("alta")]
    [InlineData("acciones")]
    public async Task PostEspecialidades_SinToken_Devuelve400SinCambios(string formulario)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var m = Marca();
        var id = await CrearPreviaAsync(app, $"{m} Activa");
        var cliente = await Admin1Async(app);

        var respuesta = formulario == "alta"
            ? await AltaAsync(cliente, $"{m} Sin token", conToken: false)
            : await AccionAsync(cliente, $"desactivar:{id}", conToken: false);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(1, await EspecialidadesConMarcaAsync(m));   // sólo la previa
        Assert.True((await BuscarAsync(app, id))!.Activa);
        Assert.Equal(1, await AuditadasConMarcaAsync(m));        // sólo el alta previa
    }
}
