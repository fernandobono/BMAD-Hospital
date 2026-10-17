using HospitalTurnos.Dominio.Agendas;
using HospitalTurnos.Dominio.Catalogo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalTurnos.Infraestructura.Persistencia.Configuraciones;

// Sin ToTable con nombre: agendas_medico sale de la convención snake_case. Los permisos (la app nunca borra una
// agenda, ADR-06) viven en la migración AgregarAgendas. Los nombres de índices y controles ya en snake_case.
public sealed class AgendaMedicoConfiguracion : IEntityTypeConfiguration<AgendaMedico>
{
    public const string IndiceUnaEnviada = "ux_agendas_medico_una_enviada";   // la 4.2 traduce su 23505
    public const string IndiceUnaEditable = "ux_agendas_medico_una_editable"; // la 4.2 traduce su 23505

    public void Configure(EntityTypeBuilder<AgendaMedico> agenda)
    {
        agenda.HasKey(a => a.Id);                                                   // long → identity
        agenda.Property(a => a.Estado).HasConversion<string>().HasMaxLength(20);    // como Canal (1.3)
        agenda.Property(a => a.MotivoRechazo).HasMaxLength(AgendaMedico.LargoMaximoMotivo);
        // Control de concurrencia con la columna de sistema xmin de PostgreSQL: no crea columna (AC 27). El nombre
        // va explícito: sin él, la convención snake_case lo renombra a "version" y la migración crea una columna nueva.
        agenda.Property<uint>("Version").IsRowVersion().HasColumnName("xmin");
        agenda.HasOne<Medico>().WithMany().HasForeignKey(a => a.MedicoId).OnDelete(DeleteBehavior.Restrict);
        agenda.HasMany(a => a.Franjas).WithOne().HasForeignKey("AgendaMedicoId")
              .IsRequired()                                                         // NOT NULL: sin franjas huérfanas
              .OnDelete(DeleteBehavior.Cascade);
        agenda.Navigation(a => a.Franjas).UsePropertyAccessMode(PropertyAccessMode.Field);   // usa _franjas
        // Una sola enviada por médico (FR-14b): la segunda defensa; la primera, con mensaje claro, es de la 4.2.
        agenda.HasIndex(a => a.MedicoId).IsUnique().HasFilter("estado = 'Enviada'").HasDatabaseName(IndiceUnaEnviada);
        // Una sola propuesta editable por médico (decisión 2 de la 4.2). CON NOMBRE: un HasIndex(a => a.MedicoId) sin
        // nombre devolvería el índice de la una-enviada y le cambiaría el filtro.
        agenda.HasIndex(a => a.MedicoId, IndiceUnaEditable).IsUnique().HasFilter("estado IN ('Borrador', 'Rechazada')")
              .HasDatabaseName(IndiceUnaEditable);
        agenda.HasIndex(a => new { a.MedicoId, a.Estado, a.VigenteDesde });       // la consulta del AC 29
        agenda.ToTable(t => t.HasCheckConstraint("ck_agendas_medico_duracion", "duracion_turno_minutos BETWEEN 10 AND 60"));
    }
}
