namespace HospitalTurnos.Dominio.Tests.Arquitectura;

public class HoraDelSistemaTests
{
    // Las cinco formas de leer el reloj del sistema sin pasar por TimeProvider.
    private static readonly string[] LecturasDelSistema =
        ["DateTime.Now", "DateTime.UtcNow", "DateTime.Today", "DateTimeOffset.Now", "DateTimeOffset.UtcNow"];

    [Fact]
    [Trait("Story", "1.2")]
    [Trait("AC", "6")]
    public void CodigoFuente_ArchivosDeSrc_NoLeenLaHoraDelSistema()
    {
        var src = RaizDelRepo.Ruta("src");
        var separador = Path.DirectorySeparatorChar;
        var reloj = Path.Combine("HospitalTurnos.Infraestructura", "Tiempo", "RelojHospital.cs");
        var migraciones = Path.Combine("Persistencia", "Migraciones") + separador;
        var apariciones = new List<string>();

        foreach (var archivo in Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories))
        {
            var relativo = Path.GetRelativePath(src, archivo);
            if (relativo.Contains($"{separador}bin{separador}") || relativo.Contains($"{separador}obj{separador}")
                || relativo == reloj || relativo.Contains(migraciones))
                continue;

            var lineas = File.ReadAllLines(archivo);
            for (var i = 0; i < lineas.Length; i++)
            {
                foreach (var lectura in LecturasDelSistema)
                {
                    // Coincidencia exacta: "DateTime.Now" no cuenta dentro de "DateTime.NowAlgo".
                    var desde = 0;
                    while ((desde = lineas[i].IndexOf(lectura, desde, StringComparison.Ordinal)) >= 0)
                    {
                        var fin = desde + lectura.Length;
                        if (fin >= lineas[i].Length || !char.IsLetterOrDigit(lineas[i][fin]))
                            apariciones.Add($"{relativo}:{i + 1} usa {lectura}: {lineas[i].Trim()}");
                        desde = fin;
                    }
                }
            }
        }

        Assert.True(apariciones.Count == 0,
            "Se lee la hora del sistema fuera de RelojHospital:\n" + string.Join("\n", apariciones));
    }
}
