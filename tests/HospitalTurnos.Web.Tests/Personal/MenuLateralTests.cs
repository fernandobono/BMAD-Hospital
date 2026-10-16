using System.Security.Claims;
using Bunit;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Web.Components.Personal;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalTurnos.Web.Tests.Personal;

// El menú es lo único que depende del rol al dibujarse: se prueba como componente, sin HTTP.
public class MenuLateralTests : BunitContext
{
    // El botón Salir va dentro de un formulario con token; bUnit no trae proveedor de tokens.
    private sealed class TokenDePrueba : AntiforgeryStateProvider
    {
        public override AntiforgeryRequestToken? GetAntiforgeryToken() =>
            new("token-de-prueba", "__RequestVerificationToken");
    }

    public static TheoryData<string, string[]> SeccionesPorRol => new()
    {
        { Roles.Recepcionista, ["/personal/recepcion/turnos", "/personal/agendas", "/personal/para-avisar"] },
        { Roles.Medico, ["/personal/medico/mi-agenda", "/personal/medico/mi-propuesta"] },
        {
            Roles.Administrador,
            [
                "/personal/admin/medicos", "/personal/admin/propuestas", "/personal/admin/especialidades",
                "/personal/admin/bloqueos", "/personal/admin/usuarios", "/personal/admin/auditoria",
                "/personal/para-avisar", "/personal/agendas",
            ]
        },
    };

    [Theory]
    [Trait("Story", "2.1")]
    [Trait("AC", "14")]
    [MemberData(nameof(SeccionesPorRol))]
    public void MenuLateral_SegunRol_MuestraSoloSusSeccionesEnElOrdenDeUx(string rol, string[] rutas)
    {
        Services.AddSingleton<AntiforgeryStateProvider, TokenDePrueba>();
        var autorizacion = AddAuthorization();
        autorizacion.SetAuthorized("carla");
        autorizacion.SetRoles(rol);
        autorizacion.SetClaims(new Claim("nombre_completo", "Carla Gómez"));

        var menu = Render<MenuLateral>();

        var enlaces = menu.FindAll("nav a").Select(a => a.GetAttribute("href")).ToList();
        Assert.Equal(rutas, enlaces);
        Assert.Contains("Carla Gómez", menu.Markup);
        var formulario = menu.Find("form[action='/personal/salir']");
        Assert.Equal("post", formulario.GetAttribute("method"), ignoreCase: true);
        Assert.Contains("Salir", formulario.QuerySelector("button")!.TextContent);
    }
}
