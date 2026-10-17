using HospitalTurnos.Dominio.Agendas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalTurnos.Infraestructura.Persistencia.Configuraciones;

// Las franjas de cada agenda. DayOfWeek va como integer (0 = domingo); DateOnly/TimeOnly los mapea Npgsql a
// date y time sin configuración. hasta = 00:00 es la medianoche: por eso el control acepta ese caso.
public sealed class FranjaConfiguracion : IEntityTypeConfiguration<Franja>
{
    public void Configure(EntityTypeBuilder<Franja> franja)
    {
        franja.HasKey(f => f.Id);
        franja.ToTable("franjas_agenda", t =>
        {
            t.HasCheckConstraint("ck_franjas_agenda_dia", "dia BETWEEN 0 AND 6");
            t.HasCheckConstraint("ck_franjas_agenda_horas", "hasta > desde OR hasta = '00:00'");
        });
    }
}
