using HospitalTurnos.Infraestructura.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalTurnos.Infraestructura.Persistencia.Configuraciones;

// Sin ToTable: el nombre mensajes_salientes sale de la convención snake_case (story 1.1, AC 8).
// Los permisos (la app no borra) viven en la migración AgregarBandejaDeSalida.
public sealed class MensajeSalienteConfiguracion : IEntityTypeConfiguration<MensajeSaliente>
{
    public void Configure(EntityTypeBuilder<MensajeSaliente> mensaje)
    {
        mensaje.HasKey(m => m.Id); // long → identity (convención de Npgsql)
        mensaje.Property(m => m.Para).HasMaxLength(320).IsRequired();
        mensaje.Property(m => m.Asunto).HasMaxLength(200).IsRequired();
        mensaje.Property(m => m.CuerpoTexto).HasColumnType("text").IsRequired();
        mensaje.Property(m => m.CuerpoHtml).HasColumnType("text").IsRequired();
        mensaje.Property(m => m.Tipo).HasMaxLength(50).IsRequired();
        mensaje.Property(m => m.ReferenciaId).HasMaxLength(100);
        mensaje.Property(m => m.UltimoError).HasMaxLength(MensajeSaliente.LargoMaximoError);
        // La consulta de cada ciclo sólo mira los pendientes: el índice parcial no crece con el historial.
        mensaje.HasIndex(m => m.ProximoIntentoEl)
            .HasFilter("enviado_el IS NULL AND fallo_definitivo_el IS NULL");
    }
}
