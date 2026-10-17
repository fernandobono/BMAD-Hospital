using System.Net;
using System.Text.RegularExpressions;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Web.Tests.Personal;
using Npgsql;
using static HospitalTurnos.Web.Tests.Personal.FormularioHttp;

namespace HospitalTurnos.Web.Tests.Catalogo;

// A1 con HttpClient sobre la app real (token y cookies incluidos), como lo usa un navegador: es SSR sin
// JavaScript propio. admin1, recepcion1 y medico1 sólo ingresan y navegan; los usuarios a vincular son
// propios de cada prueba. Los médicos no se borran: cada prueba usa su marca (apellido Prueba<marca>,
// matrícula T-<MARCA>) y nadie cuenta filas sin ella.
[Collection("Postgres")]
public partial class MedicosHttpTests(PostgresFixture fixture)
{
    private const string A1 = "/personal/admin/medicos";
    private const string Nuevo = "/personal/admin/medicos/nuevo";
    private const string ClavePropia = "Clave-Propia-2026";
    private const string Formulario = "medico";

    private static string Marca() => Guid.NewGuid().ToString("N")[..8];
    private static string Ficha(long id) => $"{A1}/{id}";

    private static async Task<HttpClient> IngresadoAsync(PersonalWebApplicationFactory app, UsuarioFijo fijo,
        bool seguirRedirecciones = false)
    {
        var cliente = Cliente(app, seguirRedirecciones: seguirRedirecciones);
        var ingreso = await IngresarAsync(cliente, fijo.Usuario, fijo.Contrasena);
        if (!seguirRedirecciones)
            Assert.NotEmpty(CookiesPersonal(ingreso));
        return cliente;
    }

    private static Task<HttpClient> Admin1Async(PersonalWebApplicationFactory app) =>
        IngresadoAsync(app, PersonalWebApplicationFactory.Admin);

    private static Dictionary<string, string> Campos(string nombre, string apellido, string matricula, long? especialidad,
        Guid? usuario = null) => new()
    {
        ["Input.Nombre"] = nombre,
        ["Input.Apellido"] = apellido,
        ["Input.Matricula"] = matricula,
        ["Input.EspecialidadId"] = especialidad?.ToString() ?? "",
        ["Input.UsuarioPersonalId"] = usuario?.ToString() ?? "",
    };

    private static async Task<string> HtmlAsync(HttpResponseMessage respuesta) =>
        WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());

    // La fila <tr> que contiene ese texto (null si no está).
    private static string? FilaCon(string html, string texto)
    {
        var celda = html.IndexOf(texto, StringComparison.Ordinal);
        if (celda < 0)
            return null;
        var inicio = html.LastIndexOf("<tr", celda, StringComparison.Ordinal);
        var fin = html.IndexOf("</tr>", celda, StringComparison.Ordinal);
        return html[inicio..fin];
    }

    // Las opciones de un <select> por su name: valor → texto.
    private static Dictionary<string, string> OpcionesDe(string html, string nombre)
    {
        var select = Regex.Match(html, $"<select[^>]*name=\"{Regex.Escape(nombre)}\"[^>]*>(.*?)</select>", RegexOptions.Singleline);
        Assert.True(select.Success, $"No está el <select> {nombre}.");
        return Opcion().Matches(select.Groups[1].Value)
            .ToDictionary(o => o.Groups[1].Value, o => o.Groups[2].Value.Trim());
    }

    private async Task<long> ContarAsync(string sql, params (string Nombre, object Valor)[] parametros)
    {
        await using var conexion = new NpgsqlConnection(fixture.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        foreach (var (nombre, valor) in parametros)
            comando.Parameters.AddWithValue(nombre, valor);
        return Convert.ToInt64(await comando.ExecuteScalarAsync());
    }

    private Task<long> MedicosConMarcaAsync(string m) =>
        ContarAsync("SELECT count(*) FROM medicos WHERE apellido LIKE @a", ("a", $"%{m}%"));

    private Task<long> AltasAuditadasConAsync(string m) =>
        ContarAsync("SELECT count(*) FROM registros_auditoria WHERE accion = 'medico.creado' AND detalle::text LIKE @p",
            ("p", $"%{m}%"));

    private Task<long> EdicionesAuditadasDeAsync(long id) =>
        ContarAsync("SELECT count(*) FROM registros_auditoria WHERE accion = 'medico.editado' AND entidad_id = @id",
            ("id", id.ToString()));

    private Task<string> ApellidoDeAsync(long id) =>
        ContarTextoAsync("SELECT apellido FROM medicos WHERE id = @id", id);

    private async Task<string> ContarTextoAsync(string sql, long id)
    {
        await using var conexion = new NpgsqlConnection(fixture.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("id", id);
        return (string)(await comando.ExecuteScalarAsync())!;
    }

    [Fact]
    [Trait("Story", "3.2")]
    [Trait("AC", "1")]
    [Trait("AC", "4")]
    [Trait("AC", "24")]
    public async Task GetMedicos_Administrador_MuestraListadoOrdenadoConSinAgenda()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var m = Marca();
        var especialidad = await app.CrearEspecialidadAsync(nombre: $"Esp{m}");
        var beto = await app.CrearMedicoAsync("Beto", $"Prueba{m}", $"T-{m}-2", especialidad);
        var ana = await app.CrearMedicoAsync("Ana", $"Prueba{m}", $"T-{m}-1", especialidad);
        var cliente = await Admin1Async(app);

        var respuesta = await cliente.GetAsync($"{A1}?buscar={m}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var html = await HtmlAsync(respuesta);
        Assert.Contains("<h1>Médicos</h1>", html);
        Assert.Contains($"href=\"{Nuevo}\"", html);
        Assert.Contains(">Nuevo médico<", html);
        foreach (var columna in new[] { "Médico", "Matrícula", "Especialidad", "Estado de agenda" })
            Assert.Contains($"<th>{columna}</th>", html);
        var filaAna = FilaCon(html, $"Prueba{m}, Ana");
        var filaBeto = FilaCon(html, $"Prueba{m}, Beto");
        Assert.NotNull(filaAna);
        Assert.NotNull(filaBeto);
        Assert.True(html.IndexOf(filaAna, StringComparison.Ordinal) < html.IndexOf(filaBeto, StringComparison.Ordinal),
            "El listado no está ordenado por apellido y nombre.");
        Assert.Contains($"href=\"{Ficha(ana)}\"", filaAna);
        Assert.Contains($"href=\"{Ficha(beto)}\"", filaBeto);
        Assert.Contains($"T-{m.ToUpperInvariant()}-1", filaAna);
        Assert.Contains($"Esp{m}", filaAna);
        Assert.Contains(">Sin agenda<", filaAna);
        Assert.Contains(">Sin agenda<", filaBeto);
        Assert.DoesNotContain("Esta sección se habilita", html);
    }

    [Theory]
    [Trait("Story", "3.2")]
    [Trait("AC", "2")]
    [Trait("AC", "3")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetMedicos_BusquedaSinResultadosOLarga_MuestraMensajeYConservaElTexto(bool larga)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var m = Marca();
        await app.CrearMedicoAsync("Ana", $"Prueba{m}", $"T-{m}", await app.CrearEspecialidadAsync());   // hay al menos uno
        var cliente = await Admin1Async(app);
        var texto = larga ? new string('x', 101) : $"nadie-{Guid.NewGuid():N}";

        var respuesta = await cliente.GetAsync($"{A1}?buscar={Uri.EscapeDataString(texto)}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var html = await HtmlAsync(respuesta);
        Assert.Equal(texto, ValorDe(html, "buscar"));
        if (larga)
        {
            Assert.Contains("La búsqueda tiene como máximo 100 caracteres.", html);
            Assert.Contains("<table", html);
            Assert.NotNull(FilaCon(html, $"Prueba{m}, Ana"));   // no filtró
        }
        else
        {
            Assert.Contains($"No hay médicos que coincidan con «{texto}». Probá con otro apellido o matrícula.", html);
            Assert.DoesNotContain("<table", html);
        }
    }

    [Fact]
    [Trait("Story", "3.2")]
    [Trait("AC", "5")]
    [Trait("AC", "6")]
    public async Task PostNuevo_DatosValidos_RedirigeALaFichaYAnunciaElAlta()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var m = Marca();
        var activa = await app.CrearEspecialidadAsync();
        var desactivada = await app.CrearEspecialidadAsync(activa: false);
        var libre = await CrearUsuarioAsync(app, [Roles.Medico], ClavePropia, nombre: $"Libre {m}");
        var vinculado = await CrearUsuarioAsync(app, [Roles.Medico], ClavePropia, nombre: $"Vinculado {m}");
        await app.CrearMedicoAsync("Otro", $"Prueba{m}", $"T-{m}-0", activa, vinculado.Id);
        var cliente = await Admin1Async(app);

        var formulario = await HtmlAsync(await cliente.GetAsync(Nuevo));
        var especialidades = OpcionesDe(formulario, "Input.EspecialidadId");
        var usuarios = OpcionesDe(formulario, "Input.UsuarioPersonalId");
        Assert.Equal("Elegí una especialidad", especialidades[""]);
        Assert.Contains(activa.ToString(), especialidades.Keys);
        Assert.DoesNotContain(desactivada.ToString(), especialidades.Keys);
        Assert.Equal("Sin usuario", usuarios[""]);
        Assert.Equal($"Libre {m} ({libre.UserName})", usuarios[libre.Id.ToString()]);
        Assert.DoesNotContain(vinculado.Id.ToString(), usuarios.Keys);
        foreach (var etiqueta in new[] { "Nombre", "Apellido", "Matrícula", "Especialidad", "Usuario del sistema" })
            Assert.Contains($">{etiqueta}</label>", formulario);

        var alta = await EnviarAsync(cliente, Nuevo, Formulario,
            Campos(" Ana ", $"Prueba{m}", $" t-{m}  1 ", activa, libre.Id));

        Assert.Equal(HttpStatusCode.Redirect, alta.StatusCode);
        var destino = Destino(alta);
        Assert.Matches($"^{A1}/\\d+\\?resultado=alta$", destino);
        var ficha = await HtmlAsync(await cliente.GetAsync(destino));
        Assert.Equal("Médico dado de alta.", Estado(ficha));
        Assert.Contains($"<h1>Prueba{m}, Ana</h1>", ficha);
        Assert.Equal($"T-{m.ToUpperInvariant()} 1", ValorDe(ficha, "Input.Matricula"));
    }

    [Theory]
    [Trait("Story", "3.2")]
    [Trait("AC", "7")]
    [InlineData("vacio")]
    [InlineData("nombre-largo")]
    [InlineData("matricula-invalida")]
    public async Task PostNuevo_CamposInvalidos_MuestraMensajePorCampoSinGuardar(string caso)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var m = Marca();
        var especialidad = await app.CrearEspecialidadAsync();
        var cliente = await Admin1Async(app);
        var (campos, mensajes) = caso switch
        {
            "vacio" => (Campos("", "", "", null),
                new[] { "Ingresá el nombre.", "Ingresá el apellido.", "Ingresá la matrícula.", "Elegí una especialidad." }),
            "nombre-largo" => (Campos(new string('a', 101), $"Prueba{m}", $"T-{m}", especialidad),
                new[] { "El nombre tiene como máximo 100 caracteres." }),
            _ => (Campos("Ana", $"Prueba{m}", "MP#12", especialidad),
                new[] { "La matrícula tiene hasta 20 caracteres: letras, números, espacios, puntos, guiones o barras." }),
        };

        var respuesta = await EnviarAsync(cliente, Nuevo, Formulario, campos);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var html = await HtmlAsync(respuesta);
        foreach (var mensaje in mensajes)
            Assert.Contains($">{mensaje}</div>", html);   // debajo del campo (ValidationMessage)
        Assert.Null(Alerta(html));                        // no es un cartel de regla
        Assert.Equal(campos["Input.Nombre"], ValorDe(html, "Input.Nombre"));
        Assert.Equal(campos["Input.Apellido"], ValorDe(html, "Input.Apellido"));
        Assert.Equal(campos["Input.Matricula"], ValorDe(html, "Input.Matricula"));
        Assert.Equal(0, await MedicosConMarcaAsync(m));
    }

    [Theory]
    [Trait("Story", "3.2")]
    [Trait("AC", "8")]
    [Trait("AC", "9")]
    [Trait("AC", "11")]
    [Trait("AC", "12")]
    [InlineData("matricula-repetida", HttpStatusCode.Conflict)]
    [InlineData("especialidad-desactivada", HttpStatusCode.BadRequest)]
    [InlineData("usuario-recepcionista", HttpStatusCode.BadRequest)]
    [InlineData("usuario-vinculado", HttpStatusCode.Conflict)]
    public async Task PostNuevo_ReglaQueFalla_RespondeConElCodigoYElCartelConservandoLosDatos(string caso,
        HttpStatusCode codigo)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var m = Marca();
        var activa = await app.CrearEspecialidadAsync();
        var matricula = $"T-{m.ToUpperInvariant()}";
        var vinculado = await CrearUsuarioAsync(app, [Roles.Medico], ClavePropia);
        await app.CrearMedicoAsync("Existente", $"Existente{m}", $"{matricula}-E", activa, vinculado.Id);
        var cliente = await Admin1Async(app);
        var (campos, mensaje) = caso switch
        {
            "matricula-repetida" => (Campos("Ana", $"Prueba{m}", $" {matricula.ToLowerInvariant()}-e ", activa),
                $"Ya hay un médico con la matrícula {matricula}-E."),
            "especialidad-desactivada" => (Campos("Ana", $"Prueba{m}", matricula, await app.CrearEspecialidadAsync(activa: false)),
                "La especialidad elegida no existe o está desactivada. Elegí otra."),
            "usuario-recepcionista" => (Campos("Ana", $"Prueba{m}", matricula, activa,
                    (await CrearUsuarioAsync(app, [Roles.Recepcionista], ClavePropia)).Id),
                "El usuario elegido no se puede vincular: tiene que estar activo y tener el rol Médico."),
            _ => (Campos("Ana", $"Prueba{m}", matricula, activa, vinculado.Id),
                $"El usuario elegido ya está vinculado al médico Existente{m}, Existente."),
        };

        var respuesta = await EnviarAsync(cliente, Nuevo, Formulario, campos);

        Assert.Equal(codigo, respuesta.StatusCode);
        var html = await HtmlAsync(respuesta);
        Assert.Equal(mensaje, Alerta(html));
        Assert.Equal("Ana", ValorDe(html, "Input.Nombre"));
        Assert.Equal($"Prueba{m}", ValorDe(html, "Input.Apellido"));
        Assert.Equal(0, await ContarAsync("SELECT count(*) FROM medicos WHERE apellido = @a", ("a", $"Prueba{m}")));
        Assert.Equal(0, await ContarAsync(
            "SELECT count(*) FROM registros_auditoria WHERE accion = 'medico.creado' AND detalle->>'apellido' = @a",
            ("a", $"Prueba{m}")));
    }

    [Fact]
    [Trait("Story", "3.2")]
    [Trait("AC", "10")]
    public async Task Nuevo_SinEspecialidadesActivas_AvisaYNoOfreceElFormulario()
    {
        // Base recién creada (todas las migraciones, sin especialidades): el aviso depende de la base vacía.
        var pg = new PostgresFixture();
        await pg.InitializeAsync();
        try
        {
            await using var app = new PersonalWebApplicationFactory(pg);
            var cliente = await Admin1Async(app);

            var respuesta = await cliente.GetAsync(Nuevo);

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            var html = await HtmlAsync(respuesta);
            Assert.Contains("No hay especialidades activas. Creá una en Especialidades antes de dar de alta un médico.", html);
            Assert.Contains("href=\"/personal/admin/especialidades\"", html);
            Assert.DoesNotContain($"value=\"{Formulario}\"", html);   // ningún <form> con el _handler del alta
            Assert.DoesNotContain("name=\"Input.Nombre\"", html);
        }
        finally
        {
            await pg.DisposeAsync();
        }
    }

    [Fact]
    [Trait("Story", "3.2")]
    [Trait("AC", "15")]
    [Trait("AC", "16")]
    public async Task PostFicha_EditarDatos_GuardaYAnunciaLosCambios()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var m = Marca();
        var especialidad = await app.CrearEspecialidadAsync();
        var id = await app.CrearMedicoAsync("Ana", $"Prueba{m}", $"T-{m}", especialidad);
        var cliente = await Admin1Async(app);

        var ficha = await HtmlAsync(await cliente.GetAsync(Ficha(id)));
        Assert.Contains($"<h1>Prueba{m}, Ana</h1>", ficha);
        Assert.Equal("Ana", ValorDe(ficha, "Input.Nombre"));
        Assert.Equal($"Prueba{m}", ValorDe(ficha, "Input.Apellido"));
        Assert.Equal($"T-{m.ToUpperInvariant()}", ValorDe(ficha, "Input.Matricula"));
        Assert.Contains($"value=\"{especialidad}\" selected", ficha);
        Assert.Contains("Sin agenda", ficha);
        Assert.Contains($"href=\"{A1}\"", ficha);

        var edicion = await EnviarAsync(cliente, Ficha(id), Formulario,
            Campos("Ana", $"Otra{m}", $"T-{m}", especialidad));

        Assert.Equal(HttpStatusCode.Redirect, edicion.StatusCode);
        Assert.Equal($"{Ficha(id)}?resultado=edicion", Destino(edicion));
        var despues = await HtmlAsync(await cliente.GetAsync(Destino(edicion)));
        Assert.Equal("Cambios guardados.", Estado(despues));
        Assert.Contains($"<h1>Otra{m}, Ana</h1>", despues);

        // Sin cambios: no guarda y lo dice.
        var igual = await EnviarAsync(cliente, Ficha(id), Formulario, Campos("Ana", $"Otra{m}", $"t-{m}", especialidad));
        Assert.Equal($"{Ficha(id)}?resultado=sin-cambios", Destino(igual));
        Assert.Equal("No había cambios para guardar.", Estado(await HtmlAsync(await cliente.GetAsync(Destino(igual)))));
        Assert.Equal(1, await EdicionesAuditadasDeAsync(id));
    }

    [Theory]
    [Trait("Story", "3.2")]
    [Trait("AC", "18")]
    [InlineData("GET")]
    [InlineData("POST")]
    public async Task Ficha_MedicoInexistente_Responde404ConEnlaceAlListado(string metodo)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var especialidad = await app.CrearEspecialidadAsync();
        var cliente = await Admin1Async(app);
        var ruta = Ficha(long.MaxValue);

        HttpResponseMessage respuesta;
        if (metodo == "GET")
            respuesta = await cliente.GetAsync(ruta);
        else
        {
            // El token y el _handler se toman de /nuevo: la ficha de un id que no existe no tiene formulario.
            var campos = Campos("Ana", "Prueba", "T-404", especialidad);
            campos["_handler"] = Formulario;
            campos["__RequestVerificationToken"] = await TokenDeAsync(cliente, Nuevo);
            respuesta = await cliente.PostAsync(ruta, new FormUrlEncodedContent(campos));
        }

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        var html = await HtmlAsync(respuesta);
        Assert.Contains("No existe el médico pedido.", html);
        Assert.Contains($"href=\"{A1}\"", html);
        Assert.Contains(">Volver a Médicos<", html);
        Assert.DoesNotContain("Algo falló", html);
    }

    [Theory]
    [Trait("Story", "3.2")]
    [Trait("AC", "19")]
    [InlineData("recepcion1", "nuevo")]
    [InlineData("recepcion1", "ficha")]
    [InlineData("medico1", "nuevo")]
    [InlineData("medico1", "ficha")]
    [InlineData("", "nuevo")]
    [InlineData("", "ficha")]
    public async Task RutasDeMedicos_SinRolAdministradorOSinSesion_NoEntran(string usuario, string pagina)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var m = Marca();
        var id = await app.CrearMedicoAsync("Ana", $"Prueba{m}", $"T-{m}", await app.CrearEspecialidadAsync());
        var ruta = pagina == "nuevo" ? Nuevo : Ficha(id);

        if (usuario == "")
        {
            var respuesta = await Cliente(app).GetAsync(ruta);
            Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
            Assert.Equal($"{RutaIngreso}?ReturnUrl={Uri.EscapeDataString(ruta)}", Destino(respuesta));
            return;
        }
        var fijo = PersonalWebApplicationFactory.Fijos.Single(f => f.Usuario == usuario);
        var navegador = await IngresadoAsync(app, fijo, seguirRedirecciones: true);

        var final = await navegador.GetAsync(ruta);

        Assert.Equal(HttpStatusCode.Forbidden, final.StatusCode);
        Assert.Contains("No tenés permiso para entrar a esta sección.", await HtmlAsync(final));
    }

    [Theory]
    [Trait("Story", "3.2")]
    [Trait("AC", "21")]
    [InlineData("alta")]
    [InlineData("ficha")]
    public async Task PostMedico_SinTokenAntiforgery_Responde400SinGuardar(string caso)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var m = Marca();
        var especialidad = await app.CrearEspecialidadAsync();
        var id = await app.CrearMedicoAsync("Ana", $"Existente{m}", $"T-{m}-E", especialidad);
        var cliente = await Admin1Async(app);
        var (ruta, campos) = caso == "alta"
            ? (Nuevo, Campos("Ana", $"Prueba{m}", $"T-{m}", especialidad))
            : (Ficha(id), Campos("Ana", $"Cambiado{m}", $"T-{m}-E", especialidad));

        var respuesta = await EnviarAsync(cliente, ruta, Formulario, campos, conToken: false);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(0, await ContarAsync("SELECT count(*) FROM medicos WHERE apellido = @a", ("a", $"Prueba{m}")));
        Assert.Equal(0, await AltasAuditadasConAsync(m));
        Assert.Equal($"Existente{m}", await ApellidoDeAsync(id));
        Assert.Equal(0, await EdicionesAuditadasDeAsync(id));
    }

    [Fact]
    [Trait("Story", "3.2")]
    [Trait("AC", "14")]
    public async Task Usuarios_ColumnaMedicoVinculado_MuestraElMedicoOSinVincular()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var m = Marca();
        var vinculado = await CrearUsuarioAsync(app, [Roles.Medico], ClavePropia);
        var sinVinculo = await CrearUsuarioAsync(app, [Roles.Medico], ClavePropia);
        var recepcionista = await CrearUsuarioAsync(app, [Roles.Recepcionista], ClavePropia);
        await app.CrearMedicoAsync("Ana", $"Prueba{m}", $"T-{m}", await app.CrearEspecialidadAsync(), vinculado.Id);
        var cliente = await Admin1Async(app);

        var html = await HtmlAsync(await cliente.GetAsync("/personal/admin/usuarios"));

        Assert.Contains("<th>Médico vinculado</th>", html);
        var filaVinculado = FilaCon(html, $">{vinculado.UserName}<");
        var filaSinVinculo = FilaCon(html, $">{sinVinculo.UserName}<");
        var filaRecepcion = FilaCon(html, $">{recepcionista.UserName}<");
        Assert.NotNull(filaVinculado);
        Assert.NotNull(filaSinVinculo);
        Assert.NotNull(filaRecepcion);
        Assert.Contains($"Prueba{m}, Ana", filaVinculado);
        Assert.Contains("Sin vincular", filaSinVinculo);
        Assert.DoesNotContain("Sin vincular", filaRecepcion);
        Assert.DoesNotContain($"Prueba{m}", filaRecepcion);
    }

    // Blazor dibuja la opción vacía como <option value> (sin ="", el navegador la toma como valor vacío).
    [GeneratedRegex("<option[^>]*?value(?:=\"([^\"]*)\")?[^>]*>([^<]*)</option>", RegexOptions.IgnoreCase)]
    private static partial Regex Opcion();
}
