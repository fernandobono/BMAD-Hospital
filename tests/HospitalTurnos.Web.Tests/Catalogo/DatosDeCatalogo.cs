using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Catalogo;
using HospitalTurnos.Dominio.Auditoria;
using HospitalTurnos.Dominio.Catalogo;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Infraestructura.Identidad;
using HospitalTurnos.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalTurnos.Web.Tests.Catalogo;

/// <summary>
/// El "Given" del catálogo en las pruebas de Web (story 3.2, dueña de estas ayudas; la 4.1 las reusa). Las
/// especialidades pasan por el caso de uso de la 3.1 con el actor de admin1 (existe en la base:
/// IActorVigente lo acepta); los médicos entran directo por el contexto, sin auditoría.
/// </summary>
public static class DatosDeCatalogo
{
    public static async Task<ActorAuditoria> ActorDeAdmin1Async(this WebApplicationFactory<Program> app)
    {
        using var alcance = app.Services.CreateScope();
        var admin1 = (await alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>()
            .FindByNameAsync(Personal.PersonalWebApplicationFactory.Admin.Usuario))!;
        return new ActorAuditoria(admin1.Id.ToString(), admin1.NombreCompleto, Roles.Administrador, CanalAuditoria.Personal);
    }

    public static async Task<long> CrearEspecialidadAsync(this WebApplicationFactory<Program> app, bool activa = true,
        string? nombre = null)
    {
        var actor = await app.ActorDeAdmin1Async();
        using var alcance = app.Services.CreateScope();
        var catalogo = alcance.ServiceProvider.GetRequiredService<ICatalogoEspecialidades>();
        var id = await catalogo.CrearAsync(actor, nombre ?? $"Esp{Guid.NewGuid().ToString("N")[..8]}", CancellationToken.None);
        if (!activa)
            await catalogo.DesactivarAsync(actor, id, CancellationToken.None);
        return id;
    }

    public static async Task<long> CrearMedicoAsync(this WebApplicationFactory<Program> app, string nombre, string apellido,
        string matricula, long especialidadId, Guid? usuarioPersonalId = null)
    {
        using var alcance = app.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var medico = new Medico(nombre, apellido, matricula, especialidadId, usuarioPersonalId);
        contexto.Medicos.Add(medico);
        await contexto.SaveChangesAsync();
        return medico.Id;
    }
}
