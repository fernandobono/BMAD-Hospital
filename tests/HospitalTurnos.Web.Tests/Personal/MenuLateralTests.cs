using System.Security.Claims;
using Bunit;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Web.Components.Personal;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalTurnos.Web.Tests.Personal;

// El menú es lo único que depende del rol al dibujarse: se prueba como componente, sin HTTP.
public class MenuLateralTests : BunitContext
{
    // Story 3.3 (AC 13): el orden es el de los grupos del menú; el nombre y "Cerrar sesión" pasaron a la barra
    // superior (MarcoPersonalTests).
    public static TheoryData<string, string[]> SeccionesPorRol => new()
    {
        { Roles.Recepcionista, ["/personal/recepcion/turnos", "/personal/agendas", "/personal/para-avisar"] },
        { Roles.Medico, ["/personal/medico/mi-agenda", "/personal/medico/mi-propuesta"] },
        {
            Roles.Administrador,
            [
                "/personal/agendas", "/personal/admin/propuestas", "/personal/admin/bloqueos", "/personal/para-avisar",
                "/personal/admin/medicos", "/personal/admin/especialidades",
                "/personal/admin/usuarios", "/personal/admin/auditoria",
            ]
        },
    };

    [Theory]
    [Trait("Story", "2.1")]
    [Trait("AC", "14")]
    [Trait("Story", "3.3")]
    [Trait("AC", "13")]
    [MemberData(nameof(SeccionesPorRol))]
    public void MenuLateral_SegunRol_MuestraSoloSusSeccionesEnElOrdenDeUx(string rol, string[] rutas)
    {
        var autorizacion = AddAuthorization();
        autorizacion.SetAuthorized("carla");
        autorizacion.SetRoles(rol);
        autorizacion.SetClaims(new Claim("nombre_completo", "Carla Gómez"));

        var menu = Render<MenuLateral>();

        var enlaces = menu.FindAll("nav a").Select(a => a.GetAttribute("href")).ToList();
        Assert.Equal(rutas, enlaces);
    }
}
