using DotNet.Testcontainers.Configurations;
using HospitalTurnos.Infraestructura.Persistencia;
using Npgsql;
using Testcontainers.PostgreSql;

namespace HospitalTurnos.Web.Tests;

/// <summary>
/// PostgreSQL 17 real en un contenedor, con los MISMOS dos roles que el Compose: el script
/// docker/postgres/initdb/01-roles.sh se copia al contenedor (contrato técnico, C-1). Crear los roles
/// con SQL propio no reproduciría los permisos reales (GRANT, REVOKE y privilegios por defecto).
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private const string Base = "hospital_turnos";

    // Contraseñas al azar por corrida: ninguna clave queda escrita en el código de pruebas.
    private readonly string _claveOwner = Guid.NewGuid().ToString("N");
    private readonly string _claveApp = Guid.NewGuid().ToString("N");
    private readonly bool _aplicarMigraciones;

    public PostgresFixture() : this(Unix.FileMode755, aplicarMigraciones: true)
    {
    }

    // Para las pruebas que necesitan su propio contenedor: T2 lo detiene y T12 copia el script
    // sin bit ejecutable. Interno para que xUnit vea un solo constructor público.
    internal PostgresFixture(UnixFileModes modoDelScript, bool aplicarMigraciones)
    {
        _aplicarMigraciones = aplicarMigraciones;
        var script = Path.Combine(AppContext.BaseDirectory, "initdb", "01-roles.sh");
        Contenedor = new PostgreSqlBuilder("postgres:17")
            .WithDatabase(Base)
            .WithUsername("postgres")
            .WithPassword(Guid.NewGuid().ToString("N"))
            .WithEnvironment("TURNOS_OWNER_PASSWORD", _claveOwner)
            .WithEnvironment("TURNOS_APP_PASSWORD", _claveApp)
            .WithResourceMapping(new FileInfo(script), new FileInfo("/docker-entrypoint-initdb.d/01-roles.sh"), fileMode: modoDelScript)
            .Build();
    }

    public PostgreSqlContainer Contenedor { get; }

    /// <summary>Cadena con el rol dueño del esquema (turnos_owner): sólo para migraciones.</summary>
    public string CadenaDuenio { get; private set; } = "";

    /// <summary>Cadena con el rol de la aplicación (turnos_app): sin permiso para cambiar el esquema.</summary>
    public string CadenaApp { get; private set; } = "";

    /// <summary>Cadena con el superusuario postgres, para inspeccionar roles y permisos.</summary>
    public string CadenaSuperusuario => Contenedor.GetConnectionString();

    public async Task InitializeAsync()
    {
        await Contenedor.StartAsync();
        CadenaDuenio = ConRol("turnos_owner", _claveOwner);
        CadenaApp = ConRol("turnos_app", _claveApp);
        if (_aplicarMigraciones)
            await AplicadorDeMigraciones.AplicarAsync(CadenaDuenio);
    }

    public Task DisposeAsync() => Contenedor.DisposeAsync().AsTask();

    private string ConRol(string usuario, string clave) =>
        new NpgsqlConnectionStringBuilder(Contenedor.GetConnectionString())
        {
            Username = usuario,
            Password = clave,
        }.ConnectionString;
}
