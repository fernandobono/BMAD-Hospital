using System.Text.RegularExpressions;

namespace HospitalTurnos.Web.Tests.Personal;

// Lee los archivos versionados tal como quedan en el repo: ninguno trae la contraseña del administrador.
public class ConfiguracionVersionadaTests
{
    private static string Raiz()
    {
        var carpeta = new DirectoryInfo(AppContext.BaseDirectory);
        while (carpeta is not null && !File.Exists(Path.Combine(carpeta.FullName, "HospitalTurnos.slnx")))
            carpeta = carpeta.Parent;
        return carpeta?.FullName ?? throw new InvalidOperationException("No se encontró HospitalTurnos.slnx");
    }

    [Fact]
    [Trait("Story", "2.1")]
    [Trait("AC", "20")]
    [Trait("AC", "27")]
    public void ConfiguracionVersionada_AppSettingsYCompose_NoTraenContrasenaDelAdministrador()
    {
        var raiz = Raiz();

        foreach (var archivo in Directory.GetFiles(Path.Combine(raiz, "src", "HospitalTurnos.Web"), "appsettings*.json"))
            Assert.DoesNotContain("Contrasena", File.ReadAllText(archivo), StringComparison.OrdinalIgnoreCase);

        var compose = File.ReadAllText(Path.Combine(raiz, "docker-compose.yml"));
        Assert.Contains("Personal__AdministradorInicial__Contrasena: ${ADMIN_INICIAL_CONTRASENA}", compose);
        Assert.Contains("Personal__AdministradorInicial__Usuario: ${ADMIN_INICIAL_USUARIO}", compose);
        Assert.Contains("Personal__AdministradorInicial__NombreCompleto: ${ADMIN_INICIAL_NOMBRE}", compose);
        Assert.DoesNotMatch(new Regex(@"\$\{[A-Z_]+:-"), compose);
        Assert.Matches(new Regex(@"ProteccionDeDatos__Carpeta:\s*(\S+)"), compose);
        var carpeta = Regex.Match(compose, @"ProteccionDeDatos__Carpeta:\s*(\S+)").Groups[1].Value;
        Assert.Contains($"claves-proteccion:{carpeta}", compose);

        var ejemplo = File.ReadAllLines(Path.Combine(raiz, ".env.ejemplo")).Select(l => l.Trim()).ToList();
        Assert.Contains(ejemplo, l => l.StartsWith("ADMIN_INICIAL_USUARIO=", StringComparison.Ordinal));
        Assert.Contains(ejemplo, l => l.StartsWith("ADMIN_INICIAL_NOMBRE=", StringComparison.Ordinal));
        Assert.Contains("ADMIN_INICIAL_CONTRASENA=", ejemplo);
    }
}
