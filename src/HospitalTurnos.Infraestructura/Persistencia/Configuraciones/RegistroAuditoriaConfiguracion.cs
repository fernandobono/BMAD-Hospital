using HospitalTurnos.Dominio.Auditoria;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalTurnos.Infraestructura.Persistencia.Configuraciones;

// Sin ToTable: el nombre registros_auditoria sale de la convención snake_case (story 1.1, AC 8).
// Los permisos y el disparador no se pueden expresar acá: viven en la migración AgregarRegistroAuditoria.
public sealed class RegistroAuditoriaConfiguracion : IEntityTypeConfiguration<RegistroAuditoria>
{
    public void Configure(EntityTypeBuilder<RegistroAuditoria> registro)
    {
        registro.HasKey(r => r.Id); // long → identity (convención de Npgsql)
        registro.Property(r => r.UsuarioId).HasMaxLength(100);
        registro.Property(r => r.Quien).HasMaxLength(200).IsRequired();
        registro.Property(r => r.Rol).HasMaxLength(50).IsRequired();
        // Como texto: un registro legal tiene que leerse sin conocer el enum.
        registro.Property(r => r.Canal).HasConversion<string>().HasMaxLength(20);
        registro.Property(r => r.Accion).HasMaxLength(100).IsRequired();
        registro.Property(r => r.EntidadTipo).HasMaxLength(100).IsRequired();
        registro.Property(r => r.EntidadId).HasMaxLength(100).IsRequired();
        registro.Property(r => r.Detalle).HasColumnType("jsonb");
    }
}
