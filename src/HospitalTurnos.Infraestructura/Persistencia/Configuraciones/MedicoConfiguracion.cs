using HospitalTurnos.Dominio.Catalogo;
using HospitalTurnos.Infraestructura.Identidad;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalTurnos.Infraestructura.Persistencia.Configuraciones;

// Sin ToTable: el nombre medicos sale de la convención snake_case. Los permisos (la app nunca borra un
// médico) viven en la migración AgregarMedicos. La FK al usuario la declara Infraestructura: el dominio
// guarda sólo el Guid.
public sealed class MedicoConfiguracion : IEntityTypeConfiguration<Medico>
{
    public const string IndiceMatricula = "ix_medicos_matricula";          // nombres ya en snake_case:
    public const string IndiceUsuario = "ix_medicos_usuario_personal_id";  // los compara la traducción del 23505

    public void Configure(EntityTypeBuilder<Medico> medico)
    {
        medico.HasKey(m => m.Id); // long → identity (convención de Npgsql)
        medico.Property(m => m.Nombre).HasMaxLength(Medico.LargoMaximoNombre).IsRequired();
        medico.Property(m => m.Apellido).HasMaxLength(Medico.LargoMaximoNombre).IsRequired();
        medico.Property(m => m.Matricula).HasMaxLength(Medico.LargoMaximoMatricula).IsRequired();
        // Los índices únicos son los que garantizan las reglas con dos administradores a la vez (AC 8 y 12).
        medico.HasIndex(m => m.Matricula).IsUnique().HasDatabaseName(IndiceMatricula);
        medico.HasIndex(m => m.UsuarioPersonalId).IsUnique().HasDatabaseName(IndiceUsuario); // varios NULL valen en PostgreSQL
        medico.HasOne<Especialidad>().WithMany().HasForeignKey(m => m.EspecialidadId).OnDelete(DeleteBehavior.Restrict);
        medico.HasOne<UsuarioPersonal>().WithMany().HasForeignKey(m => m.UsuarioPersonalId).OnDelete(DeleteBehavior.Restrict);
        medico.Ignore(m => m.NombreCompleto);
    }
}
