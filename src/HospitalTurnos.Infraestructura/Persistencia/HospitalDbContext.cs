using HospitalTurnos.Dominio.Auditoria;
using HospitalTurnos.Infraestructura.Email;
using HospitalTurnos.Infraestructura.Identidad;
using HospitalTurnos.Infraestructura.Persistencia.Configuraciones;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HospitalTurnos.Infraestructura.Persistencia;

/// <summary>
/// Contexto único de la aplicación. Las entidades las agregan las stories: 1.3 (auditoría),
/// 1.4 (bandeja de salida), 2.1 (usuarios y roles del personal, con Identity) y las siguientes.
/// La convención snake_case ya queda aplicada, también a las tablas de Identity.
/// </summary>
public class HospitalDbContext(DbContextOptions<HospitalDbContext> opciones)
    : IdentityDbContext<UsuarioPersonal, IdentityRole<Guid>, Guid>(opciones)
{
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();
    public DbSet<MensajeSaliente> MensajesSalientes => Set<MensajeSaliente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Primero Identity (define sus 7 tablas); la convención del final también las renombra.
        base.OnModelCreating(modelBuilder);
        // Activo sin HasDefaultValue(true) en la base: con un default de base, EF toma false como "sin
        // valor" y crearía activo a un usuario dado de alta desactivado. El true lo pone la clase.
        modelBuilder.Entity<UsuarioPersonal>().Property(x => x.NombreCompleto).HasMaxLength(120).IsRequired();
        // Cada configuración a mano (no hay ApplyConfigurationsFromAssembly) y ANTES de la convención.
        modelBuilder.ApplyConfiguration(new RegistroAuditoriaConfiguracion());
        modelBuilder.ApplyConfiguration(new MensajeSalienteConfiguracion());
        // Al final: así también renombra lo que configuren las entidades.
        ConvencionSnakeCase.Aplicar(modelBuilder);
    }
}
