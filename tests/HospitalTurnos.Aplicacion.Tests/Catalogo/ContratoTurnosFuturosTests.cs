using HospitalTurnos.Aplicacion.Tests.Ayudas;
using Microsoft.EntityFrameworkCore;

namespace HospitalTurnos.Aplicacion.Tests.Catalogo;

// Prueba-trampa de la story 3.1 (AC 10 y 17, decisión 5): hoy no hay turnos, así que desactivar una
// especialidad no los consulta. En cuanto el modelo tenga turnos, esta prueba falla y manda a cumplir el
// contrato. Sin base: leer el modelo no abre conexión.
public class ContratoTurnosFuturosTests
{
    [Fact]
    [Trait("Story", "3.1")]
    [Trait("AC", "10")]
    [Trait("AC", "17")]
    public void ModeloDeDatos_SinTurnos_ElContratoDeTurnosFuturosSigueVigente()
    {
        using var contexto = ContextoDePrueba.Crear("Host=no-se-usa;Database=no-se-usa");

        var conTurnos = contexto.Model.GetEntityTypes()
            .Where(e => e.ClrType.Name == "Turno" || e.GetTableName() == "turnos")
            .Select(e => e.ClrType.Name)
            .ToList();

        Assert.True(conTurnos.Count == 0,
            "La story 5.3 agregó los turnos: cumplí el contrato de la story 3.1 (AC 17: desactivar una especialidad " +
            "con turnos futuros) y la herencia de la 3.2 (AC 23: no cambiar la especialidad de un médico con turnos " +
            "futuros), y reemplazá esta prueba por las de turnos futuros.");
    }
}
