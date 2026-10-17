using HospitalTurnos.Aplicacion.Tests.Ayudas;
using HospitalTurnos.Aplicacion.Tests.Catalogo;
using HospitalTurnos.Dominio.Agendas;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Infraestructura;
using HospitalTurnos.Infraestructura.Identidad;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace HospitalTurnos.Aplicacion.Tests.Agendas;

/// <summary>
/// El "Given" de las agendas (story 4.1). La base es compartida: cada prueba crea SUS médicos (con
/// DatosDeCatalogo de la 3.2) y guarda sus agendas, sin limpiar tablas. Las agendas llegan a cada estado sólo
/// con los métodos públicos del dominio, igual que el AgendaBuilder de Dominio.Tests.
/// </summary>
public static class DatosDeAgenda
{
    public static readonly TimeZoneInfo Cordoba = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Cordoba");

    /// <summary>Un reloj falso en hora LOCAL del hospital (Córdoba, UTC−3), guardada en UTC como el reloj real.</summary>
    public static FakeTimeProvider Reloj(int anio, int mes, int dia, int hora = 12, int minuto = 0)
    {
        var reloj = new FakeTimeProvider(
            new DateTimeOffset(anio, mes, dia, hora, minuto, 0, TimeSpan.FromHours(-3)).ToUniversalTime());
        reloj.SetLocalTimeZone(Cordoba);
        return reloj;
    }

    /// <summary>El reloj de envío por defecto: viernes 16/10/2026 12:00 (7 días antes del 23/10).</summary>
    public static FakeTimeProvider RelojDeEnvio() => Reloj(2026, 10, 16);

    /// <summary>El mismo contenedor que las pruebas de la 3.2; lo último registrado gana.</summary>
    public static ServiceProvider Proveedor(this PostgresFixture fixture, Action<IServiceCollection>? reemplazos = null)
    {
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:App"] = fixture.CadenaApp,
            ["ConnectionStrings:Migraciones"] = fixture.CadenaDuenio,
        }).Build();
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AgregarInfraestructura(configuracion);
        servicios.AddIdentidadPersonal(configuracion);
        reemplazos?.Invoke(servicios);
        return servicios.BuildServiceProvider();
    }

    public static string Marca() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>Una especialidad propia, creada por un administrador propio (la 3.1 pide un actor real).</summary>
    public static async Task<long> EspecialidadPropiaAsync(this ServiceProvider proveedor)
    {
        var admin = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Administrador], nombre: $"Admin {Marca()}");
        return await proveedor.CrearEspecialidadAsync(UsuariosDePrueba.ActorDe(admin, Roles.Administrador));
    }

    /// <summary>Un médico propio con matrícula T-MARCA (única: la base es compartida).</summary>
    public static Task<long> MedicoPropioAsync(this ServiceProvider proveedor, long especialidadId,
        string apellido = "Prueba", string nombre = "Ana") =>
        proveedor.CrearMedicoAsync(nombre, apellido, $"T-{Marca().ToUpperInvariant()}", especialidadId);

    public static async Task<long> MedicoPropioAsync(this ServiceProvider proveedor) =>
        await proveedor.MedicoPropioAsync(await proveedor.EspecialidadPropiaAsync());

    /// <summary>Arma la agenda en el estado pedido con los métodos públicos y la guarda con turnos_app.</summary>
    public static async Task<long> GuardarAgendaAsync(this PostgresFixture fixture, long medicoId, EstadoPropuesta estado,
        DateOnly vigenteDesde, TimeProvider? reloj = null, int minutos = 20, params Franja[] franjas)
    {
        var agenda = ArmarAgenda(medicoId, estado, vigenteDesde, reloj ?? RelojDeEnvio(), minutos, franjas);
        await using var contexto = ContextoDePrueba.Crear(fixture.CadenaApp);
        contexto.AgendasMedico.Add(agenda);
        await contexto.SaveChangesAsync();
        return agenda.Id;
    }

    public static AgendaMedico ArmarAgenda(long medicoId, EstadoPropuesta estado, DateOnly vigenteDesde,
        TimeProvider reloj, int minutos = 20, params Franja[] franjas)
    {
        var agenda = new AgendaMedico(medicoId, minutos, vigenteDesde);
        foreach (var franja in franjas.Length > 0 ? franjas : [Franja("lunes", "08:00", "12:00")])
            agenda.AgregarFranja(franja);
        if (estado == EstadoPropuesta.Borrador)
            return agenda;
        agenda.Enviar(reloj);
        if (estado == EstadoPropuesta.Aprobada)
            agenda.Aprobar(reloj);
        else if (estado == EstadoPropuesta.Rechazada)
            agenda.Rechazar("Motivo de prueba", reloj);
        return agenda;
    }

    public static Franja Franja(string dia, string desde, string hasta) =>
        new(dia switch
        {
            "lunes" => DayOfWeek.Monday,
            "martes" => DayOfWeek.Tuesday,
            "miércoles" => DayOfWeek.Wednesday,
            "jueves" => DayOfWeek.Thursday,      // de jueves a domingo los suma la 4.2
            "viernes" => DayOfWeek.Friday,
            "sábado" => DayOfWeek.Saturday,
            "domingo" => DayOfWeek.Sunday,
            _ => throw new ArgumentOutOfRangeException(nameof(dia)),
        }, TimeOnly.Parse(desde), TimeOnly.Parse(hasta));
}
