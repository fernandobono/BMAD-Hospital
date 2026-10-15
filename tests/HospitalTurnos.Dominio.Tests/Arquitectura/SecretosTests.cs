using System.Text.RegularExpressions;

namespace HospitalTurnos.Dominio.Tests.Arquitectura;

public class SecretosTests
{
    private static readonly string[] VariablesDeClave = ["POSTGRES_PASSWORD", "TURNOS_OWNER_PASSWORD", "TURNOS_APP_PASSWORD"];

    [Fact]
    [Trait("Story", "1.1")]
    [Trait("AC", "16")]
    public void Repositorio_ArchivosVersionados_NoTienenContrasenas()
    {
        var problemas = new List<string>();

        // docker-compose.yml: cada clave obligatoria con ${X:?…} y ninguna con valor por defecto ${X:-…}.
        var compose = File.ReadAllLines(RaizDelRepo.Ruta("docker-compose.yml"));
        for (var i = 0; i < compose.Length; i++)
        {
            if (Regex.IsMatch(compose[i], @"\$\{[A-Z_]+:-"))
                problemas.Add($"docker-compose.yml:{i + 1} tiene un valor por defecto ${{…:-…}}: {compose[i].Trim()}");
        }
        foreach (var variable in VariablesDeClave)
        {
            if (!compose.Any(l => l.Contains($"${{{variable}:?")))
                problemas.Add($"docker-compose.yml no exige {variable} con ${{{variable}:?…}}");
        }

        // appsettings*.json: sin cadenas de conexión ni contraseñas.
        foreach (var archivo in new[] { "appsettings.json", "appsettings.Development.json" })
        {
            var lineas = File.ReadAllLines(RaizDelRepo.Ruta("src", "HospitalTurnos.Web", archivo));
            for (var i = 0; i < lineas.Length; i++)
            {
                if (lineas[i].Contains("ConnectionStrings", StringComparison.OrdinalIgnoreCase)
                    || lineas[i].Contains("Password", StringComparison.OrdinalIgnoreCase))
                    problemas.Add($"{archivo}:{i + 1} tiene una cadena o contraseña: {lineas[i].Trim()}");
            }
        }

        // .env.ejemplo define las tres variables.
        var ejemplo = File.ReadAllLines(RaizDelRepo.Ruta(".env.ejemplo"));
        foreach (var variable in VariablesDeClave)
        {
            if (!ejemplo.Any(l => l.TrimStart().StartsWith($"{variable}=")))
                problemas.Add($".env.ejemplo no define {variable}");
        }

        // .gitignore ignora .env.
        var gitignore = File.ReadAllLines(RaizDelRepo.Ruta(".gitignore")).Select(l => l.Trim());
        if (!gitignore.Contains(".env"))
            problemas.Add(".gitignore no tiene la línea .env");

        Assert.True(problemas.Count == 0, string.Join(Environment.NewLine, problemas));
    }
}
