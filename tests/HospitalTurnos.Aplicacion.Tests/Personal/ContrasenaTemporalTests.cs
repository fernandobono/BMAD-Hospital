using HospitalTurnos.Infraestructura;
using HospitalTurnos.Infraestructura.Identidad;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalTurnos.Aplicacion.Tests.Personal;

public class ContrasenaTemporalTests
{
    [Fact]
    [Trait("Story", "2.2")]
    [Trait("AC", "4")]
    public async Task ContrasenaTemporal_Generar_CumplePoliticaSinConfundiblesYNoSeRepite()
    {
        // La política real sale de AddIdentidadPersonal. La cadena es ficticia: armar el UserManager no
        // abre conexión, y PasswordValidator sólo lee sus opciones. La prueba no toca PostgreSQL.
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:App"] = "Host=sin-base;Database=x;Username=x;Password=x",
            ["ConnectionStrings:Migraciones"] = "Host=sin-base;Database=x;Username=x;Password=x",
        }).Build();
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AgregarInfraestructura(configuracion);
        servicios.AddIdentidadPersonal(configuracion);
        await using var proveedor = servicios.BuildServiceProvider();
        using var alcance = proveedor.CreateScope();
        var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>();
        var validador = new PasswordValidator<UsuarioPersonal>();

        var generadas = new List<string>();
        for (var i = 0; i < 500; i++)
            generadas.Add(ContrasenaTemporal.Generar());

        foreach (var contrasena in generadas)
        {
            Assert.Equal(12, contrasena.Length);
            var resultado = await validador.ValidateAsync(usuarios, new UsuarioPersonal(), contrasena);
            Assert.True(resultado.Succeeded,
                $"{contrasena}: {string.Join(" ", resultado.Errors.Select(e => e.Code))}");
            Assert.DoesNotContain(contrasena, c => "0Oo1lI".Contains(c));
        }
        Assert.Equal(500, generadas.Distinct().Count());
    }
}
