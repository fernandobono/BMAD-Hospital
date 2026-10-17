using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Personal;
using HospitalTurnos.Aplicacion.Tests.Agendas;
using HospitalTurnos.Aplicacion.Tests.Ayudas;
using HospitalTurnos.Dominio.Auditoria;
using HospitalTurnos.Dominio.Excepciones;
using HospitalTurnos.Dominio.Personal;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalTurnos.Aplicacion.Tests.Personal;

// El permiso por rol confirmado en la base (story 4.2, AC 21): el método general que usan el médico (4.2) y la
// recepción (5.1, que suma acá su caso), y el del administrador, que ahora lo llama. Usuarios propios: la base
// es compartida.
[Collection("Postgres")]
public class ActorVigenteIdentityTests(PostgresFixture fixture)
{
    private const string Mensaje = "Mensaje de la prueba.";

    // Pasan sólo "medico activo" y "administrador activo"; los otros seis, AccesoDenegadoException.
    public static TheoryData<string> Casos =>
    [
        "medico activo", "recepcionista", "medico desactivado", "administrador como medico", "medico con segundo rol",
        "inexistente", "administrador activo", "administrador desactivado",
    ];

    [Theory]
    [Trait("Story", "4.2")]
    [Trait("AC", "21")]
    [Trait("AC", "24")]
    [MemberData(nameof(Casos))]
    public async Task ExigirActivoConRolAsync_SegunElUsuarioEnLaBase_DevuelveElActorOLanza(string caso)
    {
        await using var proveedor = fixture.Proveedor();
        var nombre = $"Persona {DatosDeAgenda.Marca()}";
        var esAdministrador = caso.StartsWith("administrador ", StringComparison.Ordinal) && caso != "administrador como medico";
        var rolPedido = esAdministrador ? Roles.Administrador : Roles.Medico;
        ActorAuditoria actor;
        if (caso == "inexistente")
            actor = new(Guid.NewGuid().ToString(), "Nadie", Roles.Medico, CanalAuditoria.Personal);
        else
        {
            string[] roles = caso switch
            {
                "recepcionista" => [Roles.Recepcionista],
                "administrador como medico" or "administrador activo" or "administrador desactivado" => [Roles.Administrador],
                "medico con segundo rol" => [Roles.Medico, Roles.Recepcionista],
                _ => [Roles.Medico],
            };
            var usuario = await UsuariosDePrueba.CrearAsync(proveedor, roles,
                activo: caso is not ("medico desactivado" or "administrador desactivado"), nombre: nombre);
            // El nombre de la sesión puede estar atrasado: el que vale es el de la base.
            actor = UsuariosDePrueba.ActorDe(usuario, rolPedido) with { Quien = "Nombre de la sesión" };
        }

        using var alcance = proveedor.CreateScope();
        var actorVigente = alcance.ServiceProvider.GetRequiredService<IActorVigente>();
        Func<Task<ActorAuditoria>> exigir = esAdministrador
            ? () => actorVigente.ExigirAdministradorActivoAsync(actor, Mensaje, CancellationToken.None)
            : () => actorVigente.ExigirActivoConRolAsync(actor, Roles.Medico, Mensaje, CancellationToken.None);

        if (caso is "medico activo" or "administrador activo")
        {
            var confirmado = await exigir();
            Assert.Equal((actor.UsuarioId, nombre, rolPedido, CanalAuditoria.Personal),
                (confirmado.UsuarioId, confirmado.Quien, confirmado.Rol, confirmado.Canal));
            return;
        }
        var error = await Assert.ThrowsAsync<AccesoDenegadoException>(exigir);
        Assert.Equal(Mensaje, error.Message);
    }
}
