using Microsoft.EntityFrameworkCore;

namespace HospitalTurnos.Infraestructura.Persistencia;

/// <summary>
/// Contexto único de la aplicación. En la story 1.1 no tiene entidades: las agregan 1.3 (auditoría),
/// 1.4 (bandeja de salida) y las siguientes. La convención snake_case ya queda aplicada.
/// </summary>
public class HospitalDbContext(DbContextOptions<HospitalDbContext> opciones) : DbContext(opciones)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Al final: así también renombra lo que configuren las entidades.
        ConvencionSnakeCase.Aplicar(modelBuilder);
    }
}
