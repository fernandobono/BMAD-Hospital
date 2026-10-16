using HospitalTurnos.Aplicacion.Tests.Ayudas;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Infraestructura;
using HospitalTurnos.Infraestructura.Identidad;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HospitalTurnos.Aplicacion.Tests.Personal;

// Cada prueba necesita una base SIN usuarios, que la compartida no garantiza: levanta su propio
// contenedor (como 1.1 T2). Cuestan unos segundos más; a cambio, no dependen del orden.
public class SembradorPersonalTests
{
    private const string AvisoSinConfiguracion =
        "No hay usuarios del personal y falta Personal:AdministradorInicial (Usuario y Contrasena). La aplicación arranca sin administrador: completá ADMIN_INICIAL_USUARIO y ADMIN_INICIAL_CONTRASENA en .env y reiniciá.";

    private static async Task ConBaseNuevaAsync(Func<PostgresFixture, Task> prueba)
    {
        var pg = new PostgresFixture();
        await pg.InitializeAsync();
        try
        {
            await prueba(pg);
        }
        finally
        {
            await pg.DisposeAsync();
        }
    }

    // valores: null en una clave = la clave no está en la configuración.
    private static ServiceProvider Proveedor(PostgresFixture pg, RegistroEnMemoria<SembradorPersonal> registro,
        IDictionary<string, string?> administrador, Action<IServiceCollection>? reemplazos = null)
    {
        var claves = new Dictionary<string, string?>
        {
            ["ConnectionStrings:App"] = pg.CadenaApp,
            ["ConnectionStrings:Migraciones"] = pg.CadenaDuenio,
        };
        foreach (var (clave, valor) in administrador)
            if (valor is not null)
                claves[$"Personal:AdministradorInicial:{clave}"] = valor;
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(claves).Build();
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AgregarInfraestructura(configuracion);
        servicios.AddIdentidadPersonal(configuracion);
        servicios.AddSingleton<ILogger<SembradorPersonal>>(registro);
        reemplazos?.Invoke(servicios);   // lo último registrado gana: así entran los dobles
        return servicios.BuildServiceProvider();
    }

    private static async Task SembrarAsync(ServiceProvider proveedor)
    {
        using var alcance = proveedor.CreateScope();
        await alcance.ServiceProvider.GetRequiredService<SembradorPersonal>().SembrarAsync();
    }

    private static async Task<(List<string> Roles, List<UsuarioPersonal> Usuarios)> EstadoAsync(ServiceProvider proveedor)
    {
        using var alcance = proveedor.CreateScope();
        var roles = await alcance.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>()
            .Roles.Select(r => r.Name!).OrderBy(n => n).ToListAsync();
        var usuarios = await alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>()
            .Users.AsNoTracking().ToListAsync();
        return (roles, usuarios);
    }

    private static readonly List<string> RolesOrdenados = [.. Roles.Todos.Order()];

    [Fact]
    [Trait("Story", "2.1")]
    [Trait("AC", "16")]
    [Trait("AC", "21")]
    public Task SembrarAsync_BaseSinUsuariosConConfiguracion_CreaRolesYAdministrador() => ConBaseNuevaAsync(async pg =>
    {
        var registro = new RegistroEnMemoria<SembradorPersonal>();
        await using var proveedor = Proveedor(pg, registro, new Dictionary<string, string?>
        {
            ["Usuario"] = "admin",
            ["Contrasena"] = "Clave-Inicial-2026",
            ["NombreCompleto"] = "Administración General",
        });

        await SembrarAsync(proveedor);

        var (roles, usuarios) = await EstadoAsync(proveedor);
        Assert.Equal(RolesOrdenados, roles);
        var admin = Assert.Single(usuarios);
        Assert.Equal("admin", admin.UserName);
        Assert.Equal("Administración General", admin.NombreCompleto);
        Assert.True(admin.Activo);
        Assert.NotEqual("Clave-Inicial-2026", admin.PasswordHash);
        using (var alcance = proveedor.CreateScope())
        {
            var usuariosIdentity = alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>();
            Assert.Equal([Roles.Administrador], await usuariosIdentity.GetRolesAsync(admin));
            var hasher = alcance.ServiceProvider.GetRequiredService<IPasswordHasher<UsuarioPersonal>>();
            Assert.Equal(PasswordVerificationResult.Success,
                hasher.VerifyHashedPassword(admin, admin.PasswordHash!, "Clave-Inicial-2026"));
        }
        Assert.Contains(registro.Entradas, e => e.Mensaje == "Administrador inicial creado: admin");
        Assert.All(registro.Entradas, e => Assert.DoesNotContain("Clave-Inicial-2026", e.Mensaje));
    });

    [Theory]
    [Trait("Story", "2.1")]
    [Trait("AC", "17")]
    [InlineData("Clave-Inicial-2026")]
    [InlineData("corta1")]
    public Task SembrarAsync_YaHayUsuarios_NoCreaNiModificaNiValida(string contrasenaConfigurada) => ConBaseNuevaAsync(async pg =>
    {
        var registro = new RegistroEnMemoria<SembradorPersonal>();
        await using var proveedor = Proveedor(pg, registro, new Dictionary<string, string?>
        {
            ["Usuario"] = "admin",
            ["Contrasena"] = contrasenaConfigurada,
        });
        string hashAntes, selloAntes;
        using (var alcance = proveedor.CreateScope())
        {
            var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>();
            var existente = new UsuarioPersonal { UserName = "recepcion1", NombreCompleto = "Carla Gómez" };
            Assert.True((await usuarios.CreateAsync(existente, "Recepcion-2026")).Succeeded);
            hashAntes = existente.PasswordHash!;
            selloAntes = existente.SecurityStamp!;
        }

        await SembrarAsync(proveedor);   // no lanza, ni siquiera con una contraseña fuera de política

        var (roles, usuariosDespues) = await EstadoAsync(proveedor);
        var unico = Assert.Single(usuariosDespues);
        Assert.Equal("recepcion1", unico.UserName);
        Assert.Equal(hashAntes, unico.PasswordHash);
        Assert.Equal(selloAntes, unico.SecurityStamp);
        Assert.Equal(RolesOrdenados, roles);   // los roles faltantes sí se crean
    });

    public static TheoryData<string, string?, string?> CasosSinConfiguracion => new()
    {
        // caso, Usuario, Contrasena (null = la clave no existe)
        { "sin la sección", null, null },
        { "vacíos", "", "" },
        { "sólo espacios", "  ", "  " },
        { "sin contraseña", "admin", "" },
    };

    [Theory]
    [Trait("Story", "2.1")]
    [Trait("AC", "18")]
    [MemberData(nameof(CasosSinConfiguracion))]
    public Task SembrarAsync_SinConfiguracionOVacia_SoloCreaRolesYAvisa(string caso, string? usuario, string? contrasena) =>
        ConBaseNuevaAsync(async pg =>
        {
            var registro = new RegistroEnMemoria<SembradorPersonal>();
            await using var proveedor = Proveedor(pg, registro, new Dictionary<string, string?>
            {
                ["Usuario"] = usuario,
                ["Contrasena"] = contrasena,
            });

            await SembrarAsync(proveedor);

            var (roles, usuarios) = await EstadoAsync(proveedor);
            Assert.Equal(RolesOrdenados, roles);
            Assert.True(usuarios.Count == 0, $"{caso}: {usuarios.Count} usuarios");
            var aviso = Assert.Single(registro.Entradas, e => e.Nivel == LogLevel.Warning);
            Assert.Equal(AvisoSinConfiguracion, aviso.Mensaje);
        });

    // Doble de RoleManager: todo real salvo crear un rol, que falla. Con la base real no se puede
    // provocar a pedido.
    private sealed class RolesQueNoSeCrean(
        IRoleStore<IdentityRole<Guid>> store, IEnumerable<IRoleValidator<IdentityRole<Guid>>> validadores,
        ILookupNormalizer normalizador, IdentityErrorDescriber errores, ILogger<RoleManager<IdentityRole<Guid>>> registro)
        : RoleManager<IdentityRole<Guid>>(store, validadores, normalizador, errores, registro)
    {
        public override Task<IdentityResult> CreateAsync(IdentityRole<Guid> role) =>
            Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "RolNoCreado" }));
    }

    [Fact]
    [Trait("Story", "2.1")]
    [Trait("AC", "16")]
    [Trait("AC", "19")]
    public Task SembrarAsync_NoSePuedeCrearUnRol_FallaSinCrearUsuario() => ConBaseNuevaAsync(async pg =>
    {
        var registro = new RegistroEnMemoria<SembradorPersonal>();
        await using var proveedor = Proveedor(pg, registro, new Dictionary<string, string?>
        {
            ["Usuario"] = "admin",
            ["Contrasena"] = "Clave-Inicial-2026",
        }, s => s.AddScoped<RoleManager<IdentityRole<Guid>>, RolesQueNoSeCrean>());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => SembrarAsync(proveedor));

        Assert.Contains("RolNoCreado", error.Message);
        Assert.Empty((await EstadoAsync(proveedor)).Usuarios);
    });

    [Fact]
    [Trait("Story", "2.1")]
    [Trait("AC", "19")]
    public Task SembrarAsync_ContrasenaFueraDePolitica_FallaSinCrearUsuario() => ConBaseNuevaAsync(async pg =>
    {
        var registro = new RegistroEnMemoria<SembradorPersonal>();
        await using var proveedor = Proveedor(pg, registro, new Dictionary<string, string?>
        {
            ["Usuario"] = "admin",
            ["Contrasena"] = "corta1",
        });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => SembrarAsync(proveedor));

        Assert.Contains("Personal:AdministradorInicial:Contrasena", error.Message);
        Assert.DoesNotContain("corta1", error.Message);
        Assert.Empty((await EstadoAsync(proveedor)).Usuarios);
    });
}
