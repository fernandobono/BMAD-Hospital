using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Personal;
using Npgsql;

namespace HospitalTurnos.Web.Tests.Personal.Dobles;

/// <summary>
/// A4 con la base que no responde a los cambios (T31). El listado anda (la página se tiene que poder
/// dibujar para tomar el token); cada cambio lanza lo que lanzaría Npgsql, con un detalle que no debe
/// llegar a la pantalla.
/// </summary>
public sealed class GestionUsuariosConBaseCaida : IGestionUsuariosPersonal
{
    public Task<IReadOnlyList<UsuarioPersonalFila>> ListarAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<UsuarioPersonalFila>>([]);

    public Task<AltaUsuarioPersonal> CrearAsync(NuevoUsuarioPersonal datos, ActorAuditoria actor, CancellationToken ct) =>
        throw new NpgsqlException("detalle secreto de la base");

    public Task DesactivarAsync(Guid usuarioId, ActorAuditoria actor, CancellationToken ct) =>
        throw new NpgsqlException("detalle secreto de la base");

    public Task ReactivarAsync(Guid usuarioId, ActorAuditoria actor, CancellationToken ct) =>
        throw new NpgsqlException("detalle secreto de la base");

    public Task DesbloquearAsync(Guid usuarioId, ActorAuditoria actor, CancellationToken ct) =>
        throw new NpgsqlException("detalle secreto de la base");
}
