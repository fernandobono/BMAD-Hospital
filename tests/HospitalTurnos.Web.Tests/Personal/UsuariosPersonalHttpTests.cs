using System.Net;
using System.Text.RegularExpressions;
using HospitalTurnos.Aplicacion.Personal;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Infraestructura.Identidad;
using HospitalTurnos.Web.Tests.Personal.Dobles;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using static HospitalTurnos.Web.Tests.Personal.FormularioHttp;

namespace HospitalTurnos.Web.Tests.Personal;

// A4 con HttpClient sobre la app real (token y cookies incluidos), como lo usa un navegador: es SSR sin
// JavaScript propio. admin1 actúa sobre usuarios propios de cada prueba; su estado nunca cambia.
[Collection("Postgres")]
public class UsuariosPersonalHttpTests(PostgresFixture fixture)
{
    private const string A4 = "/personal/admin/usuarios";
    private const string ClavePropia = "Clave-Propia-26";
    private const string TextoEnConstruccion = "Esta sección se habilita en una próxima versión.";

    private static string Marca() => Guid.NewGuid().ToString("N")[..8];

    private static async Task<HttpClient> IngresadoAsync(PersonalWebApplicationFactory app, string usuario, string clave,
        bool seguirRedirecciones = false)
    {
        var cliente = Cliente(app, seguirRedirecciones: seguirRedirecciones);
        var ingreso = await IngresarAsync(cliente, usuario, clave);
        // Ingresó: sin seguir la redirección, 302 con la cookie; siguiéndola, ya no está en S0.
        if (seguirRedirecciones)
            Assert.NotEqual(RutaIngreso, ingreso.RequestMessage!.RequestUri!.AbsolutePath);
        else
            Assert.NotEmpty(CookiesPersonal(ingreso));
        return cliente;
    }

    private static Task<HttpClient> Admin1Async(PersonalWebApplicationFactory app) =>
        IngresadoAsync(app, PersonalWebApplicationFactory.Admin.Usuario, PersonalWebApplicationFactory.Admin.Contrasena);

    private static Task<HttpResponseMessage> AltaAsync(HttpClient cliente, string nombre, string usuario, string rol,
        bool conToken = true, string? accept = null) =>
        EnviarAsync(cliente, A4, "alta-usuario", new Dictionary<string, string>
        {
            ["Alta.NombreCompleto"] = nombre,
            ["Alta.Usuario"] = usuario,
            ["Alta.Rol"] = rol,
        }, conToken, accept);

    private static Task<HttpResponseMessage> AccionAsync(HttpClient cliente, string accion, bool conToken = true) =>
        EnviarAsync(cliente, A4, "acciones-usuarios", new Dictionary<string, string> { ["Accion"] = accion }, conToken);

    private static async Task<string> HtmlAsync(HttpResponseMessage respuesta) =>
        WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());

    // La fila <tr> de la tabla que muestra a ese usuario (null si no está).
    private static string? FilaDe(string html, string usuario)
    {
        var celda = html.IndexOf($">{usuario}<", StringComparison.Ordinal);
        if (celda < 0)
            return null;
        var inicio = html.LastIndexOf("<tr", celda, StringComparison.Ordinal);
        var fin = html.IndexOf("</tr>", celda, StringComparison.Ordinal);
        return html[inicio..fin];
    }

    private static async Task<UsuarioPersonal?> BuscarAsync(PersonalWebApplicationFactory app, string usuario)
    {
        using var alcance = app.Services.CreateScope();
        return await alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>().FindByNameAsync(usuario);
    }

    private async Task<long> UsuariosConNombreAsync(string usuario)
    {
        await using var conexion = new NpgsqlConnection(fixture.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand("SELECT count(*) FROM asp_net_users WHERE normalized_user_name = @n", conexion);
        comando.Parameters.AddWithValue("n", usuario.ToUpperInvariant());
        return (long)(await comando.ExecuteScalarAsync())!;
    }

    private async Task<List<string>> DetallesAuditadosDeAsync(Guid id)
    {
        await using var conexion = new NpgsqlConnection(fixture.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(
            "SELECT coalesce(detalle::text, '') FROM registros_auditoria WHERE entidad_id = @id", conexion);
        comando.Parameters.AddWithValue("id", id.ToString());
        var detalles = new List<string>();
        await using var lector = await comando.ExecuteReaderAsync();
        while (await lector.ReadAsync())
            detalles.Add(lector.GetString(0));
        return detalles;
    }

    private static async Task MarcarInactivoAsync(PersonalWebApplicationFactory app, Guid id)
    {
        using var alcance = app.Services.CreateScope();
        var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>();
        var usuario = (await usuarios.FindByIdAsync(id.ToString()))!;
        usuario.Activo = false;
        Assert.True((await usuarios.UpdateAsync(usuario)).Succeeded);
    }

    [Fact]
    [Trait("Story", "2.2")]
    [Trait("AC", "1")]
    [Trait("AC", "27")]
    public async Task GetUsuarios_Administrador_MuestraTablaConAccionesSegunEstado()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var cliente = await Admin1Async(app);
        var activo = await CrearUsuarioAsync(app, [Roles.Recepcionista], ClavePropia);
        var desactivado = await CrearUsuarioAsync(app, [Roles.Medico], ClavePropia, activo: false);
        var bloqueado = await CrearUsuarioAsync(app, [Roles.Recepcionista], ClavePropia);
        await FallarAsync(app, bloqueado.Id, 5);
        var admin1 = (await BuscarAsync(app, PersonalWebApplicationFactory.Admin.Usuario))!;

        var respuesta = await cliente.GetAsync(A4);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(respuesta.Headers.CacheControl?.NoStore, "falta Cache-Control: no-store");
        var html = await HtmlAsync(respuesta);
        Assert.Contains("<h1>Usuarios del personal</h1>", html);
        Assert.DoesNotContain(TextoEnConstruccion, html);
        foreach (var columna in new[] { "Usuario", "Nombre", "Rol", "Estado", "Acciones" })
            Assert.Contains($"<th>{columna}</th>", html);

        var filaActivo = FilaDe(html, activo.UserName!)!;
        Assert.Contains("Activo", filaActivo);
        Assert.Contains("Recepcionista", filaActivo);
        Assert.Contains($"value=\"desactivar:{activo.Id}\"", filaActivo);
        Assert.DoesNotContain("reactivar:", filaActivo);
        Assert.DoesNotContain("desbloquear:", filaActivo);

        var filaDesactivado = FilaDe(html, desactivado.UserName!)!;
        Assert.Contains("Desactivado", filaDesactivado);
        Assert.Contains("Médico", filaDesactivado);
        Assert.Contains($"value=\"reactivar:{desactivado.Id}\"", filaDesactivado);
        Assert.DoesNotContain("desactivar:", filaDesactivado);

        var filaBloqueado = FilaDe(html, bloqueado.UserName!)!;
        Assert.Matches(@"Bloqueado hasta las \d\d:\d\d", filaBloqueado);
        Assert.Contains($"value=\"desbloquear:{bloqueado.Id}\"", filaBloqueado);

        var filaAdmin1 = FilaDe(html, admin1.UserName!)!;
        Assert.DoesNotContain($"desactivar:{admin1.Id}", filaAdmin1);
        Assert.Contains("(sos vos)", filaAdmin1);   // legibilidad: dice por qué no tiene el botón
    }

    [Theory]
    [Trait("Story", "2.2")]
    [Trait("AC", "2")]
    [InlineData("recepcion1")]
    [InlineData("medico1")]
    public async Task Usuarios_OtrosRoles_NoVenNiModifican(string usuario)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var fijo = PersonalWebApplicationFactory.Fijos.Single(f => f.Usuario == usuario);
        var navegador = await IngresadoAsync(app, fijo.Usuario, fijo.Contrasena, seguirRedirecciones: true);

        var pagina = await navegador.GetAsync(A4);

        Assert.Equal(HttpStatusCode.Forbidden, pagina.StatusCode);
        Assert.Contains("No tenés permiso para entrar a esta sección.", await HtmlAsync(pagina));

        // POST armado a mano, con un token válido de su propia sesión (de su página de inicio).
        var cliente = await IngresadoAsync(app, fijo.Usuario, fijo.Contrasena);
        var nuevo = $"p-{Marca()}";
        var campos = new Dictionary<string, string>
        {
            ["_handler"] = "alta-usuario",
            ["__RequestVerificationToken"] = await TokenDeAsync(cliente, fijo.Rol == Roles.Medico
                ? "/personal/medico/mi-agenda" : "/personal/recepcion/turnos"),
            ["Alta.NombreCompleto"] = "Persona Intrusa",
            ["Alta.Usuario"] = nuevo,
            ["Alta.Rol"] = Roles.Administrador,
        };
        var post = await cliente.PostAsync(A4, new FormUrlEncodedContent(campos));

        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.StartsWith("/personal/acceso-denegado", Destino(post));
        Assert.Null(await BuscarAsync(app, nuevo));
    }

    [Fact]
    [Trait("Story", "2.2")]
    [Trait("AC", "3")]
    [Trait("AC", "4")]
    public async Task PostAlta_DatosValidos_MuestraLaContrasenaTemporalUnaSolaVez()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var cliente = await Admin1Async(app);
        var m = Marca();
        var usuario = $"p-{m}";

        var respuesta = await AltaAsync(cliente, $"Persona {m}", usuario, Roles.Recepcionista);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(respuesta.Headers.CacheControl?.NoStore, "falta Cache-Control: no-store");
        var html = await HtmlAsync(respuesta);
        var temporal = Regex.Match(html, "<code id=\"contrasena-temporal\">(.*?)</code>").Groups[1].Value;
        Assert.Equal(12, temporal.Length);
        Assert.Equal(
            $"Creaste el usuario {usuario} (Recepcionista). Contraseña temporal: {temporal}. Anotala y dásela a la persona: no se vuelve a mostrar. La tiene que cambiar en su primer ingreso.",
            Estado(html));
        Assert.NotNull(FilaDe(html, usuario));
        Assert.Equal("", ValorDe(html, "Alta.NombreCompleto"));
        Assert.Equal("", ValorDe(html, "Alta.Usuario"));
        Assert.All(app.Registro.Entradas, e => Assert.DoesNotContain(temporal, e.Mensaje + e.Excepcion));
        var creado = (await BuscarAsync(app, usuario))!;
        Assert.All(await DetallesAuditadosDeAsync(creado.Id), d => Assert.DoesNotContain(temporal, d));

        var despues = await HtmlAsync(await cliente.GetAsync(A4));
        Assert.DoesNotContain(temporal, despues);
        Assert.DoesNotContain("contrasena-temporal", despues);

        var otro = Cliente(app);
        var ingreso = await IngresarAsync(otro, usuario, temporal);
        Assert.Equal(HttpStatusCode.Redirect, ingreso.StatusCode);
        Assert.Equal("/personal/cambiar-contrasena", Destino(ingreso));
    }

    [Theory]
    [Trait("Story", "2.2")]
    [Trait("AC", "5")]
    [InlineData("", "", "", new[] { "Ingresá el nombre completo.", "Ingresá el nombre de usuario.", "Elegí un rol." })]
    [InlineData("Persona Nueva", "con espacio", Roles.Medico,
        new[] { "El usuario tiene entre 3 y 100 caracteres: letras sin acento, números, punto, guion o guion bajo." })]
    [InlineData("Persona Nueva", "{u}", "Superusuario", new[] { "Elegí un rol." })]
    public async Task PostAlta_DatosInvalidosOArmados_MuestraMensajesSinCrear(string nombre, string usuario, string rol, string[] mensajes)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var cliente = await Admin1Async(app);
        var tipeado = usuario.Replace("{u}", $"p-{Marca()}");

        var respuesta = await AltaAsync(cliente, nombre, tipeado, rol);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var html = await HtmlAsync(respuesta);
        foreach (var mensaje in mensajes)
            Assert.Contains(mensaje, html);
        Assert.Contains("Por ejemplo: ana.ruiz. Letras sin acento, números, punto, guion o guion bajo.", html);
        Assert.Null(Estado(html));
        if (tipeado.Length > 0)
            Assert.Equal(0, await UsuariosConNombreAsync(tipeado));
    }

    [Fact]
    [Trait("Story", "2.2")]
    [Trait("AC", "6")]
    public async Task PostAlta_UsuarioRepetido_Devuelve409ConCartel()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var cliente = await Admin1Async(app);

        var respuesta = await AltaAsync(cliente, "Otra Persona", "Recepcion1", Roles.Medico);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        var html = await HtmlAsync(respuesta);
        Assert.Equal("Ya existe un usuario con el nombre recepcion1. Elegí otro.", Alerta(html));
        Assert.Equal("Recepcion1", ValorDe(html, "Alta.Usuario"));
        Assert.Equal(1, await UsuariosConNombreAsync("recepcion1"));
    }

    [Theory]
    [Trait("Story", "2.2")]
    [Trait("AC", "8")]
    [Trait("AC", "11")]
    [Trait("AC", "13")]
    [InlineData("desactivar")]
    [InlineData("reactivar")]
    [InlineData("desbloquear")]
    public async Task PostAcciones_SegunEstado_CambianElEstadoYConfirman(string accion)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var cliente = await Admin1Async(app);
        var propio = await CrearUsuarioAsync(app, [Roles.Recepcionista], ClavePropia, activo: accion != "reactivar");
        if (accion == "desbloquear")
            await FallarAsync(app, propio.Id, 5);

        var respuesta = await AccionAsync(cliente, $"{accion}:{propio.Id}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var html = await HtmlAsync(respuesta);
        var fila = FilaDe(html, propio.UserName!)!;
        var despues = await RecargarAsync(app, propio.Id);
        switch (accion)
        {
            case "desactivar":
                Assert.Equal($"Desactivaste a {propio.UserName}. Si tenía una sesión abierta, se corta en 5 minutos como máximo.", Estado(html));
                Assert.False(despues.Activo);
                Assert.Contains("Desactivado", fila);
                Assert.Contains($"value=\"reactivar:{propio.Id}\"", fila);
                break;
            case "reactivar":
                Assert.Equal($"Reactivaste a {propio.UserName}. Ya puede ingresar con su contraseña.", Estado(html));
                Assert.True(despues.Activo);
                Assert.Contains($"value=\"desactivar:{propio.Id}\"", fila);
                break;
            default:
                Assert.Equal($"Desbloqueaste a {propio.UserName}. Ya puede volver a intentar el ingreso.", Estado(html));
                Assert.Null(despues.LockoutEnd);
                Assert.DoesNotContain("Bloqueado hasta", fila);
                Assert.DoesNotContain("desbloquear:", fila);
                break;
        }
    }

    [Theory]
    [Trait("Story", "2.2")]
    [Trait("AC", "9")]
    [Trait("AC", "12")]
    [Trait("AC", "14")]
    [Trait("AC", "15")]
    [Trait("AC", "16")]
    [InlineData("a-a-si-mismo", HttpStatusCode.BadRequest, "No podés desactivar tu propio usuario. Pedíselo a otro administrador.")]
    [InlineData("b-ya-desactivado", HttpStatusCode.Conflict, "Ese usuario ya estaba desactivado.")]
    [InlineData("c-no-bloqueado", HttpStatusCode.Conflict, "Ese usuario no está bloqueado.")]
    [InlineData("d-inexistente", HttpStatusCode.NotFound, "No encontramos ese usuario. Recargá la página.")]
    [InlineData("e-accion-armada", HttpStatusCode.BadRequest, "No reconocemos esa acción. Recargá la página.")]
    [InlineData("f-actor-desactivado", HttpStatusCode.Forbidden, "No tenés permiso para administrar usuarios.")]
    public async Task PostAcciones_Errores_MuestranCartelConSuCodigo(string caso, HttpStatusCode codigo, string mensaje)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var objetivo = await CrearUsuarioAsync(app, [Roles.Recepcionista], ClavePropia, activo: caso is not ("b-ya-desactivado" or "f-actor-desactivado"));
        HttpClient cliente;
        string accion;
        UsuarioPersonal? adminPropio = null;
        if (caso is "a-a-si-mismo" or "f-actor-desactivado")
        {
            adminPropio = await CrearUsuarioAsync(app, [Roles.Administrador], "Admin-Propio-26");
            cliente = await IngresadoAsync(app, adminPropio.UserName!, "Admin-Propio-26");
        }
        else
            cliente = await Admin1Async(app);
        switch (caso)
        {
            case "a-a-si-mismo": accion = $"desactivar:{adminPropio!.Id}"; break;
            case "b-ya-desactivado": accion = $"desactivar:{objetivo.Id}"; break;
            case "c-no-bloqueado": accion = $"desbloquear:{objetivo.Id}"; break;
            case "d-inexistente": accion = $"desactivar:{Guid.NewGuid()}"; break;
            case "e-accion-armada": accion = $"borrar:{objetivo.Id}"; break;
            default:
                // Lo desactivan en la base y, SIN adelantar el reloj, su sesión todavía no se revalidó.
                await MarcarInactivoAsync(app, adminPropio!.Id);
                accion = $"reactivar:{objetivo.Id}";
                break;
        }
        var antes = await RecargarAsync(app, objetivo.Id);

        var respuesta = await AccionAsync(cliente, accion);

        Assert.Equal(codigo, respuesta.StatusCode);
        Assert.Equal(mensaje, Alerta(await HtmlAsync(respuesta)));
        var despues = await RecargarAsync(app, objetivo.Id);
        Assert.Equal(antes.Activo, despues.Activo);
        Assert.Equal(antes.SecurityStamp, despues.SecurityStamp);
        Assert.Equal(antes.LockoutEnd, despues.LockoutEnd);
        if (adminPropio is not null && caso == "a-a-si-mismo")
            Assert.True((await RecargarAsync(app, adminPropio.Id)).Activo);
    }

    [Fact]
    [Trait("Story", "2.2")]
    [Trait("AC", "8")]
    [Trait("AC", "26")]
    public async Task Desactivar_SesionAbiertaDelUsuario_SeCortaAlRevalidar()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var propio = await CrearUsuarioAsync(app, [Roles.Recepcionista], ClavePropia);
        var clienteB = await IngresadoAsync(app, propio.UserName!, ClavePropia);
        var clienteA = await Admin1Async(app);

        var desactivacion = await AccionAsync(clienteA, $"desactivar:{propio.Id}");
        Assert.Equal(HttpStatusCode.OK, desactivacion.StatusCode);
        app.Reloj.Advance(TimeSpan.FromMinutes(6));   // pasa la revalidación de 5 minutos (2.1 AC 25)
        var pedido = await clienteB.GetAsync("/personal/recepcion/turnos");

        Assert.Equal(HttpStatusCode.Redirect, pedido.StatusCode);
        Assert.Equal("/personal/ingreso?ReturnUrl=%2Fpersonal%2Frecepcion%2Fturnos", Destino(pedido));
    }

    [Theory]
    [Trait("Story", "2.2")]
    [Trait("AC", "17")]
    [InlineData("alta")]
    [InlineData("acciones")]
    [InlineData("cambio")]
    public async Task Post_SinTokenAntiforgery_Devuelve400SinCambios(string formulario)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var nuevo = $"p-{Marca()}";
        var propio = await CrearUsuarioAsync(app, [Roles.Recepcionista], ClavePropia);
        HttpResponseMessage respuesta;
        if (formulario == "cambio")
        {
            using (var alcance = app.Services.CreateScope())
            {
                var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>();
                var cargado = (await usuarios.FindByIdAsync(propio.Id.ToString()))!;
                cargado.DebeCambiarContrasena = true;
                Assert.True((await usuarios.UpdateAsync(cargado)).Succeeded);
            }
            var cliente = await IngresadoAsync(app, propio.UserName!, ClavePropia);
            respuesta = await EnviarAsync(cliente, "/personal/cambiar-contrasena", "cambio-contrasena", new Dictionary<string, string>
            {
                ["Input.Actual"] = ClavePropia,
                ["Input.Nueva"] = "Nueva-Clave-2026",
                ["Input.Repetida"] = "Nueva-Clave-2026",
            }, conToken: false);
        }
        else
        {
            var cliente = await Admin1Async(app);
            respuesta = formulario == "alta"
                ? await AltaAsync(cliente, "Persona Nueva", nuevo, Roles.Medico, conToken: false)
                : await AccionAsync(cliente, $"desactivar:{propio.Id}", conToken: false);
        }

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Null(await BuscarAsync(app, nuevo));
        var despues = await RecargarAsync(app, propio.Id);
        Assert.True(despues.Activo);
        if (formulario == "cambio")
            Assert.True(despues.DebeCambiarContrasena);
    }

    [Fact]
    [Trait("Story", "2.2")]
    [Trait("AC", "18")]
    public async Task PostAlta_FallaLaBase_MuestraErrorGenericoSinContrasena()
    {
        await using var fabrica = new PersonalWebApplicationFactory(fixture);
        await using var app = fabrica.WithWebHostBuilder(b =>
            b.ConfigureTestServices(s => s.AddScoped<IGestionUsuariosPersonal, GestionUsuariosConBaseCaida>()));
        var cliente = Cliente(app);
        var ingreso = await IngresarAsync(cliente, PersonalWebApplicationFactory.Admin.Usuario, PersonalWebApplicationFactory.Admin.Contrasena);
        Assert.Equal(HttpStatusCode.Redirect, ingreso.StatusCode);

        var respuesta = await AltaAsync(cliente, "Persona Nueva", $"p-{Marca()}", Roles.Medico, accept: "text/html");

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        var html = await HtmlAsync(respuesta);
        Assert.Contains("Algo falló. Probá de nuevo en unos minutos.", html);
        Assert.DoesNotContain("Npgsql", html);
        Assert.DoesNotContain("secreto", html);
        Assert.DoesNotContain("contrasena-temporal", html);
        Assert.Contains(fabrica.Registro.Entradas, e =>
            e.Categoria.EndsWith("ManejadorGlobalDeErrores", StringComparison.Ordinal)
            && e.Nivel == LogLevel.Error && e.Excepcion is NpgsqlException);
    }

    // ---------------------------------------------------------------------------------------------
    // Story 2.3: contraseña olvidada, otra temporal desde A4.

    private const string TextoBotonRestablecer = "Nueva contraseña temporal";

    private static string TemporalDe(string html) =>
        Regex.Match(html, "<code id=\"contrasena-temporal\">(.*?)</code>").Groups[1].Value;

    // Lo que no tiene que cambiar cuando la acción se rechaza: sello, hash y marca.
    private static void SinCambios(UsuarioPersonal antes, UsuarioPersonal despues)
    {
        Assert.Equal(antes.SecurityStamp, despues.SecurityStamp);
        Assert.Equal(antes.PasswordHash, despues.PasswordHash);
        Assert.Equal(antes.DebeCambiarContrasena, despues.DebeCambiarContrasena);
    }

    [Fact]
    [Trait("Story", "2.3")]
    [Trait("AC", "12")]
    [Trait("AC", "17")]
    public async Task GetUsuarios_BotonNuevaContrasena_SoloEnActivosAjenos()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var cliente = await Admin1Async(app);
        var activo = await CrearUsuarioAsync(app, [Roles.Recepcionista], ClavePropia);
        var desactivado = await CrearUsuarioAsync(app, [Roles.Medico], ClavePropia, activo: false);
        var bloqueado = await CrearUsuarioAsync(app, [Roles.Recepcionista], ClavePropia);
        await FallarAsync(app, bloqueado.Id, 5);
        var sinRol = await CrearUsuarioAsync(app, [], ClavePropia);
        var variosRoles = await CrearUsuarioAsync(app, [Roles.Recepcionista, Roles.Medico], ClavePropia);
        var admin1 = (await BuscarAsync(app, PersonalWebApplicationFactory.Admin.Usuario))!;

        var html = await HtmlAsync(await cliente.GetAsync(A4));

        foreach (var usuario in new[] { activo, bloqueado, sinRol, variosRoles })
        {
            var fila = FilaDe(html, usuario.UserName!)!;
            Assert.Contains($"value=\"restablecer:{usuario.Id}\">{TextoBotonRestablecer}</button>", fila);
        }
        Assert.DoesNotContain("restablecer:", FilaDe(html, desactivado.UserName!)!);
        // AC 17: el botón nuevo convive con los de la 2.2, cada uno en su fila.
        Assert.Contains($"value=\"desactivar:{activo.Id}\"", FilaDe(html, activo.UserName!)!);
        Assert.Contains($"value=\"reactivar:{desactivado.Id}\"", FilaDe(html, desactivado.UserName!)!);
        Assert.Contains($"value=\"desbloquear:{bloqueado.Id}\"", FilaDe(html, bloqueado.UserName!)!);
        var filaAdmin1 = FilaDe(html, admin1.UserName!)!;
        Assert.DoesNotContain("restablecer:", filaAdmin1);
        Assert.Contains("(sos vos)", filaAdmin1);
    }

    [Fact]
    [Trait("Story", "2.3")]
    [Trait("AC", "1")]
    [Trait("AC", "2")]
    [Trait("AC", "4")]
    [Trait("AC", "13")]
    public async Task PostRestablecer_UsuarioActivo_MuestraLaTemporalUnaSolaVez()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var cliente = await Admin1Async(app);
        var propio = await CrearUsuarioAsync(app, [Roles.Recepcionista], ClavePropia);

        var respuesta = await AccionAsync(cliente, $"restablecer:{propio.Id}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(respuesta.Headers.CacheControl?.NoStore, "falta Cache-Control: no-store");
        var html = await HtmlAsync(respuesta);
        var temporal = TemporalDe(html);
        Assert.Equal(12, temporal.Length);
        Assert.Equal(
            $"Nueva contraseña temporal para {propio.UserName}: {temporal}. Anotala y dásela a la persona: no se vuelve a mostrar. La anterior ya no sirve y la tiene que cambiar en su próximo ingreso.",
            Estado(html));
        Assert.Contains("Debe cambiar la contraseña", FilaDe(html, propio.UserName!)!);
        Assert.All(app.Registro.Entradas, e => Assert.DoesNotContain(temporal, e.Mensaje + e.Excepcion));
        Assert.All(await DetallesAuditadosDeAsync(propio.Id), d => Assert.DoesNotContain(temporal, d));

        var despues = await HtmlAsync(await cliente.GetAsync(A4));
        Assert.DoesNotContain(temporal, despues);
        Assert.DoesNotContain("contrasena-temporal", despues);

        var otro = Cliente(app);
        var conLaAnterior = await IngresarAsync(otro, propio.UserName!, ClavePropia);
        Assert.Equal(HttpStatusCode.OK, conLaAnterior.StatusCode);
        Assert.Contains("Usuario o contraseña incorrectos.", await HtmlAsync(conLaAnterior));
        var conLaTemporal = await IngresarAsync(otro, propio.UserName!, temporal);
        Assert.Equal(HttpStatusCode.Redirect, conLaTemporal.StatusCode);
        Assert.Equal("/personal/cambiar-contrasena", Destino(conLaTemporal));
    }

    [Fact]
    [Trait("Story", "2.3")]
    [Trait("AC", "5")]
    [Trait("AC", "13")]
    public async Task PostRestablecer_UsuarioBloqueado_AvisaQueTambienQuedoDesbloqueado()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var cliente = await Admin1Async(app);
        var propio = await CrearUsuarioAsync(app, [Roles.Recepcionista], ClavePropia);
        await FallarAsync(app, propio.Id, 5);

        var respuesta = await AccionAsync(cliente, $"restablecer:{propio.Id}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var html = await HtmlAsync(respuesta);
        Assert.EndsWith(" Su usuario también quedó desbloqueado.", Estado(html));
        var fila = FilaDe(html, propio.UserName!)!;
        Assert.DoesNotContain("Bloqueado hasta las", fila);
        Assert.DoesNotContain("desbloquear:", fila);
    }

    [Theory]
    [Trait("Story", "2.3")]
    [Trait("AC", "6")]
    [Trait("AC", "7")]
    [Trait("AC", "8")]
    [Trait("AC", "9")]
    [InlineData("a-a-si-mismo", HttpStatusCode.BadRequest,
        "No podés generarte una contraseña temporal a vos mismo. Si querés otra, cambiala en /personal/cambiar-contrasena.")]
    [InlineData("b-desactivado", HttpStatusCode.Conflict, "Ese usuario está desactivado. Reactivalo antes de generarle otra contraseña.")]
    [InlineData("c-inexistente", HttpStatusCode.NotFound, "No encontramos ese usuario. Recargá la página.")]
    [InlineData("d-actor-desactivado", HttpStatusCode.Forbidden, "No tenés permiso para administrar usuarios.")]
    public async Task PostRestablecer_Errores_MuestranCartelConSuCodigo(string caso, HttpStatusCode codigo, string mensaje)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var objetivo = await CrearUsuarioAsync(app, [Roles.Recepcionista], ClavePropia, activo: caso != "b-desactivado");
        HttpClient cliente;
        UsuarioPersonal? adminPropio = null;
        if (caso is "a-a-si-mismo" or "d-actor-desactivado")
        {
            adminPropio = await CrearUsuarioAsync(app, [Roles.Administrador], "Admin-Propio-26");
            cliente = await IngresadoAsync(app, adminPropio.UserName!, "Admin-Propio-26");
        }
        else
            cliente = await Admin1Async(app);
        string accion;
        switch (caso)
        {
            case "a-a-si-mismo": accion = $"restablecer:{adminPropio!.Id}"; break;
            case "b-desactivado": accion = $"restablecer:{objetivo.Id}"; break;
            case "c-inexistente": accion = $"restablecer:{Guid.NewGuid()}"; break;
            default:
                // Lo desactivan en la base y, SIN adelantar el reloj, su sesión todavía no se revalidó.
                await MarcarInactivoAsync(app, adminPropio!.Id);
                accion = $"restablecer:{objetivo.Id}";
                break;
        }
        var antes = await RecargarAsync(app, objetivo.Id);
        var adminAntes = adminPropio is null ? null : await RecargarAsync(app, adminPropio.Id);

        var respuesta = await AccionAsync(cliente, accion);

        Assert.Equal(codigo, respuesta.StatusCode);
        Assert.Equal(mensaje, Alerta(await HtmlAsync(respuesta)));
        SinCambios(antes, await RecargarAsync(app, objetivo.Id));
        if (caso == "a-a-si-mismo")
            SinCambios(adminAntes!, await RecargarAsync(app, adminPropio!.Id));
    }

    [Fact]
    [Trait("Story", "2.3")]
    [Trait("AC", "3")]
    public async Task Restablecer_SesionAbiertaDelUsuario_SeCortaAlRevalidar()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var propio = await CrearUsuarioAsync(app, [Roles.Recepcionista], ClavePropia);
        var clienteB = await IngresadoAsync(app, propio.UserName!, ClavePropia);
        // Control: otra persona con su sesión abierta, a la que no se le genera nada, sigue adentro.
        var otro = await CrearUsuarioAsync(app, [Roles.Recepcionista], ClavePropia);
        var clienteC = await IngresadoAsync(app, otro.UserName!, ClavePropia);
        var clienteA = await Admin1Async(app);

        var restablecimiento = await AccionAsync(clienteA, $"restablecer:{propio.Id}");
        Assert.Equal(HttpStatusCode.OK, restablecimiento.StatusCode);
        app.Reloj.Advance(TimeSpan.FromMinutes(6));   // pasa la revalidación de 5 minutos (2.1 AC 25)
        var pedido = await clienteB.GetAsync("/personal/recepcion/turnos");
        var control = await clienteC.GetAsync("/personal/recepcion/turnos");

        Assert.Equal(HttpStatusCode.Redirect, pedido.StatusCode);
        Assert.Equal("/personal/ingreso?ReturnUrl=%2Fpersonal%2Frecepcion%2Fturnos", Destino(pedido));
        Assert.Equal(HttpStatusCode.OK, control.StatusCode);
    }

    [Theory]
    [Trait("Story", "2.3")]
    [Trait("AC", "14")]
    [InlineData("sin-token")]
    [InlineData("otro-rol")]
    public async Task PostRestablecer_SinTokenOOtroRol_NoCambiaNada(string caso)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var objetivo = await CrearUsuarioAsync(app, [Roles.Recepcionista], ClavePropia);
        var antes = await RecargarAsync(app, objetivo.Id);
        HttpResponseMessage respuesta;
        if (caso == "sin-token")
            respuesta = await AccionAsync(await Admin1Async(app), $"restablecer:{objetivo.Id}", conToken: false);
        else
        {
            // POST armado a mano, con un token válido de su propia sesión (de su página de inicio).
            var recepcion = PersonalWebApplicationFactory.Fijos.Single(f => f.Usuario == "recepcion1");
            var cliente = await IngresadoAsync(app, recepcion.Usuario, recepcion.Contrasena);
            respuesta = await cliente.PostAsync(A4, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["_handler"] = "acciones-usuarios",
                ["__RequestVerificationToken"] = await TokenDeAsync(cliente, "/personal/recepcion/turnos"),
                ["Accion"] = $"restablecer:{objetivo.Id}",
            }));
        }

        if (caso == "sin-token")
            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        else
        {
            Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
            Assert.StartsWith("/personal/acceso-denegado", Destino(respuesta));
        }
        SinCambios(antes, await RecargarAsync(app, objetivo.Id));
    }

    [Fact]
    [Trait("Story", "2.3")]
    [Trait("AC", "15")]
    [Trait("AC", "16")]
    public async Task PostRestablecer_FallaLaBase_MuestraErrorGenericoSinContrasena()
    {
        await using var fabrica = new PersonalWebApplicationFactory(fixture);
        await using var app = fabrica.WithWebHostBuilder(b =>
            b.ConfigureTestServices(s => s.AddScoped<IGestionUsuariosPersonal, GestionUsuariosConBaseCaida>()));
        var cliente = Cliente(app);
        var ingreso = await IngresarAsync(cliente, PersonalWebApplicationFactory.Admin.Usuario, PersonalWebApplicationFactory.Admin.Contrasena);
        Assert.Equal(HttpStatusCode.Redirect, ingreso.StatusCode);

        // Sin Accept: text/html el manejador de la 1.2 responde problem+json (lección de la 2.1 T15).
        var respuesta = await EnviarAsync(cliente, A4, "acciones-usuarios",
            new Dictionary<string, string> { ["Accion"] = $"restablecer:{Guid.NewGuid()}" }, accept: "text/html");

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        var html = await HtmlAsync(respuesta);
        Assert.Contains("Algo falló. Probá de nuevo en unos minutos.", html);
        Assert.DoesNotContain("Npgsql", html);
        Assert.DoesNotContain("secreto", html);
        Assert.DoesNotContain("contrasena-temporal", html);
        Assert.Contains(fabrica.Registro.Entradas, e =>
            e.Categoria.EndsWith("ManejadorGlobalDeErrores", StringComparison.Ordinal)
            && e.Nivel == LogLevel.Error && e.Excepcion is NpgsqlException);
    }
}
