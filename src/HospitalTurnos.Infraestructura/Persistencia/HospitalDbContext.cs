using HospitalTurnos.Dominio.Auditoria;
using HospitalTurnos.Infraestructura.Email;
using HospitalTurnos.Infraestructura.Persistencia.Configuraciones;
using Microsoft.EntityFrameworkCore;

namespace HospitalTurnos.Infraestructura.Persistencia;

/// <summary>
/// Contexto único de la aplicación. Las entidades las agregan las stories: 1.3 (auditoría),
/// 1.4 (bandeja de salida) y las siguientes. La convención snake_case ya queda aplicada.
/// </summary>
public class HospitalDbContext(DbContextOptions<HospitalDbContext> opciones) : DbContext(opciones)
{
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();
    public DbSet<MensajeSaliente> MensajesSalientes => Set<MensajeSaliente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Cada configuración a mano (no hay ApplyConfigurationsFromAssembly) y ANTES de la convención.
        modelBuilder.ApplyConfiguration(new RegistroAuditoriaConfiguracion());
        modelBuilder.ApplyConfiguration(new MensajeSalienteConfiguracion());
        // Al final: así también renombra lo que configuren las entidades.
        ConvencionSnakeCase.Aplicar(modelBuilder);
    }
}
