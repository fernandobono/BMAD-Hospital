using HospitalTurnos.Api.Dominio;
using Microsoft.EntityFrameworkCore;

namespace HospitalTurnos.Api.Datos;

public class HospitalDbContext(DbContextOptions<HospitalDbContext> options) : DbContext(options)
{
    public DbSet<Especialidad> Especialidades => Set<Especialidad>();
    public DbSet<Medico> Medicos => Set<Medico>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Turno> Turnos => Set<Turno>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Especialidad>(e =>
        {
            e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Nombre).IsUnique();
        });

        modelBuilder.Entity<Medico>(e =>
        {
            e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            e.Property(x => x.Apellido).HasMaxLength(100).IsRequired();
            e.Property(x => x.Matricula).HasMaxLength(30).IsRequired();
            e.HasIndex(x => x.Matricula).IsUnique();
            e.HasOne(x => x.Especialidad).WithMany().HasForeignKey(x => x.EspecialidadId);
            e.HasMany(x => x.Horarios).WithOne().HasForeignKey(h => h.MedicoId);
            e.Navigation(x => x.Horarios).HasField("_horarios");
            e.Ignore(x => x.NombreCompleto);
        });

        modelBuilder.Entity<Paciente>(e =>
        {
            e.Property(x => x.Dni).HasMaxLength(8).IsRequired();
            e.HasIndex(x => x.Dni).IsUnique();
            e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            e.Property(x => x.Apellido).HasMaxLength(100).IsRequired();
            e.Property(x => x.Telefono).HasMaxLength(30).IsRequired();
            e.Property(x => x.Email).HasMaxLength(200);
            e.Ignore(x => x.NombreCompleto);
        });

        modelBuilder.Entity<Turno>(e =>
        {
            e.HasOne(x => x.Paciente).WithMany().HasForeignKey(x => x.PacienteId);
            e.HasOne(x => x.Medico).WithMany().HasForeignKey(x => x.MedicoId);
            // Red de seguridad ante dos reservas simultáneas del mismo horario:
            // sólo puede haber un turno RESERVADO por médico y por paciente en cada horario.
            e.HasIndex(x => new { x.MedicoId, x.FechaHora }).IsUnique().HasFilter("\"Estado\" = 0");
            e.HasIndex(x => new { x.PacienteId, x.FechaHora }).IsUnique().HasFilter("\"Estado\" = 0");
        });
    }
}
