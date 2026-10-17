using System.Text.Json;
using HospitalTurnos.Aplicacion.Agendas;
using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Tests.Ayudas;
using HospitalTurnos.Dominio.Agendas;
using HospitalTurnos.Dominio.Auditoria;
using HospitalTurnos.Dominio.Excepciones;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using static HospitalTurnos.Aplicacion.Tests.Agendas.DatosDeAgenda;
using static HospitalTurnos.Aplicacion.Tests.Agendas.DatosDePropuesta;

namespace HospitalTurnos.Aplicacion.Tests.Agendas;

// M2 (story 4.2) contra la base real: los índices, la concurrencia (xmin), la transacción y la auditoría viven
// ahí. Base compartida: cada prueba crea SUS médicos y filtra por su id; nunca se limpian tablas. "Hoy" es el
// viernes 16/10/2026 12:00 de Córdoba (el mínimo para regir es el 23/10/2026). Nadie espera tiempo real.
[Collection("Postgres")]
public class PropuestaAgendaTests(PostgresFixture fixture)
{
    private const string SinPermiso = "Sólo un médico activo puede armar o enviar su propuesta de agenda.";
    private const string SinVinculo = "Tu usuario no está vinculado a un médico. Pedíselo al administrador.";
    private const string YaHayEnviada =
        "Ya tenés una propuesta enviada esperando aprobación: hasta que el administrador la revise no podés modificarla ni armar otra.";
    private const string YaHayEnCurso = "Ya tenés una propuesta en curso: volvé a abrir la pantalla para seguir con ésa.";
    private const string CambioMientrasTanto =
        "Tu propuesta cambió mientras la guardabas: volvé a abrir la pantalla para ver cómo quedó.";

    private static readonly DateOnly Minimo = new(2026, 10, 23);
    private static readonly DateOnly Lunes2 = new(2026, 11, 2);
    private static readonly DateOnly Lunes30 = new(2026, 11, 30);

    private readonly FakeTimeProvider reloj = RelojDeEnvio();

    private ServiceProvider Proveedor(Action<IServiceCollection>? reemplazos = null) =>
        fixture.Proveedor(s =>
        {
            s.AddSingleton<TimeProvider>(reloj);
            reemplazos?.Invoke(s);
        });

    // Un segundo contenedor con la carrera: el "Given" se arma con uno SIN interceptor (se dispara en el primer guardado).
    private ServiceProvider ProveedorConCarrera(string sql) =>
        Proveedor(s => s.ConfigureDbContext<HospitalDbContext>(o => o.AddInterceptors(new InterceptorQueSeAdelanta(fixture.CadenaApp, sql))));

    private async Task<List<AgendaMedico>> AgendasDeAsync(long medicoId)
    {
        await using var contexto = ContextoDePrueba.Crear(fixture.CadenaApp);
        return await contexto.AgendasMedico.AsNoTracking().Include(a => a.Franjas)
            .Where(a => a.MedicoId == medicoId).OrderBy(a => a.Id).ToListAsync();
    }

    private async Task<object?> EscalarAsync(string sql, string? cadena = null)
    {
        await using var conexion = new NpgsqlConnection(cadena ?? fixture.CadenaDuenio);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        return await comando.ExecuteScalarAsync();
    }

    private async Task<long> AuditoriasDeAgendaAsync(long agendaId) =>
        (long)(await EscalarAsync(
            $"SELECT count(*) FROM registros_auditoria WHERE entidad_tipo = 'AgendaMedico' AND entidad_id = '{agendaId}'"))!;

    private async Task<long> EnviosAuditadosDeAsync(ActorAuditoria actor) =>
        (long)(await EscalarAsync(
            $"SELECT count(*) FROM registros_auditoria WHERE accion = 'propuesta-agenda.enviada' AND usuario_id = '{actor.UsuarioId}'"))!;

    private static string TextoDe(IEnumerable<Franja> franjas) =>
        string.Join(" | ", franjas.OrderBy(f => f.Dia).ThenBy(f => f.Desde).Select(f => $"{f.Dia} {f.Desde:HH\\:mm}-{f.Hasta:HH\\:mm}"));

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "2")]
    public async Task ObtenerAsync_SinVinculo_LoIndicaSinAgendas()
    {
        await using var proveedor = Proveedor();
        var usuario = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Medico]);

        var vista = await proveedor.ConPropuestaAsync(p => p.ObtenerAsync(UsuariosDePrueba.ActorDe(usuario, Roles.Medico), default));

        Assert.True(vista.SinVinculo);
        Assert.Null(vista.Vigente);
        Assert.Null(vista.ProximaAprobada);
        Assert.Null(vista.ParaCopiar);
        Assert.Null(vista.EnCurso);
    }

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "3")]
    [Trait("AC", "4")]
    [Trait("AC", "5")]
    [Trait("AC", "24")]
    public async Task ObtenerAsync_ConVariasAgendas_DevuelveVigenteProximaParaCopiarYEnCurso()
    {
        await using var proveedor = Proveedor();
        var (medico, actor, _) = await proveedor.MedicoConUsuarioAsync();
        var (otro, _, _) = await proveedor.MedicoConUsuarioAsync();
        var a = await fixture.GuardarAgendaAsync(medico, EstadoPropuesta.Aprobada, new DateOnly(2026, 10, 1),
            Reloj(2026, 9, 16), 20, Franja("lunes", "08:00", "12:00"));
        var b = await fixture.GuardarAgendaAsync(medico, EstadoPropuesta.Aprobada, Lunes30, null, 30, Franja("martes", "08:00", "12:00"));
        var r = await fixture.GuardarAgendaAsync(medico, EstadoPropuesta.Rechazada, Lunes2, null, 20,
            Franja("lunes", "14:00", "18:00"), Franja("miércoles", "08:00", "12:00"));
        await fixture.GuardarAgendaAsync(otro, EstadoPropuesta.Borrador, Lunes2);

        var vista = await proveedor.ConPropuestaAsync(p => p.ObtenerAsync(actor, default));

        Assert.False(vista.SinVinculo);
        Assert.Equal(a, vista.Vigente!.Id);
        Assert.Equal(b, vista.ProximaAprobada!.Id);
        Assert.Equal(b, vista.ParaCopiar!.Id);
        Assert.Equal(r, vista.EnCurso!.Id);
        Assert.Equal("Monday 14:00-18:00 | Wednesday 08:00-12:00", TextoDe(vista.EnCurso.Franjas));
        Assert.Equal(Minimo, vista.MinimoVigenteDesde);
    }

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "12")]
    public async Task GuardarBorradorAsync_SinPropuesta_CreaUnBorradorSinAuditar()
    {
        await using var proveedor = Proveedor();
        var (medico, actor, _) = await proveedor.MedicoConUsuarioAsync();

        var resultado = await proveedor.ConPropuestaAsync(p =>
            p.GuardarBorradorAsync(Datos(20, Lunes30, (DayOfWeek.Monday, "08:00", "12:00")), actor, default));

        Assert.Equal(ResultadoGuardado.Creada, resultado);
        var agenda = Assert.Single(await AgendasDeAsync(medico));
        Assert.Equal((EstadoPropuesta.Borrador, 20, Lunes30), (agenda.Estado, agenda.DuracionTurnoMinutos, agenda.VigenteDesde));
        Assert.Equal("Monday 08:00-12:00", TextoDe(agenda.Franjas));
        Assert.Equal(0, await AuditoriasDeAgendaAsync(agenda.Id));
    }

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "13")]
    public async Task GuardarBorradorAsync_ConCambios_AplicaSoloLasDiferencias()
    {
        await using var proveedor = Proveedor();
        var (medico, actor, _) = await proveedor.MedicoConUsuarioAsync();
        var id = await fixture.GuardarAgendaAsync(medico, EstadoPropuesta.Borrador, Lunes2, null, 20,
            Franja("lunes", "08:00", "12:00"), Franja("miércoles", "08:00", "12:00"));
        var franjaDelLunes = await EscalarAsync($"SELECT id FROM franjas_agenda WHERE agenda_medico_id = {id} AND dia = 1");

        var resultado = await proveedor.ConPropuestaAsync(p => p.GuardarBorradorAsync(Datos(30, Lunes2,
            (DayOfWeek.Monday, "08:00", "12:00"), (DayOfWeek.Wednesday, "14:00", "18:00"), (DayOfWeek.Friday, "08:00", "12:00")),
            actor, default));

        Assert.Equal(ResultadoGuardado.Guardada, resultado);
        var agenda = Assert.Single(await AgendasDeAsync(medico));
        Assert.Equal(30, agenda.DuracionTurnoMinutos);
        Assert.Equal("Monday 08:00-12:00 | Wednesday 14:00-18:00 | Friday 08:00-12:00", TextoDe(agenda.Franjas));
        Assert.Equal(franjaDelLunes, await EscalarAsync($"SELECT id FROM franjas_agenda WHERE agenda_medico_id = {id} AND dia = 1"));
    }

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "13")]
    public async Task GuardarBorradorAsync_SinCambios_NoGuardaYLaRechazadaSigueRechazada()
    {
        await using var proveedor = Proveedor();
        var (medico, actor, _) = await proveedor.MedicoConUsuarioAsync();
        await fixture.GuardarAgendaAsync(medico, EstadoPropuesta.Rechazada, Lunes2, null, 20,
            Franja("lunes", "08:00", "12:00"), Franja("miércoles", "08:00", "12:00"));

        var resultado = await proveedor.ConPropuestaAsync(p => p.GuardarBorradorAsync(Datos(20, Lunes2,
            (DayOfWeek.Wednesday, "08:00", "12:00"), (DayOfWeek.Monday, "08:00", "12:00")), actor, default));   // otro orden

        Assert.Equal(ResultadoGuardado.SinCambios, resultado);
        var agenda = Assert.Single(await AgendasDeAsync(medico));
        Assert.Equal(EstadoPropuesta.Rechazada, agenda.Estado);
        Assert.Equal("Motivo de prueba", agenda.MotivoRechazo);
    }

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "13")]
    public async Task GuardarBorradorAsync_RechazadaCorregida_PasaABorradorConElMotivo()
    {
        await using var proveedor = Proveedor();
        var (medico, actor, _) = await proveedor.MedicoConUsuarioAsync();
        await fixture.GuardarAgendaAsync(medico, EstadoPropuesta.Rechazada, Lunes2);
        var antes = Assert.Single(await AgendasDeAsync(medico));

        var resultado = await proveedor.ConPropuestaAsync(p =>
            p.GuardarBorradorAsync(Datos(30, Lunes2, (DayOfWeek.Monday, "08:00", "12:00")), actor, default));

        Assert.Equal(ResultadoGuardado.Guardada, resultado);
        var agenda = Assert.Single(await AgendasDeAsync(medico));
        Assert.Equal((EstadoPropuesta.Borrador, 30, "Motivo de prueba", antes.RechazadaEl),
            (agenda.Estado, agenda.DuracionTurnoMinutos, agenda.MotivoRechazo, agenda.RechazadaEl));
    }

    [Theory]
    [Trait("Story", "4.2")]
    [Trait("AC", "14")]
    [InlineData("superpuestas", "La franja del lunes de 10:00 a 14:00 se superpone con la de 08:00 a 12:00.")]
    [InlineData("duracion", "La duración del turno tiene que estar entre 10 y 60 minutos.")]
    [InlineData("horas", "La franja tiene que terminar después de empezar.")]
    public async Task GuardarBorradorAsync_DatosQueElDominioRechaza_LanzaSinGuardar(string caso, string mensaje)
    {
        await using var proveedor = Proveedor();
        var (medico, actor, _) = await proveedor.MedicoConUsuarioAsync();
        await fixture.GuardarAgendaAsync(medico, EstadoPropuesta.Borrador, Lunes2);
        var datos = caso switch
        {
            "superpuestas" => Datos(20, Lunes2, (DayOfWeek.Monday, "08:00", "12:00"), (DayOfWeek.Monday, "10:00", "14:00")),
            "duracion" => Datos(5, Lunes2, (DayOfWeek.Monday, "08:00", "12:00")),
            _ => Datos(20, Lunes2, (DayOfWeek.Monday, "12:00", "08:00")),
        };

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
            proveedor.ConPropuestaAsync(p => p.GuardarBorradorAsync(datos, actor, default)));

        Assert.Equal(mensaje, error.Message);
        var agenda = Assert.Single(await AgendasDeAsync(medico));
        Assert.Equal((20, "Monday 08:00-12:00"), (agenda.DuracionTurnoMinutos, TextoDe(agenda.Franjas)));
    }

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "15")]
    [Trait("AC", "17")]
    [Trait("AC", "24")]
    public async Task EnviarAsync_BorradorValido_GuardaEnviaYAuditaJuntos()
    {
        await using var proveedor = Proveedor();
        var (medico, actor, usuarioId) = await proveedor.MedicoConUsuarioAsync();
        var id = await fixture.GuardarAgendaAsync(medico, EstadoPropuesta.Borrador, Lunes2);

        await proveedor.ConPropuestaAsync(p => p.EnviarAsync(Datos(20, Minimo,
            (DayOfWeek.Monday, "08:00", "12:00"), (DayOfWeek.Thursday, "14:00", "18:00")),
            actor with { Quien = "Nombre de la sesión" }, default));

        var agenda = Assert.Single(await AgendasDeAsync(medico));
        Assert.Equal((EstadoPropuesta.Enviada, reloj.GetUtcNow(), Minimo), (agenda.Estado, agenda.EnviadaEl, agenda.VigenteDesde));
        Assert.Equal("Monday 08:00-12:00 | Thursday 14:00-18:00", TextoDe(agenda.Franjas));

        await using var conexion = new NpgsqlConnection(fixture.CadenaDuenio);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand($"""
            SELECT usuario_id, quien, rol, canal, accion, entidad_id, cuando, detalle::text FROM registros_auditoria
            WHERE entidad_tipo = 'AgendaMedico' AND entidad_id = '{id}'
            """, conexion);
        await using var lector = await comando.ExecuteReaderAsync();
        Assert.True(await lector.ReadAsync());
        Assert.Equal((usuarioId.ToString(), actor.Quien, Roles.Medico, "Personal", AccionesAuditoria.PropuestaAgendaEnviada, id.ToString()),
            (lector.GetString(0), lector.GetString(1), lector.GetString(2), lector.GetString(3), lector.GetString(4), lector.GetString(5)));
        Assert.Equal(reloj.GetUtcNow(), lector.GetFieldValue<DateTimeOffset>(6));
        using var detalle = JsonDocument.Parse(lector.GetString(7));
        var raiz = detalle.RootElement;
        Assert.Equal(medico, raiz.GetProperty("medicoId").GetInt64());
        Assert.Equal(20, raiz.GetProperty("duracionTurnoMinutos").GetInt32());
        Assert.Equal("2026-10-23", raiz.GetProperty("vigenteDesde").GetString());
        Assert.Equal(24, raiz.GetProperty("turnosPorSemana").GetInt32());
        // El formato de franjas del contrato (AC 17): día como en la base (0 = domingo), horas HH:mm.
        Assert.Equal(["1 08:00 12:00", "4 14:00 18:00"], raiz.GetProperty("franjas").EnumerateArray()
            .Select(f => $"{f.GetProperty("dia").GetInt32()} {f.GetProperty("desde").GetString()} {f.GetProperty("hasta").GetString()}"));
        Assert.False(await lector.ReadAsync());
    }

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "15")]
    [Trait("AC", "17")]
    public async Task EnviarAsync_SinPropuestaGuardada_CreaYEnviaEnUnPaso()
    {
        await using var proveedor = Proveedor();
        var (medico, actor, _) = await proveedor.MedicoConUsuarioAsync();

        await proveedor.ConPropuestaAsync(p => p.EnviarAsync(Datos(20, Minimo, (DayOfWeek.Friday, "20:00", "00:00")), actor, default));

        var agenda = Assert.Single(await AgendasDeAsync(medico));
        Assert.Equal(EstadoPropuesta.Enviada, agenda.Estado);
        Assert.Equal("Friday 20:00-00:00", TextoDe(agenda.Franjas));
        Assert.Equal(1, await AuditoriasDeAgendaAsync(agenda.Id));
    }

    [Theory]
    [Trait("Story", "4.2")]
    [Trait("AC", "16")]
    [InlineData("fecha")]
    [InlineData("sin franjas")]
    [InlineData("rechazada sin cambios")]
    public async Task EnviarAsync_ReglaQueFalla_NoGuardaNadaNiAudita(string caso)
    {
        await using var proveedor = Proveedor();
        var (medico, actor, _) = await proveedor.MedicoConUsuarioAsync();
        var estado = caso == "rechazada sin cambios" ? EstadoPropuesta.Rechazada : EstadoPropuesta.Borrador;
        var id = await fixture.GuardarAgendaAsync(medico, estado, Lunes2);
        var envio = () => proveedor.ConPropuestaAsync(p => p.EnviarAsync(caso switch
        {
            "fecha" => Datos(30, new DateOnly(2026, 10, 22), (DayOfWeek.Monday, "08:00", "12:00")),
            "sin franjas" => Datos(30, Minimo),
            _ => Datos(20, Lunes2, (DayOfWeek.Monday, "08:00", "12:00")),
        }, actor, default));

        Exception error = caso == "rechazada sin cambios"
            ? await Assert.ThrowsAsync<ConflictoException>(envio)
            : await Assert.ThrowsAsync<ReglaDeNegocioException>(envio);

        Assert.Equal(caso switch
        {
            "fecha" => "La agenda tiene que regir desde el 23/10/2026 o después: 7 días desde hoy.",
            "sin franjas" => "Agregá al menos una franja antes de enviar la propuesta.",
            _ => "Corregí la propuesta antes de reenviarla.",
        }, error.Message);
        var agenda = Assert.Single(await AgendasDeAsync(medico));
        Assert.Equal((20, estado, Lunes2, "Monday 08:00-12:00"),
            (agenda.DuracionTurnoMinutos, agenda.Estado, agenda.VigenteDesde, TextoDe(agenda.Franjas)));
        Assert.Equal(0, await AuditoriasDeAgendaAsync(id));
    }

    [Theory]
    [Trait("Story", "4.2")]
    [Trait("AC", "18")]
    [InlineData("guardar")]
    [InlineData("enviar")]
    public async Task EscriturasDePropuesta_YaHayUnaEnviada_LanzaConflictoSinTocarNada(string operacion)
    {
        await using var proveedor = Proveedor();
        var (medico, actor, _) = await proveedor.MedicoConUsuarioAsync();
        var enviada = await fixture.GuardarAgendaAsync(medico, EstadoPropuesta.Enviada, Lunes2);
        var datos = Datos(30, Lunes30, (DayOfWeek.Tuesday, "08:00", "12:00"));

        var error = await Assert.ThrowsAsync<ConflictoException>(() => proveedor.ConPropuestaAsync(p =>
            operacion == "guardar" ? p.GuardarBorradorAsync(datos, actor, default) : p.EnviarAsync(datos, actor, default)));

        Assert.Equal(YaHayEnviada, error.Message);
        var agenda = Assert.Single(await AgendasDeAsync(medico));
        Assert.Equal((enviada, EstadoPropuesta.Enviada, 20, Lunes2), (agenda.Id, agenda.Estado, agenda.DuracionTurnoMinutos, agenda.VigenteDesde));
    }

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "19")]
    [Trait("AC", "24")]
    public async Task EnviarAsync_OtraEnviadaSeAdelanta_TraduceElIndiceAConflicto()
    {
        long medico, borrador;
        ActorAuditoria actor;
        await using (var sinCarrera = Proveedor())
        {
            (medico, actor, _) = await sinCarrera.MedicoConUsuarioAsync();
            borrador = await fixture.GuardarAgendaAsync(medico, EstadoPropuesta.Borrador, Lunes2);
        }
        await using var conCarrera = ProveedorConCarrera(
            $"INSERT INTO agendas_medico (medico_id, duracion_turno_minutos, vigente_desde, estado, enviada_el) VALUES ({medico}, 20, '2026-12-01', 'Enviada', now())");

        var error = await Assert.ThrowsAsync<ConflictoException>(() => conCarrera.ConPropuestaAsync(p =>
            p.EnviarAsync(Datos(20, Minimo, (DayOfWeek.Monday, "08:00", "12:00")), actor, default)));

        Assert.Equal(YaHayEnviada, error.Message);
        var agendas = await AgendasDeAsync(medico);
        Assert.Equal(EstadoPropuesta.Borrador, agendas.Single(a => a.Id == borrador).Estado);
        Assert.Equal(new DateOnly(2026, 12, 1), Assert.Single(agendas, a => a.Estado == EstadoPropuesta.Enviada).VigenteDesde);
        Assert.Equal(0, await AuditoriasDeAgendaAsync(borrador));
    }

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "19")]
    public async Task GuardarBorradorAsync_OtroBorradorSeAdelanta_TraduceElIndiceAConflicto()
    {
        long medico;
        ActorAuditoria actor;
        await using (var sinCarrera = Proveedor())
            (medico, actor, _) = await sinCarrera.MedicoConUsuarioAsync();
        await using var conCarrera = ProveedorConCarrera(
            $"INSERT INTO agendas_medico (medico_id, duracion_turno_minutos, vigente_desde, estado) VALUES ({medico}, 20, '2026-12-01', 'Borrador')");

        var error = await Assert.ThrowsAsync<ConflictoException>(() => conCarrera.ConPropuestaAsync(p =>
            p.GuardarBorradorAsync(Datos(30, Lunes30, (DayOfWeek.Monday, "08:00", "12:00")), actor, default)));

        Assert.Equal(YaHayEnCurso, error.Message);
        var agenda = Assert.Single(await AgendasDeAsync(medico));
        Assert.Equal((EstadoPropuesta.Borrador, new DateOnly(2026, 12, 1)), (agenda.Estado, agenda.VigenteDesde));
    }

    [Theory]
    [Trait("Story", "4.2")]
    [Trait("AC", "19")]
    [InlineData("cambia la duracion")]
    [InlineData("solo agrega una franja")]
    public async Task GuardarBorradorAsync_LaPropuestaCambioMientrasTanto_TraduceLaConcurrenciaAConflicto(string caso)
    {
        long medico, id;
        ActorAuditoria actor;
        await using (var sinCarrera = Proveedor())
        {
            (medico, actor, _) = await sinCarrera.MedicoConUsuarioAsync();
            id = await fixture.GuardarAgendaAsync(medico, EstadoPropuesta.Borrador, Lunes2);
        }
        await using var conCarrera = ProveedorConCarrera($"UPDATE agendas_medico SET duracion_turno_minutos = 45 WHERE id = {id}");
        var datos = caso == "cambia la duracion"
            ? Datos(30, Lunes2, (DayOfWeek.Monday, "08:00", "12:00"))
            : Datos(20, Lunes2, (DayOfWeek.Monday, "08:00", "12:00"), (DayOfWeek.Tuesday, "08:00", "12:00"));

        var error = await Assert.ThrowsAsync<ConflictoException>(() =>
            conCarrera.ConPropuestaAsync(p => p.GuardarBorradorAsync(datos, actor, default)));

        Assert.Equal(CambioMientrasTanto, error.Message);
        var agenda = Assert.Single(await AgendasDeAsync(medico));
        Assert.Equal((45, "Monday 08:00-12:00"), (agenda.DuracionTurnoMinutos, TextoDe(agenda.Franjas)));
    }

    [Theory]
    [Trait("Story", "4.2")]
    [Trait("AC", "21")]
    [InlineData("guardar", "recepcionista")]
    [InlineData("guardar", "medico desactivado")]
    [InlineData("guardar", "medico sin vinculo")]
    [InlineData("enviar", "recepcionista")]
    [InlineData("enviar", "medico desactivado")]
    [InlineData("enviar", "medico sin vinculo")]
    public async Task EscriturasDePropuesta_ActorNoMedicoActivoOSinVinculo_LanzaAccesoDenegadoSinGuardar(string operacion, string quien)
    {
        await using var proveedor = Proveedor();
        long? medico = null;
        ActorAuditoria actor;
        switch (quien)
        {
            case "recepcionista":
                actor = UsuariosDePrueba.ActorDe(await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Recepcionista]), Roles.Medico);
                break;
            case "medico desactivado":
                (var id, actor, _) = await proveedor.MedicoConUsuarioAsync(activo: false);
                medico = id;
                break;
            default:
                actor = UsuariosDePrueba.ActorDe(await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Medico]), Roles.Medico);
                break;
        }
        var datos = Datos(20, Minimo, (DayOfWeek.Monday, "08:00", "12:00"));

        var error = await Assert.ThrowsAsync<AccesoDenegadoException>(() => proveedor.ConPropuestaAsync(p =>
            operacion == "guardar" ? p.GuardarBorradorAsync(datos, actor, default) : p.EnviarAsync(datos, actor, default)));

        Assert.Equal(quien == "medico sin vinculo" ? SinVinculo : SinPermiso, error.Message);
        if (medico is not null)
            Assert.Empty(await AgendasDeAsync(medico.Value));
        Assert.Equal(0, await EnviosAuditadosDeAsync(actor));
    }

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "22")]
    public async Task EscriturasDePropuesta_SoloTocanLaDelMedicoDeLaSesion()
    {
        await using var proveedor = Proveedor();
        var (m1, actor1, _) = await proveedor.MedicoConUsuarioAsync();
        var (m2, _, _) = await proveedor.MedicoConUsuarioAsync();
        await fixture.GuardarAgendaAsync(m1, EstadoPropuesta.Borrador, Lunes2);
        await fixture.GuardarAgendaAsync(m2, EstadoPropuesta.Borrador, Lunes2);
        var datos = Datos(30, Minimo, (DayOfWeek.Tuesday, "08:00", "12:00"));

        await proveedor.ConPropuestaAsync(p => p.GuardarBorradorAsync(datos, actor1, default));
        await proveedor.ConPropuestaAsync(p => p.EnviarAsync(datos, actor1, default));

        var deM1 = Assert.Single(await AgendasDeAsync(m1));
        Assert.Equal((EstadoPropuesta.Enviada, 30, "Tuesday 08:00-12:00"), (deM1.Estado, deM1.DuracionTurnoMinutos, TextoDe(deM1.Franjas)));
        var deM2 = Assert.Single(await AgendasDeAsync(m2));
        Assert.Equal((EstadoPropuesta.Borrador, 20, Lunes2, "Monday 08:00-12:00"),
            (deM2.Estado, deM2.DuracionTurnoMinutos, deM2.VigenteDesde, TextoDe(deM2.Franjas)));
    }

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "19")]
    [Trait("AC", "24")]
    [Trait("AC", "25")]
    public async Task Migracion_UnaPropuestaEditable_CreaElIndiceYConservaLosDeLa41()
    {
        var editable = (string?)await EscalarAsync(
            "SELECT indexdef FROM pg_indexes WHERE tablename = 'agendas_medico' AND indexname = 'ux_agendas_medico_una_editable'");
        var enviada = (string?)await EscalarAsync(
            "SELECT indexdef FROM pg_indexes WHERE tablename = 'agendas_medico' AND indexname = 'ux_agendas_medico_una_enviada'");

        Assert.NotNull(editable);
        Assert.StartsWith("CREATE UNIQUE INDEX", editable);
        Assert.Contains("(medico_id)", editable);
        Assert.Contains("'Borrador'", editable);
        Assert.Contains("'Rechazada'", editable);
        Assert.NotNull(enviada);
        Assert.StartsWith("CREATE UNIQUE INDEX", enviada);
        Assert.Contains("'Enviada'", enviada);
        Assert.DoesNotContain("Borrador", enviada);

        await using var proveedor = Proveedor();
        var medico = await proveedor.MedicoPropioAsync();
        await EscalarAsync($"INSERT INTO agendas_medico (medico_id, duracion_turno_minutos, vigente_desde, estado) VALUES ({medico}, 20, '2026-12-01', 'Borrador')",
            fixture.CadenaApp);
        var error = await Assert.ThrowsAsync<PostgresException>(() => EscalarAsync(
            $"INSERT INTO agendas_medico (medico_id, duracion_turno_minutos, vigente_desde, estado, motivo_rechazo, enviada_el, rechazada_el) VALUES ({medico}, 20, '2026-12-01', 'Rechazada', 'x', now(), now())",
            fixture.CadenaApp));
        Assert.Equal((PostgresErrorCodes.UniqueViolation, "ux_agendas_medico_una_editable"), (error.SqlState, error.ConstraintName));
        Assert.Equal(false, await EscalarAsync("SELECT has_table_privilege('turnos_app', 'registros_auditoria', 'UPDATE')"));
    }
}
