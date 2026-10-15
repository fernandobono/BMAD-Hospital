namespace HospitalTurnos.Dominio.Tests.Arquitectura;

/// <summary>
/// Encuentra la raíz del repo subiendo desde la carpeta de salida de las pruebas hasta el
/// HospitalTurnos.slnx. Mismo patrón que BuscarRaizDelRepo del proyecto guía.
/// </summary>
public static class RaizDelRepo
{
    public const string ArchivoDeSolucion = "HospitalTurnos.slnx";

    public static string Buscar()
    {
        var carpeta = new DirectoryInfo(AppContext.BaseDirectory);
        while (carpeta is not null)
        {
            if (File.Exists(Path.Combine(carpeta.FullName, ArchivoDeSolucion)))
                return carpeta.FullName;
            carpeta = carpeta.Parent;
        }
        throw new InvalidOperationException(
            $"No se encontró {ArchivoDeSolucion} subiendo desde {AppContext.BaseDirectory}");
    }

    public static string Ruta(params string[] partes) =>
        Path.Combine([Buscar(), .. partes]);
}
