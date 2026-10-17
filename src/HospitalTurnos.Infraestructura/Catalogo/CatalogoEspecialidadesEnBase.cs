using System.Globalization;
using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Catalogo;
using HospitalTurnos.Aplicacion.Personal;
using HospitalTurnos.Dominio.Auditoria;
using HospitalTurnos.Dominio.Catalogo;
using HospitalTurnos.Dominio.Excepciones;
using HospitalTurnos.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HospitalTurnos.Infraestructura.Catalogo;

/// <summary>
/// A2 sobre el HospitalDbContext del alcance (story 3.1). Cada escritura confirma primero en la base que el
/// actor es un administrador activo (decisión 16) y guarda el cambio con su auditoría, juntos o nada.
/// Desactivar no consulta turnos: todavía no existen (AC 10); la 5.3 cumple el contrato del AC 17.
/// </summary>
public sealed class CatalogoEspecialidadesEnBase(
    HospitalDbContext contexto,
    IAuditoria auditoria,
    IActorVigente actorVigente) : ICatalogoEspecialidades
{
    private const string SinPermiso = "Sólo el administrador puede modificar las especialidades.";
    private const string YaNoExiste = "La especialidad ya no existe. Actualizá la lista.";
    private const string EntidadTipo = "Especialidad";
    private const string IndiceNombre = "ix_especialidades_nombre_normalizado";   // migración AgregarEspecialidades

    public Task<IReadOnlyList<EspecialidadResumen>> ListarAsync(CancellationToken ct) =>
        ListarDondeAsync(contexto.Especialidades, ct);

    public Task<IReadOnlyList<EspecialidadResumen>> ListarActivasAsync(CancellationToken ct) =>
        ListarDondeAsync(contexto.Especialidades.Where(e => e.Activa), ct);

    public Task<EspecialidadResumen?> BuscarAsync(long id, CancellationToken ct) =>
        contexto.Especialidades.AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new EspecialidadResumen(e.Id, e.Nombre, e.Activa))
            .FirstOrDefaultAsync(ct);

    public async Task<long> CrearAsync(ActorAuditoria actor, string nombre, CancellationToken ct)
    {
        // 1. Permiso EN LA BASE (AC 13), antes de tocar nada; el actor devuelto es el que se audita.
        actor = await actorVigente.ExigirAdministradorActivoAsync(actor, SinPermiso, ct);
        var nueva = new Especialidad(nombre);                 // 2. reglas del nombre (AC 1)
        var existente = await contexto.Especialidades.AsNoTracking()
            .FirstOrDefaultAsync(e => e.NombreNormalizado == nueva.NombreNormalizado, ct);
        if (existente is not null) throw Repetida(existente); // 3. con el nombre guardado (AC 6)
        contexto.Especialidades.Add(nueva);

        // 4. El id lo da la base al guardar: se guarda primero y se audita con el id real, dentro de UNA
        //    transacción, para que el cambio y su registro queden juntos o ninguno (AC 5).
        await using var tx = await contexto.Database.BeginTransactionAsync(ct);
        try
        {
            await contexto.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: IndiceNombre,
        })
        {
            // 5. Otra alta ganó la carrera entre la verificación y el guardado (AC 7). La entidad sigue
            //    en el contexto como Added, pero se lanza enseguida y el contexto es del pedido.
            await tx.RollbackAsync(ct);
            var ganadora = await contexto.Especialidades.AsNoTracking()
                .FirstAsync(x => x.NombreNormalizado == nueva.NombreNormalizado, ct);
            throw Repetida(ganadora);
        }
        auditoria.Registrar(actor, AccionesAuditoria.EspecialidadCreada, EntidadTipo,
            nueva.Id.ToString(CultureInfo.InvariantCulture), new { nombre = nueva.Nombre });
        await contexto.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return nueva.Id;
    }

    public Task DesactivarAsync(ActorAuditoria actor, long id, CancellationToken ct) =>
        CambiarEstadoAsync(actor, id, e => e.Desactivar(), AccionesAuditoria.EspecialidadDesactivada, ct);

    public Task ReactivarAsync(ActorAuditoria actor, long id, CancellationToken ct) =>
        CambiarEstadoAsync(actor, id, e => e.Reactivar(), AccionesAuditoria.EspecialidadReactivada, ct);

    private async Task CambiarEstadoAsync(ActorAuditoria actor, long id, Action<Especialidad> cambio, string accion,
        CancellationToken ct)
    {
        actor = await actorVigente.ExigirAdministradorActivoAsync(actor, SinPermiso, ct);   // AC 13
        var especialidad = await contexto.Especialidades.FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NoEncontradoException(YaNoExiste);                                  // AC 12
        cambio(especialidad);                                    // las reglas del AC 3 y 4 viven en el dominio
        auditoria.Registrar(actor, accion, EntidadTipo, id.ToString(CultureInfo.InvariantCulture),
            new { nombre = especialidad.Nombre });
        await contexto.SaveChangesAsync(ct);                     // el id ya existe: un solo guardado
    }

    private static async Task<IReadOnlyList<EspecialidadResumen>> ListarDondeAsync(IQueryable<Especialidad> consulta,
        CancellationToken ct) =>
        await consulta.AsNoTracking()
            .OrderBy(e => e.NombreNormalizado)
            .Select(e => new EspecialidadResumen(e.Id, e.Nombre, e.Activa))
            .ToListAsync(ct);

    private static ConflictoException Repetida(Especialidad existente) => new(existente.Activa
        ? $"Ya existe la especialidad {existente.Nombre}."
        : $"Ya existe la especialidad {existente.Nombre}, desactivada. Reactivala desde la lista.");
}
