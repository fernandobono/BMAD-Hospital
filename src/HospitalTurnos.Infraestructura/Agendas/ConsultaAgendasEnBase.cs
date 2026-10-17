using HospitalTurnos.Aplicacion.Agendas;
using HospitalTurnos.Dominio.Agendas;
using HospitalTurnos.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace HospitalTurnos.Infraestructura.Agendas;

/// <summary>
/// IConsultaAgendas sobre el HospitalDbContext del alcance (story 4.1, AC 29). La base trae las candidatas y el
/// dominio elige: la regla de vigencia no se repite en SQL. "Hoy" es la fecha del hospital según el reloj inyectado.
/// </summary>
public sealed class ConsultaAgendasEnBase(HospitalDbContext db, TimeProvider reloj) : IConsultaAgendas
{
    public async Task<AgendaMedico?> AgendaQueRigeAsync(long medicoId, DateOnly fecha, CancellationToken ct)
    {
        var candidatas = await db.AgendasMedico.AsNoTracking().Include(a => a.Franjas)
            .Where(a => a.MedicoId == medicoId && a.Estado == EstadoPropuesta.Aprobada && a.VigenteDesde <= fecha)
            .ToListAsync(ct);
        return AgendaMedico.QueRigeEn(candidatas, fecha);     // la regla, una sola vez, en el dominio
    }

    public async Task<IReadOnlyDictionary<long, SituacionDeAgenda>> EstadosDeAgendaAsync(
        IReadOnlyCollection<long> medicoIds, CancellationToken ct)
    {
        var ids = medicoIds.Distinct().ToList();
        // Una consulta para todo el listado; sin franjas: el estado no las necesita.
        var agendas = await db.AgendasMedico.AsNoTracking()
            .Where(a => ids.Contains(a.MedicoId)
                        && (a.Estado == EstadoPropuesta.Aprobada || a.Estado == EstadoPropuesta.Enviada))
            .ToListAsync(ct);
        var porMedico = agendas.ToLookup(a => a.MedicoId);
        var hoy = DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);   // fecha del hospital, no la UTC
        return ids.ToDictionary(id => id, id => EstadoDeAgenda.Calcular(porMedico[id], hoy));
    }
}
