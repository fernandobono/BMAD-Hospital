using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HospitalTurnos.Infraestructura.Persistencia;

/// <summary>
/// Sólo para "dotnet ef migrations add": la herramienta tiene que construir el contexto pero NO se
/// conecta a la base. Sin esta fábrica intentaría leer ConnectionStrings:App, que no está en ningún
/// archivo versionado. Por eso la cadena no lleva usuario ni contraseña.
/// </summary>
public class FabricaDeContextoEnDiseno : IDesignTimeDbContextFactory<HospitalDbContext>
{
    public HospitalDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<HospitalDbContext>()
            .UseNpgsql("Host=localhost;Database=hospital_turnos")
            .Options);
}
