using HospitalTurnos.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace HospitalTurnos.Aplicacion.Tests.Ayudas;

/// <summary>Un HospitalDbContext suelto, sin el contenedor de servicios de la app, con la cadena que pida la prueba.</summary>
public static class ContextoDePrueba
{
    public static HospitalDbContext Crear(string cadena) =>
        new(new DbContextOptionsBuilder<HospitalDbContext>().UseNpgsql(cadena).Options);
}
