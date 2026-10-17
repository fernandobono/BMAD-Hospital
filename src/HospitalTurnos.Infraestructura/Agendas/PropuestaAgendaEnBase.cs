using System.Globalization;
using HospitalTurnos.Aplicacion.Agendas;
using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Personal;
using HospitalTurnos.Dominio.Agendas;
using HospitalTurnos.Dominio.Auditoria;
using HospitalTurnos.Dominio.Excepciones;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Infraestructura.Persistencia;
using HospitalTurnos.Infraestructura.Persistencia.Configuraciones;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HospitalTurnos.Infraestructura.Agendas;

/// <summary>
/// M2 sobre el HospitalDbContext del alcance (story 4.2). Cada escritura confirma en la base que el actor es un
/// médico activo y busca SU médico (AC 21 y 22); después, el control previo de la enviada (AC 18), los cambios con
/// los métodos del dominio (4.1) y un solo guardado, con las carreras traducidas a la regla (AC 19). Sólo el envío
/// se audita, junto con el cambio o nada (AC 17).
/// </summary>
public sealed class PropuestaAgendaEnBase(
    HospitalDbContext contexto,
    IAuditoria auditoria,
    IActorVigente actorVigente,
    TimeProvider reloj) : IPropuestaAgenda
{
    private const string SinPermiso = "Sólo un médico activo puede armar o enviar su propuesta de agenda.";
    private const string SinVinculo = "Tu usuario no está vinculado a un médico. Pedíselo al administrador.";
    private const string YaHayEnviada =
        "Ya tenés una propuesta enviada esperando aprobación: hasta que el administrador la revise no podés modificarla ni armar otra.";
    private const string YaHayEnCurso = "Ya tenés una propuesta en curso: volvé a abrir la pantalla para seguir con ésa.";
    private const string CambioMientrasTanto =
        "Tu propuesta cambió mientras la guardabas: volvé a abrir la pantalla para ver cómo quedó.";
    private const string EntidadTipo = "AgendaMedico";

    private static readonly EstadoPropuesta[] EnCurso = [EstadoPropuesta.Borrador, EstadoPropuesta.Rechazada, EstadoPropuesta.Enviada];

    public async Task<VistaPropuesta> ObtenerAsync(ActorAuditoria actor, CancellationToken ct)
    {
        var hoy = DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);
        var minimo = hoy.AddDays(AgendaMedico.DiasMinimosDeAnticipacion);
        if (await MedicoDeAsync(actor, ct) is not { } medicoId)
            return new VistaPropuesta(true, null, null, null, null, minimo);

        // Un médico tiene pocas agendas (unas por año): una consulta y el resto en memoria.
        var agendas = await contexto.AgendasMedico.AsNoTracking().Include(a => a.Franjas)
            .Where(a => a.MedicoId == medicoId).ToListAsync(ct);
        var aprobadas = agendas.Where(a => a.Estado == EstadoPropuesta.Aprobada).ToList();
        return new VistaPropuesta(
            SinVinculo: false,
            Vigente: AgendaMedico.QueRigeEn(agendas, hoy),
            ProximaAprobada: aprobadas.Where(a => a.VigenteDesde > hoy)
                .OrderBy(a => a.VigenteDesde).ThenByDescending(a => a.AprobadaEl).FirstOrDefault(),
            ParaCopiar: aprobadas.OrderByDescending(a => a.VigenteDesde).ThenByDescending(a => a.AprobadaEl).FirstOrDefault(),
            EnCurso: agendas.FirstOrDefault(a => a.Estado == EstadoPropuesta.Enviada)
                ?? agendas.FirstOrDefault(a => a.Estado is EstadoPropuesta.Borrador or EstadoPropuesta.Rechazada),
            MinimoVigenteDesde: minimo);
    }

    public async Task<ResultadoGuardado> GuardarBorradorAsync(DatosAgenda datos, ActorAuditoria actor, CancellationToken ct)
    {
        var (medicoId, _) = await MedicoDeLaSesionAsync(actor, ct);
        var editable = await EditableSinEnviadaAsync(medicoId, ct);
        if (editable is null)
        {
            var nueva = Nueva(medicoId, datos);
            contexto.AgendasMedico.Add(nueva);
            await GuardarTraduciendoAsync(nueva, ct);
            return ResultadoGuardado.Creada;
        }
        if (!AplicarDiferencias(editable, datos))
            return ResultadoGuardado.SinCambios;     // una rechazada sigue rechazada (AC 13)
        await GuardarTraduciendoAsync(editable, ct);
        return ResultadoGuardado.Guardada;
    }

    public async Task EnviarAsync(DatosAgenda datos, ActorAuditoria actor, CancellationToken ct)
    {
        var (medicoId, actorDeLaBase) = await MedicoDeLaSesionAsync(actor, ct);
        var editable = await EditableSinEnviadaAsync(medicoId, ct);
        var agenda = editable ?? Nueva(medicoId, datos);                  // AC 15: crea si no hay
        if (editable is null)
            contexto.AgendasMedico.Add(agenda);
        else
            AplicarDiferencias(agenda, datos);                            // AC 13
        agenda.Enviar(reloj);                                             // 4.1 AC 9: si lanza, no se guarda nada

        // El id de una agenda nueva nace al guardar y la auditoría lo necesita: transacción, como CrearAsync de la 3.2.
        await using var transaccion = await contexto.Database.BeginTransactionAsync(ct);
        await GuardarTraduciendoAsync(agenda, ct);
        auditoria.Registrar(actorDeLaBase, AccionesAuditoria.PropuestaAgendaEnviada, EntidadTipo,
            agenda.Id.ToString(CultureInfo.InvariantCulture), DetalleDe(agenda));
        await contexto.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
    }

    // AC 21: rol y actividad en la base primero; después, el médico vinculado al usuario.
    private async Task<(long MedicoId, ActorAuditoria Actor)> MedicoDeLaSesionAsync(ActorAuditoria actor, CancellationToken ct)
    {
        var actorDeLaBase = await actorVigente.ExigirActivoConRolAsync(actor, Roles.Medico, SinPermiso, ct);
        return (await MedicoDeAsync(actorDeLaBase, ct) ?? throw new AccesoDenegadoException(SinVinculo), actorDeLaBase);
    }

    private async Task<long?> MedicoDeAsync(ActorAuditoria actor, CancellationToken ct)
    {
        if (!Guid.TryParse(actor.UsuarioId, out var usuarioId))
            return null;
        return await contexto.Medicos.AsNoTracking().Where(m => m.UsuarioPersonalId == usuarioId)
            .Select(m => (long?)m.Id).FirstOrDefaultAsync(ct);
    }

    // AC 18: la primera defensa, con seguimiento para editar la que hay.
    private async Task<AgendaMedico?> EditableSinEnviadaAsync(long medicoId, CancellationToken ct)
    {
        var enCurso = await contexto.AgendasMedico.Include(a => a.Franjas)
            .Where(a => a.MedicoId == medicoId && EnCurso.Contains(a.Estado)).ToListAsync(ct);
        if (enCurso.Any(a => a.Estado == EstadoPropuesta.Enviada))
            throw new ConflictoException(YaHayEnviada);
        return enCurso.FirstOrDefault();
    }

    private static AgendaMedico Nueva(long medicoId, DatosAgenda datos)
    {
        var agenda = new AgendaMedico(medicoId, datos.DuracionTurnoMinutos, datos.VigenteDesde);
        foreach (var franja in datos.Franjas)
            agenda.AgregarFranja(new Franja(franja.Dia, franja.Desde, franja.Hasta));
        return agenda;
    }

    /// <summary>
    /// Sólo las diferencias (AC 13): primero se quitan las franjas que ya no están igual; después se agregan las que
    /// faltan (así una franja que cambió de hora no choca con su versión vieja); duración y fecha sólo si cambiaron.
    /// Una fila repetida en los datos se agrega dos veces y el dominio la rechaza como superpuesta.
    /// </summary>
    private static bool AplicarDiferencias(AgendaMedico agenda, DatosAgenda datos)
    {
        var cambio = false;
        foreach (var franja in agenda.Franjas.Where(f => !datos.Franjas.Any(d => Igual(f, d))).ToList())
        {
            agenda.QuitarFranja(franja.Dia, franja.Desde);
            cambio = true;
        }
        var conservadas = new HashSet<Franja>();
        foreach (var dato in datos.Franjas)
        {
            if (agenda.Franjas.FirstOrDefault(f => Igual(f, dato) && !conservadas.Contains(f)) is { } igual)
            {
                conservadas.Add(igual);
                continue;
            }
            agenda.AgregarFranja(new Franja(dato.Dia, dato.Desde, dato.Hasta));
            cambio = true;
        }
        if (agenda.DuracionTurnoMinutos != datos.DuracionTurnoMinutos)
        {
            agenda.CambiarDuracion(datos.DuracionTurnoMinutos);
            cambio = true;
        }
        if (agenda.VigenteDesde != datos.VigenteDesde)
        {
            agenda.CambiarVigenteDesde(datos.VigenteDesde);
            cambio = true;
        }
        return cambio;
    }

    private static bool Igual(Franja franja, DatosFranja dato) =>
        franja.Dia == dato.Dia && franja.Desde == dato.Desde && franja.Hasta == dato.Hasta;

    // AC 19: las tres carreras se traducen a la regla; un índice desconocido sigue como error (500 genérico).
    private async Task GuardarTraduciendoAsync(AgendaMedico agenda, CancellationToken ct)
    {
        // Aunque sólo cambien franjas, la fila de la agenda se actualiza: así xmin también protege ese guardado.
        if (agenda.Id != 0)
            contexto.Entry(agenda).State = EntityState.Modified;
        try
        {
            await contexto.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictoException(CambioMientrasTanto);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: AgendaMedicoConfiguracion.IndiceUnaEnviada,
        })
        {
            throw new ConflictoException(YaHayEnviada);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: AgendaMedicoConfiguracion.IndiceUnaEditable,
        })
        {
            throw new ConflictoException(YaHayEnCurso);
        }
    }

    // Formato de franjas de la auditoría, contrato del AC 17: el día como en la base (0 = domingo) y HH:mm.
    private static object DetalleDe(AgendaMedico agenda) => new
    {
        medicoId = agenda.MedicoId,
        duracionTurnoMinutos = agenda.DuracionTurnoMinutos,
        vigenteDesde = agenda.VigenteDesde.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        franjas = agenda.Franjas.OrderBy(f => (int)f.Dia).ThenBy(f => f.Desde).Select(f => new
        {
            dia = (int)f.Dia,
            desde = f.Desde.ToString("HH:mm", CultureInfo.InvariantCulture),
            hasta = f.Hasta.ToString("HH:mm", CultureInfo.InvariantCulture),
        }).ToList(),
        turnosPorSemana = agenda.TurnosPorSemana(),
    };
}
