using System.Xml.Linq;

namespace HospitalTurnos.Dominio.Tests.Arquitectura;

public class EstructuraDeLaSolucionTests
{
    // Tabla "Proyectos" del contrato técnico: proyecto → (carpeta, referencias).
    private static readonly Dictionary<string, (string Carpeta, string[] Referencias)> Contrato = new()
    {
        ["HospitalTurnos.Dominio"] = ("src", []),
        ["HospitalTurnos.Aplicacion"] = ("src", ["HospitalTurnos.Dominio"]),
        ["HospitalTurnos.Infraestructura"] = ("src", ["HospitalTurnos.Aplicacion", "HospitalTurnos.Dominio"]),
        ["HospitalTurnos.Web"] = ("src", ["HospitalTurnos.Aplicacion", "HospitalTurnos.Infraestructura"]),
        ["HospitalTurnos.Dominio.Tests"] = ("tests", ["HospitalTurnos.Dominio"]),
        ["HospitalTurnos.Aplicacion.Tests"] = ("tests", ["HospitalTurnos.Aplicacion", "HospitalTurnos.Infraestructura"]),
        ["HospitalTurnos.Web.Tests"] = ("tests", ["HospitalTurnos.Web"]),
    };

    // Armado por partes para que este mismo archivo no cuente como referencia al código viejo.
    private static readonly string NombreViejo = "HospitalTurnos" + ".Api";

    [Fact]
    [Trait("Story", "1.1")]
    [Trait("AC", "1")]
    [Trait("AC", "2")]
    [Trait("AC", "3")]
    [Trait("AC", "14")]
    public void Solucion_ProyectosYReferencias_CumplenElContratoTecnico()
    {
        var raiz = RaizDelRepo.Buscar();

        // AC 1: la solución lista exactamente los 7 proyectos.
        var solucion = XDocument.Load(Path.Combine(raiz, RaizDelRepo.ArchivoDeSolucion));
        var enSolucion = solucion.Descendants("Project")
            .Select(p => Path.GetFileNameWithoutExtension((string)p.Attribute("Path")!))
            .OrderBy(n => n)
            .ToArray();
        Assert.Equal(Contrato.Keys.OrderBy(n => n).ToArray(), enSolucion);

        // AC 1: el código inicial ya no existe.
        Assert.False(Directory.Exists(Path.Combine(raiz, "src", NombreViejo)), $"Sigue existiendo src/{NombreViejo}");
        Assert.False(Directory.Exists(Path.Combine(raiz, "tests", "HospitalTurnos.Tests")), "Sigue existiendo tests/HospitalTurnos.Tests");

        // AC 2 y AC 3: referencias del contrato y ningún PackageReference con Version.
        foreach (var (proyecto, (carpeta, esperadas)) in Contrato)
        {
            var csproj = XDocument.Load(Path.Combine(raiz, carpeta, proyecto, $"{proyecto}.csproj"));
            var referencias = csproj.Descendants("ProjectReference")
                .Select(r => Path.GetFileNameWithoutExtension(((string)r.Attribute("Include")!).Replace('\\', '/')))
                .OrderBy(n => n)
                .ToArray();
            Assert.True(esperadas.OrderBy(n => n).SequenceEqual(referencias),
                $"{proyecto}: referencias [{string.Join(", ", referencias)}], el contrato dice [{string.Join(", ", esperadas)}]");

            if (carpeta == "src")
                Assert.DoesNotContain("HospitalTurnos.Web", referencias.Where(_ => proyecto != "HospitalTurnos.Web"));

            var conVersion = csproj.Descendants("PackageReference").Where(p => p.Attribute("Version") is not null || p.Element("Version") is not null);
            Assert.True(!conVersion.Any(), $"{proyecto}: un PackageReference declara Version (va en Directory.Packages.props)");
        }

        // AC 3: gestión central de paquetes y advertencias como errores sólo en src/.
        var paquetes = XDocument.Load(Path.Combine(raiz, "Directory.Packages.props"));
        Assert.Equal("true", paquetes.Descendants("ManagePackageVersionsCentrally").Single().Value.Trim());
        var propsSrc = XDocument.Load(Path.Combine(raiz, "src", "Directory.Build.props"));
        Assert.Equal("true", propsSrc.Descendants("TreatWarningsAsErrors").Single().Value.Trim());

        // AC 14: nada de src/ ni tests/ referencia el código viejo.
        var referenciasAlViejo = new[] { "src", "tests" }
            .SelectMany(c => Directory.EnumerateFiles(Path.Combine(raiz, c), "*", SearchOption.AllDirectories))
            .Where(f => !EstaEnCarpetaGenerada(f))
            .Where(f => File.ReadAllText(f).Contains(NombreViejo))
            .ToArray();
        Assert.True(referenciasAlViejo.Length == 0, $"Referencian {NombreViejo}: {string.Join(", ", referenciasAlViejo)}");
    }

    private static bool EstaEnCarpetaGenerada(string archivo)
    {
        var partes = archivo.Split(Path.DirectorySeparatorChar);
        return partes.Contains("bin") || partes.Contains("obj") || partes.Contains("lib");
    }
}
