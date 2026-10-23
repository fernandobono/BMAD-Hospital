using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Catalogo;
using HospitalTurnos.Aplicacion.Tests.Agendas;
using HospitalTurnos.Aplicacion.Tests.Ayudas;
using HospitalTurnos.Dominio.Personal;
using Microsoft.Extensions.DependencyInjection;
using static HospitalTurnos.Aplicacion.Tests.Agendas.DatosDeAgenda;

namespace HospitalTurnos.Aplicacion.Tests.Auditoria;

// Story 3.3, AC 46: "Creado por / Modificado por" sale de la auditoría, sólo leyendo (con turnos_app, que tiene
// SELECT e INSERT sobre registros_auditoria: no se toca la protección de la tabla, ADR-07).
[Collection("Postgres")]
public class ConsultaTrazabilidadTests(PostgresFixture fixture)
{
    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "46")]
    public async Task Trazabilidad_DevuelveCreacionYUltimaModificacion()
    {
        await using var proveedor = fixture.Proveedor();
        var lucia = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Administrador], nombre: $"Lucía {Marca()}");
        var diego = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Administrador], nombre: $"Diego {Marca()}");
        var especialidad = await proveedor.EspecialidadPropiaAsync();
        var matricula = $"T-{Marca().ToUpperInvariant()}";
        long id;
        using (var alcance = proveedor.CreateScope())
        {
            var gestion = alcance.ServiceProvider.GetRequiredService<IGestionMedicos>();
            id = await gestion.CrearAsync(new DatosMedico("Pablo", "Ríos", matricula, especialidad, null),
                UsuariosDePrueba.ActorDe(lucia, Roles.Administrador), CancellationToken.None);
        }
        using (var alcance = proveedor.CreateScope())
            await alcance.ServiceProvider.GetRequiredService<IGestionMedicos>().EditarAsync(id,
                new DatosMedico("Pablo José", "Ríos", matricula, especialidad, null), UsuariosDePrueba.ActorDe(lucia, Roles.Administrador), CancellationToken.None);
        using (var alcance = proveedor.CreateScope())
            await alcance.ServiceProvider.GetRequiredService<IGestionMedicos>().EditarAsync(id,
                new DatosMedico("Pablo", "Ríos", matricula, especialidad, null), UsuariosDePrueba.ActorDe(diego, Roles.Administrador), CancellationToken.None);

        using var lectura = proveedor.CreateScope();
        var consulta = lectura.ServiceProvider.GetRequiredService<IConsultaTrazabilidad>();
        var traza = await consulta.ObtenerAsync("Medico", id.ToString(), CancellationToken.None);
        var ninguna = await consulta.ObtenerAsync("Medico", "-1", CancellationToken.None);

        Assert.NotNull(traza);
        Assert.Equal(lucia.NombreCompleto, traza.Creado.Quien);
        Assert.Equal(diego.NombreCompleto, traza.Modificado!.Quien);
        Assert.True(traza.Modificado.Cuando >= traza.Creado.Cuando);
        Assert.Null(ninguna);
    }
}
