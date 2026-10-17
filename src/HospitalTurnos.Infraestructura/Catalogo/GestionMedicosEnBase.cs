using System.Globalization;
using HospitalTurnos.Aplicacion.Agendas;
using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Catalogo;
using HospitalTurnos.Aplicacion.Personal;
using HospitalTurnos.Dominio.Auditoria;
using HospitalTurnos.Dominio.Catalogo;
using HospitalTurnos.Dominio.Excepciones;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Infraestructura.Persistencia;
using HospitalTurnos.Infraestructura.Persistencia.Configuraciones;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HospitalTurnos.Infraestructura.Catalogo;

/// <summary>
/// A1 sobre el HospitalDbContext del alcance (story 3.2). Cada escritura confirma primero en la base que el
/// actor es un administrador activo (decisión 16), valida sólo lo que cambia (AC 17) y guarda el cambio con
/// su auditoría, juntos o nada. Las carreras contra los índices únicos se traducen a la misma regla (AC 8 y 12).
/// El estado de agenda de cada médico lo trae IConsultaAgendas (story 4.1, AC 30; contrato del AC 23 de la 3.2).
/// </summary>
public sealed class GestionMedicosEnBase(
    HospitalDbContext contexto,
    IAuditoria auditoria,
    ICatalogoEspecialidades catalogo,
    IActorVigente actorVigente,
    IConsultaAgendas consultaAgendas) : IGestionMedicos
{
    private const string SinPermiso = "Sólo un administrador puede dar de alta o modificar médicos.";
    private const string NoExiste = "No existe el médico pedido.";
    private const string EspecialidadInvalida = "La especialidad elegida no existe o está desactivada. Elegí otra.";
    private const string UsuarioNoVinculable =
        "El usuario elegido no se puede vincular: tiene que estar activo y tener el rol Médico.";
    private const string EntidadTipo = "Medico";

    public async Task<IReadOnlyList<MedicoListado>> ListarAsync(string? buscar, CancellationToken ct)
    {
        var consulta = contexto.Medicos.AsNoTracking();
        var texto = buscar?.Trim();
        if (!string.IsNullOrEmpty(texto) && texto.Length <= IGestionMedicos.LargoMaximoBusqueda)
        {
            // Con ESCAPE '\': % y _ dejan de ser comodines (AC 2). El patrón viaja como parámetro.
            var patron = $"%{Escapar(texto)}%";
            consulta = consulta.Where(m => EF.Functions.ILike(m.Apellido, patron, "\\")
                || EF.Functions.ILike(m.Nombre, patron, "\\")
                || EF.Functions.ILike(m.Matricula, patron, "\\"));
        }
        var filas = await consulta
            .Join(contexto.Especialidades, m => m.EspecialidadId, e => e.Id, (m, e) => new { m, Especialidad = e.Nombre })
            .OrderBy(x => x.m.Apellido).ThenBy(x => x.m.Nombre)
            .Select(x => new { x.m.Id, x.m.Apellido, x.m.Nombre, x.m.Matricula, x.Especialidad })
            .ToListAsync(ct);
        // Una sola consulta de agendas para todo el listado, no una por fila (AC 30 de la 4.1).
        var agendas = await consultaAgendas.EstadosDeAgendaAsync(filas.Select(x => x.Id).ToList(), ct);
        return filas.Select(x => new MedicoListado(x.Id, x.Apellido, x.Nombre, x.Matricula, x.Especialidad, agendas[x.Id]))
            .ToList();
    }

    public async Task<DatosFichaMedico?> ObtenerAsync(long id, CancellationToken ct)
    {
        var fila = await contexto.Medicos.AsNoTracking()
            .Where(m => m.Id == id)
            .Join(contexto.Especialidades, m => m.EspecialidadId, e => e.Id, (m, e) => new { m, e.Nombre, e.Activa })
            .FirstOrDefaultAsync(ct);
        if (fila is null) return null;
        var m = fila.m;
        var agenda = await consultaAgendas.EstadosDeAgendaAsync([m.Id], ct);
        return new DatosFichaMedico(m.Id, new DatosMedico(m.Nombre, m.Apellido, m.Matricula, m.EspecialidadId, m.UsuarioPersonalId),
            fila.Nombre, fila.Activa, agenda[m.Id]);
    }

    public async Task<IReadOnlyList<OpcionUsuarioMedico>> UsuariosParaVincularAsync(long? medicoId, CancellationToken ct)
    {
        // El vinculado hoy a ESTE médico aparece aunque ya no cumpla (desactivado después, AC 17): así el
        // formulario muestra el valor real y guardarlo sin tocarlo no lo desvincula.
        var propio = medicoId is null
            ? null
            : await contexto.Medicos.Where(m => m.Id == medicoId).Select(m => m.UsuarioPersonalId).FirstOrDefaultAsync(ct);
        return await Vinculables()
            .Where(u => !contexto.Medicos.Any(m => m.UsuarioPersonalId == u.Id))
            .Union(contexto.Users.Where(u => u.Id == propio))
            .OrderBy(u => u.NombreCompleto)
            .Select(u => new OpcionUsuarioMedico(u.Id, u.UserName!, u.NombreCompleto))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> MedicosVinculadosAsync(CancellationToken ct) =>
        await contexto.Medicos.AsNoTracking()
            .Where(m => m.UsuarioPersonalId != null)
            .ToDictionaryAsync(m => m.UsuarioPersonalId!.Value, m => m.Apellido + ", " + m.Nombre, ct);

    public async Task<long> CrearAsync(DatosMedico datos, ActorAuditoria actor, CancellationToken ct)
    {
        // AC 20, EN LA BASE y antes de mirar nada; el actor devuelto (nombre leído de la base) es el que se audita.
        actor = await actorVigente.ExigirAdministradorActivoAsync(actor, SinPermiso, ct);
        var medico = new Medico(datos.Nombre, datos.Apellido, datos.Matricula, datos.EspecialidadId, datos.UsuarioPersonalId);
        await ExigirEspecialidadActivaAsync(medico.EspecialidadId, ct);            // AC 9
        await ExigirMatriculaLibreAsync(medico.Matricula, excepto: null, ct);      // AC 8
        if (medico.UsuarioPersonalId is { } usuario)
            await ExigirUsuarioVinculableAsync(usuario, excepto: null, ct);        // AC 11 y 12

        // El id (identity) nace al guardar y la auditoría lo necesita: transacción explícita para que
        // médico y registro queden juntos o ninguno (Arq. §7.5). Si algo lanza, se descarta al salir.
        await using var transaccion = await contexto.Database.BeginTransactionAsync(ct);
        contexto.Medicos.Add(medico);
        await GuardarTraduciendoCarrerasAsync(medico, ct);
        auditoria.Registrar(actor, AccionesAuditoria.MedicoCreado, EntidadTipo, Texto(medico.Id), new
        {
            nombre = medico.Nombre,
            apellido = medico.Apellido,
            matricula = medico.Matricula,
            especialidadId = medico.EspecialidadId,
            usuarioPersonalId = medico.UsuarioPersonalId,
        });
        await contexto.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        return medico.Id;
    }

    public async Task<bool> EditarAsync(long id, DatosMedico datos, ActorAuditoria actor, CancellationToken ct)
    {
        actor = await actorVigente.ExigirAdministradorActivoAsync(actor, SinPermiso, ct);   // AC 20
        var medico = await contexto.Medicos.FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new NoEncontradoException(NoExiste);                                   // AC 18
        // Si una validación de abajo falla, la entidad queda cambiada en memoria pero nunca se guarda:
        // el contexto es del pedido y se descarta.
        var cambios = medico.CambiarDatos(datos.Nombre, datos.Apellido, datos.Matricula, datos.EspecialidadId,
            datos.UsuarioPersonalId);
        if (cambios.Count == 0) return false;                                              // AC 16

        // Sólo lo que cambia se revalida (AC 17).
        foreach (var cambio in cambios)
        {
            switch (cambio.Campo)
            {
                case "especialidadId":
                    await ExigirEspecialidadActivaAsync(medico.EspecialidadId, ct);
                    break;
                case "matricula":
                    await ExigirMatriculaLibreAsync(medico.Matricula, excepto: id, ct);
                    break;
                case "usuarioPersonalId" when medico.UsuarioPersonalId is { } usuario:
                    await ExigirUsuarioVinculableAsync(usuario, excepto: id, ct);
                    break;
            }
        }
        auditoria.Registrar(actor, AccionesAuditoria.MedicoEditado, EntidadTipo, Texto(id), new
        {
            cambios = cambios.ToDictionary(c => c.Campo, c => new { antes = c.Antes, despues = c.Despues }),
        });
        await GuardarTraduciendoCarrerasAsync(medico, ct);   // el id ya existe: un solo guardado
        return true;
    }

    private async Task ExigirEspecialidadActivaAsync(long especialidadId, CancellationToken ct)
    {
        if (await catalogo.BuscarAsync(especialidadId, ct) is not { Activa: true })
            throw new ReglaDeNegocioException(EspecialidadInvalida);
    }

    private async Task ExigirMatriculaLibreAsync(string matricula, long? excepto, CancellationToken ct)
    {
        if (await contexto.Medicos.AnyAsync(m => m.Matricula == matricula && m.Id != excepto, ct))
            throw MatriculaRepetida(matricula);
    }

    private async Task ExigirUsuarioVinculableAsync(Guid usuario, long? excepto, CancellationToken ct)
    {
        if (!await Vinculables().AnyAsync(u => u.Id == usuario, ct))
            throw new ReglaDeNegocioException(UsuarioNoVinculable);                // AC 11
        var otro = await contexto.Medicos.AsNoTracking()
            .Where(m => m.UsuarioPersonalId == usuario && m.Id != excepto)
            .Select(m => m.Apellido + ", " + m.Nombre)
            .FirstOrDefaultAsync(ct);
        if (otro is not null)
            throw new ConflictoException($"El usuario elegido ya está vinculado al médico {otro}.");   // AC 12
    }

    // Existe, está activo y su ÚNICO rol es Medico (AC 11). Las tablas de Identity, del mismo contexto.
    private IQueryable<Identidad.UsuarioPersonal> Vinculables()
    {
        var rolMedico = contexto.Roles.Where(r => r.Name == Roles.Medico).Select(r => r.Id);
        return contexto.Users.Where(u => u.Activo
            && contexto.UserRoles.Count(ur => ur.UserId == u.Id) == 1
            && contexto.UserRoles.Any(ur => ur.UserId == u.Id && rolMedico.Contains(ur.RoleId)));
    }

    // AC 8 y 12: la carrera la frena el índice; el usuario ve la misma regla, no un 500.
    private async Task GuardarTraduciendoCarrerasAsync(Medico medico, CancellationToken ct)
    {
        try
        {
            await contexto.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: MedicoConfiguracion.IndiceMatricula,
        })
        {
            throw MatriculaRepetida(medico.Matricula);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: MedicoConfiguracion.IndiceUsuario,
        })
        {
            throw new ConflictoException("El usuario elegido ya está vinculado a otro médico.");
        }
        // Otro índice: no es una regla conocida, sigue como error (500 genérico).
    }

    private static ConflictoException MatriculaRepetida(string matricula) =>
        new($"Ya hay un médico con la matrícula {matricula}.");

    private static string Escapar(string texto) =>
        texto.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private static string Texto(long valor) => valor.ToString(CultureInfo.InvariantCulture);
}
