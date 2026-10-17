using System.Net;
using HospitalTurnos.Web.Personal;
using HospitalTurnos.Web.Tests.Personal;
using static HospitalTurnos.Web.Tests.Personal.FormularioHttp;

namespace HospitalTurnos.Web.Tests.Catalogo;

// Los permisos de las pantallas ABM los decide el pedido HTTP, antes de abrir el circuito: sin sesión, 302 al ingreso;
// con otro rol, 403 con el texto de siempre (2.1, 3.1, 3.2). Story 3.3 (AC 89, 92): se conservan por HTTP; los
// formularios SSR de la 3.2 ya no existen y su comportamiento se prueba como componente (MedicosInteractivaTests).
[Collection("Postgres")]
public class MedicosHttpTests(PostgresFixture fixture)
{
    private static string Marca() => Guid.NewGuid().ToString("N")[..8];

    [Theory]
    [Trait("Story", "3.3")]
    [Trait("AC", "89")]
    [Trait("Story", "3.2")]
    [Trait("AC", "19")]
    [Trait("Story", "3.1")]
    [Trait("AC", "13")]
    [Trait("Story", "2.2")]
    [Trait("AC", "2")]
    [InlineData("", "medicos")]
    [InlineData("", "nuevo")]
    [InlineData("", "ficha")]
    [InlineData("", "especialidades")]
    [InlineData("", "usuarios")]
    [InlineData("recepcion1", "medicos")]
    [InlineData("recepcion1", "nuevo")]
    [InlineData("recepcion1", "ficha")]
    [InlineData("recepcion1", "especialidades")]
    [InlineData("recepcion1", "usuarios")]
    [InlineData("medico1", "ficha")]
    [InlineData("medico1", "usuarios")]
    public async Task PaginasAbm_SinSesionOConOtroRol_SiguenEn302O403(string usuario, string pantalla)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var m = Marca();
        var id = await app.CrearMedicoAsync("Ana", $"Prueba{m}", $"T-{m}", await app.CrearEspecialidadAsync());
        var ruta = pantalla switch
        {
            "medicos" => RutasPersonal.Medicos,
            "nuevo" => RutasPersonal.NuevoMedico,
            "ficha" => RutasPersonal.FichaMedico(id),
            "especialidades" => RutasPersonal.Especialidades,
            _ => RutasPersonal.Usuarios,
        };

        if (usuario == "")
        {
            var respuesta = await Cliente(app).GetAsync(ruta);
            Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
            Assert.Equal($"{RutaIngreso}?ReturnUrl={Uri.EscapeDataString(ruta)}", Destino(respuesta));
            return;
        }
        var fijo = PersonalWebApplicationFactory.Fijos.Single(f => f.Usuario == usuario);
        var navegador = Cliente(app, seguirRedirecciones: true);
        await IngresarAsync(navegador, fijo.Usuario, fijo.Contrasena);

        var final = await navegador.GetAsync(ruta);

        Assert.Equal(HttpStatusCode.Forbidden, final.StatusCode);
        Assert.Contains("No tenés permiso para entrar a esta sección.", WebUtility.HtmlDecode(await final.Content.ReadAsStringAsync()));
    }
}
