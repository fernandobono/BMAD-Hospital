using Bunit;
using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Catalogo;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Web.Components.Personal.Administracion;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalTurnos.Web.Tests.Catalogo;

// El estado vacío no se puede ver por HTTP en una base compartida (siempre hay especialidades de otras
// pruebas): se prueba como componente, con un catálogo de prueba que no tiene ninguna.
public class EspecialidadesPaginaTests : BunitContext
{
    private sealed class TokenDePrueba : AntiforgeryStateProvider
    {
        public override AntiforgeryRequestToken? GetAntiforgeryToken() =>
            new("token-de-prueba", "__RequestVerificationToken");
    }

    // Doble de prueba escrito a mano: un catálogo sin especialidades. Las escrituras no se usan acá.
    private sealed class CatalogoVacio : ICatalogoEspecialidades
    {
        public Task<IReadOnlyList<EspecialidadResumen>> ListarAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<EspecialidadResumen>>([]);

        public Task<IReadOnlyList<EspecialidadResumen>> ListarActivasAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<EspecialidadResumen>>([]);

        public Task<EspecialidadResumen?> BuscarAsync(long id, CancellationToken ct) =>
            Task.FromResult<EspecialidadResumen?>(null);

        public Task<long> CrearAsync(ActorAuditoria actor, string nombre, CancellationToken ct) =>
            throw new InvalidOperationException("No se usa en esta prueba.");

        public Task DesactivarAsync(ActorAuditoria actor, long id, CancellationToken ct) =>
            throw new InvalidOperationException("No se usa en esta prueba.");

        public Task ReactivarAsync(ActorAuditoria actor, long id, CancellationToken ct) =>
            throw new InvalidOperationException("No se usa en esta prueba.");
    }

    [Fact]
    [Trait("Story", "3.1")]
    [Trait("AC", "15")]
    public void PaginaEspecialidades_SinEspecialidades_MuestraElEstadoVacio()
    {
        Services.AddSingleton<AntiforgeryStateProvider, TokenDePrueba>();
        Services.AddSingleton<ICatalogoEspecialidades, CatalogoVacio>();
        var autorizacion = AddAuthorization();
        autorizacion.SetAuthorized("lucia");
        autorizacion.SetRoles(Roles.Administrador);

        var pagina = Render<Especialidades>();

        Assert.Contains("Todavía no hay especialidades. Agregá la primera con el formulario de arriba.", pagina.Markup);
        Assert.Empty(pagina.FindAll("table"));
        Assert.NotNull(pagina.Find("input#nombre"));
        Assert.Contains("Agregar", pagina.Find("form button[type=submit]").TextContent);
    }
}
