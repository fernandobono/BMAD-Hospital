using System.Net;
using HospitalTurnos.Web.Personal;
using HospitalTurnos.Web.Tests.Personal;
using static HospitalTurnos.Web.Tests.Personal.FormularioHttp;

namespace HospitalTurnos.Web.Tests.Catalogo;

// Story 3.3 (AC 92): A1, A2 y A4 son interactivas sin prerender. Por HTTP se conserva lo que el pedido decide: que la
// página carga con el marco estático y el marcador del circuito. Los formularios SSR de la 3.1 ya no existen: su
// comportamiento se prueba como componente (EspecialidadesInteractivaTests) y los permisos en MedicosHttpTests (T58).
[Collection("Postgres")]
public class EspecialidadesHttpTests(PostgresFixture fixture)
{
    [Theory]
    [Trait("Story", "3.3")]
    [Trait("AC", "1")]
    [Trait("AC", "92")]
    [InlineData(RutasPersonal.Especialidades)]
    [InlineData(RutasPersonal.Medicos)]
    [InlineData(RutasPersonal.NuevoMedico)]
    [InlineData(RutasPersonal.Usuarios)]
    public async Task PaginasAbm_Administrador_CarganElMarcoYElCircuito(string ruta)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var cliente = Cliente(app);
        await IngresarAsync(cliente, PersonalWebApplicationFactory.Admin.Usuario, PersonalWebApplicationFactory.Admin.Contrasena);

        var respuesta = await cliente.GetAsync(ruta);
        var html = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("class=\"barra-superior\"", html);                      // el marco, estático
        Assert.Contains("<!--Blazor:{\"type\":\"server\"", html);                 // la página, en el circuito
        Assert.DoesNotContain("name=\"_handler\"", html);                         // sin formularios SSR
    }
}
