using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Catalogo;
using HospitalTurnos.Dominio.Catalogo;
using HospitalTurnos.Infraestructura.Persistencia;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalTurnos.Aplicacion.Tests.Catalogo;

/// <summary>
/// El "Given" del catálogo (story 3.2, dueña de estas ayudas; la 4.1 las reusa). Las especialidades pasan
/// por el caso de uso de la 3.1 con un administrador real; los médicos entran directo por el contexto,
/// sin auditoría, porque son el punto de partida de la prueba y no lo que se prueba.
/// </summary>
public static class DatosDeCatalogo
{
    public static async Task<long> CrearEspecialidadAsync(this ServiceProvider proveedor, ActorAuditoria administrador,
        bool activa = true, string? nombre = null)
    {
        using var alcance = proveedor.CreateScope();
        var catalogo = alcance.ServiceProvider.GetRequiredService<ICatalogoEspecialidades>();
        var id = await catalogo.CrearAsync(administrador, nombre ?? $"Esp{Guid.NewGuid().ToString("N")[..8]}",
            CancellationToken.None);
        if (!activa)
            await catalogo.DesactivarAsync(administrador, id, CancellationToken.None);
        return id;
    }

    public static async Task<long> CrearMedicoAsync(this ServiceProvider proveedor, string nombre, string apellido,
        string matricula, long especialidadId, Guid? usuarioPersonalId = null)
    {
        using var alcance = proveedor.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var medico = new Medico(nombre, apellido, matricula, especialidadId, usuarioPersonalId);
        contexto.Medicos.Add(medico);
        await contexto.SaveChangesAsync();
        return medico.Id;
    }
}
