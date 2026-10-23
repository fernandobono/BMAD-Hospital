using Bunit;
using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Catalogo;
using HospitalTurnos.Aplicacion.Comun;
using HospitalTurnos.Dominio.Auditoria;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Web.Components.Comunes;
using HospitalTurnos.Web.Components.Personal.Administracion;
using HospitalTurnos.Web.Personal;
using HospitalTurnos.Web.Tests.Ayudas;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalTurnos.Web.Tests.Catalogo;

// El estado vacío no se puede ver en una base compartida (siempre hay especialidades de otras pruebas): se prueba
// como componente, con un catálogo de prueba que no tiene ninguna. Story 3.3: el vacío de la guía, con la acción.
public class EspecialidadesPaginaTests : BunitContext
{
    // Doble de prueba escrito a mano: un catálogo sin especialidades. Las escrituras no se usan acá.
    private sealed class CatalogoVacio : ICatalogoEspecialidades
    {
        public Task<IReadOnlyList<EspecialidadResumen>> ListarAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<EspecialidadResumen>>([]);

        public Task<IReadOnlyList<EspecialidadResumen>> ListarActivasAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<EspecialidadResumen>>([]);

        public Task<EspecialidadResumen?> BuscarAsync(long id, CancellationToken ct) =>
            Task.FromResult<EspecialidadResumen?>(null);

        public Task<PaginaDeResultados<EspecialidadFila>> ListarPaginadoAsync(ConsultaDeListado consulta, CancellationToken ct) =>
            Task.FromResult(new PaginaDeResultados<EspecialidadFila>([], 0));

        public Task<int> ContarMedicosAsync(long id, CancellationToken ct) => Task.FromResult(0);

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
    [Trait("Story", "3.3")]
    [Trait("AC", "54")]
    public void PaginaEspecialidades_SinEspecialidades_MuestraElEstadoVacio()
    {
        Services.AddSingleton<ICatalogoEspecialidades, CatalogoVacio>();
        Services.AddSingleton<AlcancePorOperacion>();
        Services.AddSingleton(TimeProvider.System);
        Services.AddScoped<Notificaciones>();
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.Como(new ActorAuditoria(Guid.NewGuid().ToString(), "Lucía Pérez", Roles.Administrador, CanalAuditoria.Personal));

        var pagina = Render<Especialidades>();

        pagina.WaitForAssertion(() =>
            Assert.Equal("Todavía no hay especialidades. Creá la primera.", pagina.Find(".estado-vacio p").TextContent.Trim()));
        Assert.Empty(pagina.FindAll("table"));
        var accion = pagina.Find(".estado-vacio button");
        Assert.Equal("Nueva especialidad", accion.TextContent.Trim());
        accion.Click();
        Assert.NotNull(pagina.Find("[role=dialog] input#nombre"));
    }
}
