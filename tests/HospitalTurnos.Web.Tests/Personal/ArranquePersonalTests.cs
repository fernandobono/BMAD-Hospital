using System.Net;
using HospitalTurnos.Infraestructura.Identidad;
using HospitalTurnos.Web.Tests.Ayudas;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HospitalTurnos.Web.Tests.Personal;

public class ArranquePersonalTests
{
    [Fact]
    [Trait("Story", "2.1")]
    [Trait("AC", "18")]
    public async Task Arranque_AdministradorInicialVacio_ArrancaSinSembrarYAvisa()
    {
        // Base recién creada, sin usuarios (como 1.1 T2). No se usa la fábrica de 2.1: siembra los fijos.
        var pg = new PostgresFixture();
        await pg.InitializeAsync();
        try
        {
            var registro = new ProveedorDeRegistroEnMemoria();
            await using var fabrica = new HospitalWebApplicationFactory(pg);
            await using var app = fabrica.WithWebHostBuilder(b =>
            {
                // Como Compose sin .env: las variables llegan, pero vacías.
                b.ConfigureAppConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Personal:AdministradorInicial:Usuario"] = "",
                    ["Personal:AdministradorInicial:Contrasena"] = "",
                }));
                b.ConfigureTestServices(s => s.AddSingleton<ILoggerProvider>(registro));
            });

            var cliente = app.CreateClient();   // no lanza: el arranque sigue sin administrador
            var salud = await cliente.GetAsync("/salud");

            Assert.Equal(HttpStatusCode.OK, salud.StatusCode);
            using (var alcance = app.Services.CreateScope())
            {
                var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>();
                Assert.Equal(0, await usuarios.Users.CountAsync());
            }
            Assert.Contains(registro.Entradas, e =>
                e.Categoria == typeof(SembradorPersonal).FullName && e.Nivel == LogLevel.Warning
                && e.Mensaje.StartsWith("No hay usuarios del personal y falta Personal:AdministradorInicial", StringComparison.Ordinal));
        }
        finally
        {
            await pg.DisposeAsync();
        }
    }
}
