using HospitalTurnos.Dominio.Catalogo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalTurnos.Infraestructura.Persistencia.Configuraciones;

// Sin ToTable: el nombre especialidades y el del índice salen de la convención snake_case.
// Los permisos (la app nunca borra) viven en la migración AgregarEspecialidades.
public sealed class EspecialidadConfiguracion : IEntityTypeConfiguration<Especialidad>
{
    public void Configure(EntityTypeBuilder<Especialidad> especialidad)
    {
        especialidad.HasKey(e => e.Id); // long → identity (convención de Npgsql)
        especialidad.Property(e => e.Nombre).HasMaxLength(Especialidad.LargoMaximo).IsRequired();
        especialidad.Property(e => e.NombreNormalizado).HasMaxLength(Especialidad.LargoMaximo).IsRequired();
        // Activa sin HasDefaultValue(true): el true lo pone la clase (EF tomaría false como "sin valor").
        // El índice único es el que impide el duplicado aunque dos altas pasen la verificación a la vez (AC 7).
        especialidad.HasIndex(e => e.NombreNormalizado).IsUnique();
    }
}
