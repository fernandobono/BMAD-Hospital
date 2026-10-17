using HospitalTurnos.Aplicacion.Catalogo;
using HospitalTurnos.Aplicacion.Comun;
using HospitalTurnos.Aplicacion.Personal;
using HospitalTurnos.Aplicacion.Tests.Agendas;
using HospitalTurnos.Aplicacion.Tests.Ayudas;
using HospitalTurnos.Dominio.Agendas;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Infraestructura.Identidad;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using static HospitalTurnos.Aplicacion.Tests.Agendas.DatosDeAgenda;

namespace HospitalTurnos.Aplicacion.Tests.Catalogo;

// Story 3.3, sección F (decisión D2): consultas de sólo lectura, paginadas, ordenadas y contadas en la base. La base
// es compartida: cada prueba crea sus registros con una marca y filtra por ella (búsqueda o especialidad propia).
[Collection("Postgres")]
public class ListadosPaginadosTests(PostgresFixture fixture)
{
    private static ConsultaDeListado Consulta(string? buscar = null, string orden = "medico", int pagina = 1, int tamano = 25,
        params (string Nombre, string Valor)[] filtros) =>
        new(buscar, filtros.ToDictionary(f => f.Nombre, f => f.Valor), orden, pagina, tamano);

    private static async Task<T> ConAsync<TServicio, T>(ServiceProvider proveedor, Func<TServicio, Task<T>> operacion)
        where TServicio : notnull
    {
        using var alcance = proveedor.CreateScope();
        return await operacion(alcance.ServiceProvider.GetRequiredService<TServicio>());
    }

    [Theory]
    [Trait("Story", "3.3")]
    [Trait("AC", "42")]
    [Trait("AC", "81")]
    [InlineData(2, 25, 2)]
    [InlineData(9, 10, 3)]
    public async Task Listados_PaginanOrdenanYCuentanEnLaBase(int pagina, int esperados, int paginaDevuelta)
    {
        await using var proveedor = fixture.Proveedor();
        var especialidad = await proveedor.EspecialidadPropiaAsync();
        var marca = Marca();
        for (var i = 0; i < 60; i++)
            await proveedor.CrearMedicoAsync($"Nombre{i:00}", $"Pag{marca}{i:00}", $"T-{Marca().ToUpperInvariant()}", especialidad);

        var resultado = await ConAsync<IGestionMedicos, PaginaDeResultados<MedicoListado>>(proveedor,
            g => g.ListarPaginadoAsync(Consulta($"Pag{marca}", pagina: pagina), CancellationToken.None));

        Assert.Equal(60, resultado.Total);
        Assert.Equal(esperados, resultado.Items.Count);
        var primero = (paginaDevuelta - 1) * 25;
        Assert.Equal($"Pag{marca}{primero:00}", resultado.Items[0].Apellido);   // orden por apellido, en la base
        Assert.Equal(resultado.Items.OrderBy(m => m.Apellido, StringComparer.Ordinal).Select(m => m.Id), resultado.Items.Select(m => m.Id));
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "43")]
    public async Task Especialidades_TraenMedicosAsociados_YBuscanSinTildes()
    {
        await using var proveedor = fixture.Proveedor();
        var admin = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Administrador], nombre: $"Admin {Marca()}");
        var actor = UsuariosDePrueba.ActorDe(admin, Roles.Administrador);
        var marca = Marca();
        var cardiologia = await proveedor.CrearEspecialidadAsync(actor, nombre: $"Cardiología {marca}");
        var inactiva = await proveedor.CrearEspecialidadAsync(actor, activa: false, nombre: $"Cardiología infantil {marca}");
        for (var i = 0; i < 3; i++)
            await proveedor.MedicoPropioAsync(cardiologia);

        var activas = await ConAsync<ICatalogoEspecialidades, PaginaDeResultados<EspecialidadFila>>(proveedor,
            c => c.ListarPaginadoAsync(Consulta($"cardiologia {marca}", "nombre", filtros: ("estado", "activos")), CancellationToken.None));
        var todas = await ConAsync<ICatalogoEspecialidades, PaginaDeResultados<EspecialidadFila>>(proveedor,
            c => c.ListarPaginadoAsync(Consulta(marca.ToUpperInvariant(), "-medicos", filtros: ("estado", "todos")), CancellationToken.None));
        var inactivas = await ConAsync<ICatalogoEspecialidades, PaginaDeResultados<EspecialidadFila>>(proveedor,
            c => c.ListarPaginadoAsync(Consulta(marca, "nombre", filtros: ("estado", "inactivos")), CancellationToken.None));
        var contados = await ConAsync<ICatalogoEspecialidades, int>(proveedor, c => c.ContarMedicosAsync(cardiologia, CancellationToken.None));

        var fila = Assert.Single(activas.Items);
        Assert.Equal(new EspecialidadFila(cardiologia, $"Cardiología {marca}", true, 3), fila);
        Assert.Equal([cardiologia, inactiva], todas.Items.Select(e => e.Id));   // más médicos primero
        Assert.Equal(2, todas.Total);
        Assert.Equal([inactiva], inactivas.Items.Select(e => e.Id));
        Assert.Equal(3, contados);
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "44")]
    [Trait("AC", "82")]
    public async Task Medicos_FiltranPorEspecialidadYEstadoDeAgenda()
    {
        await using var proveedor = fixture.Proveedor(s => s.AddSingleton<TimeProvider>(Reloj(2026, 10, 16)));
        var especialidad = await proveedor.EspecialidadPropiaAsync();
        var otra = await proveedor.EspecialidadPropiaAsync();
        var sinAgenda = await proveedor.MedicoPropioAsync(especialidad, "Zeta");
        var vigente = await proveedor.MedicoPropioAsync(especialidad, "Alfa");
        var programada = await proveedor.MedicoPropioAsync(especialidad, "Beta");
        var pendiente = await proveedor.MedicoPropioAsync(especialidad, "Gama");
        await proveedor.MedicoPropioAsync(otra, "Alfa");
        await fixture.GuardarAgendaAsync(vigente, EstadoPropuesta.Aprobada, new DateOnly(2026, 10, 12), Reloj(2026, 10, 1));
        await fixture.GuardarAgendaAsync(programada, EstadoPropuesta.Aprobada, new DateOnly(2026, 11, 2));
        await fixture.GuardarAgendaAsync(pendiente, EstadoPropuesta.Enviada, new DateOnly(2026, 11, 9));

        async Task<long[]> IdsAsync(string? buscar, params (string, string)[] filtros) =>
            (await ConAsync<IGestionMedicos, PaginaDeResultados<MedicoListado>>(proveedor,
                g => g.ListarPaginadoAsync(Consulta(buscar, filtros: filtros), CancellationToken.None))).Items.Select(m => m.Id).ToArray();
        var esp = ("especialidad", especialidad.ToString());

        Assert.Equal([vigente, programada, pendiente, sinAgenda], await IdsAsync(null, esp));
        Assert.Equal([vigente], await IdsAsync(null, esp, ("agenda", "vigente")));
        Assert.Equal([programada], await IdsAsync(null, esp, ("agenda", "programada")));
        Assert.Equal([pendiente], await IdsAsync(null, esp, ("agenda", "pendiente")));
        Assert.Equal([sinAgenda], await IdsAsync(null, esp, ("agenda", "sin-agenda")));
        Assert.Equal([vigente, programada, pendiente, sinAgenda], await IdsAsync(new string('x', 101), esp));   // no filtra
        Assert.Equal([sinAgenda], await IdsAsync("zeta", esp));

        var porMatricula = await ConAsync<IGestionMedicos, PaginaDeResultados<MedicoListado>>(proveedor,
            g => g.ListarPaginadoAsync(Consulta(orden: "-matricula", filtros: esp), CancellationToken.None));
        Assert.Equal(porMatricula.Items.OrderByDescending(m => m.Matricula, StringComparer.Ordinal).Select(m => m.Id),
            porMatricula.Items.Select(m => m.Id));
        var conAgenda = porMatricula.Items.Single(m => m.Id == programada);
        Assert.Equal(new SituacionDeAgenda(EstadoAgendaMedico.SinAgenda, new DateOnly(2026, 11, 2)), conAgenda.Agenda);
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "45")]
    public async Task Usuarios_FiltranPorRolYEstado()
    {
        await using var proveedor = fixture.Proveedor();
        var marca = Marca();
        var recepcion = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Recepcionista], nombre: $"Beatriz {marca}");
        var medico = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Medico], nombre: $"Andrés {marca}");
        var admin = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Administrador], nombre: $"Carla {marca}");
        var inactivo = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Medico], activo: false, nombre: $"Daniel {marca}");

        async Task<Guid[]> IdsAsync(string orden, params (string, string)[] filtros) =>
            (await ConAsync<IGestionUsuariosPersonal, PaginaDeResultados<UsuarioPersonalFila>>(proveedor,
                g => g.ListarPaginadoAsync(Consulta(marca, orden, filtros: filtros), CancellationToken.None))).Items.Select(u => u.Id).ToArray();

        Assert.Equal([medico.Id, recepcion.Id, admin.Id], await IdsAsync("nombre", ("estado", "activos")));
        Assert.Equal([medico.Id, inactivo.Id], await IdsAsync("nombre", ("estado", "todos"), ("rol", Roles.Medico)));
        Assert.Equal([inactivo.Id], await IdsAsync("nombre", ("estado", "inactivos")));
        Assert.Equal([admin.Id, medico.Id, recepcion.Id], await IdsAsync("rol", ("estado", "activos")));
        Assert.Equal([recepcion.Id], await IdsAsync("nombre", ("estado", "activos"), ("rol", Roles.Recepcionista)));
        var porUsuario = await ConAsync<IGestionUsuariosPersonal, PaginaDeResultados<UsuarioPersonalFila>>(proveedor,
            g => g.ListarPaginadoAsync(Consulta(recepcion.UserName, "nombre"), CancellationToken.None));
        Assert.Equal(recepcion.Id, Assert.Single(porUsuario.Items).Id);
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "91")]
    public async Task ListasAnteriores_DevuelvenLoMismo()
    {
        await using var proveedor = fixture.Proveedor();
        var admin = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Administrador], nombre: $"Admin {Marca()}");
        var actor = UsuariosDePrueba.ActorDe(admin, Roles.Administrador);
        var marca = Marca();
        var b = await proveedor.CrearEspecialidadAsync(actor, nombre: $"Bbb {marca}");
        var a = await proveedor.CrearEspecialidadAsync(actor, activa: false, nombre: $"Aaa {marca}");
        var medicoB = await proveedor.MedicoPropioAsync(b, $"Lista{marca}B");
        var medicoA = await proveedor.MedicoPropioAsync(b, $"Lista{marca}A");

        var todas = await ConAsync<ICatalogoEspecialidades, IReadOnlyList<EspecialidadResumen>>(proveedor, c => c.ListarAsync(CancellationToken.None));
        var activas = await ConAsync<ICatalogoEspecialidades, IReadOnlyList<EspecialidadResumen>>(proveedor, c => c.ListarActivasAsync(CancellationToken.None));
        var medicos = await ConAsync<IGestionMedicos, IReadOnlyList<MedicoListado>>(proveedor, g => g.ListarAsync($"Lista{marca}", CancellationToken.None));

        Assert.Equal([new EspecialidadResumen(a, $"Aaa {marca}", false), new EspecialidadResumen(b, $"Bbb {marca}", true)],
            todas.Where(e => e.Nombre.EndsWith(marca, StringComparison.Ordinal)));
        Assert.Equal([new EspecialidadResumen(b, $"Bbb {marca}", true)], activas.Where(e => e.Nombre.EndsWith(marca, StringComparison.Ordinal)));
        Assert.Equal([medicoA, medicoB], medicos.Select(m => m.Id));
    }
}
