using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Comun;
using HospitalTurnos.Dominio.Agendas;

namespace HospitalTurnos.Aplicacion.Catalogo;

/// <summary>
/// Los médicos del plantel (FR-13, story 3.2) y su vínculo con el usuario médico (FR-10). Lo usa A1, y A4
/// para la columna "Médico vinculado". Las escrituras confirman en la base que el actor es un administrador
/// activo (decisión 16).
/// </summary>
public interface IGestionMedicos
{
    /// <summary>Un texto más largo no filtra: la pantalla avisa (AC 2).</summary>
    public const int LargoMaximoBusqueda = 100;

    /// <summary>Ordenados por apellido y nombre; con texto, los que lo contienen en apellido, nombre o matrícula.</summary>
    Task<IReadOnlyList<MedicoListado>> ListarAsync(string? buscar, CancellationToken ct);

    /// <summary>
    /// A1 paginada en la base (story 3.3, AC 44): busca como ListarAsync (un texto de más de 100 caracteres no
    /// filtra), filtra por "especialidad" y por "agenda" (vigente, programada, pendiente, sin-agenda) y ordena por
    /// "medico" (apellido y nombre), "matricula" o "especialidad".
    /// </summary>
    Task<PaginaDeResultados<MedicoListado>> ListarPaginadoAsync(ConsultaDeListado consulta, CancellationToken ct);

    Task<DatosFichaMedico?> ObtenerAsync(long id, CancellationToken ct);

    /// <summary>Activos, con el único rol Medico, sin vínculo; más el vinculado hoy a medicoId si viene.</summary>
    Task<IReadOnlyList<OpcionUsuarioMedico>> UsuariosParaVincularAsync(long? medicoId, CancellationToken ct);

    /// <summary>usuarioId → "Apellido, Nombre" (columna de A4, AC 14).</summary>
    Task<IReadOnlyDictionary<Guid, string>> MedicosVinculadosAsync(CancellationToken ct);

    Task<long> CrearAsync(DatosMedico datos, ActorAuditoria actor, CancellationToken ct);

    /// <summary>true si guardó; false si no había cambios (AC 16).</summary>
    Task<bool> EditarAsync(long id, DatosMedico datos, ActorAuditoria actor, CancellationToken ct);
}

public sealed record DatosMedico(string Nombre, string Apellido, string Matricula, long EspecialidadId, Guid? UsuarioPersonalId);

public sealed record MedicoListado(long Id, string Apellido, string Nombre, string Matricula, string Especialidad,
    SituacionDeAgenda Agenda);

// "Datos…" y no "FichaMedico": la página FichaMedico.razor ya genera una clase con ese nombre (I-5).
public sealed record DatosFichaMedico(long Id, DatosMedico Datos, string Especialidad, bool EspecialidadActiva,
    SituacionDeAgenda Agenda);

public sealed record OpcionUsuarioMedico(Guid Id, string Usuario, string NombreCompleto);
