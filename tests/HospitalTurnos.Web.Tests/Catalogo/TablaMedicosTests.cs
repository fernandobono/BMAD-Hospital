using Bunit;
using HospitalTurnos.Aplicacion.Catalogo;
using HospitalTurnos.Dominio.Agendas;
using HospitalTurnos.Web.Components.Personal.Administracion;

namespace HospitalTurnos.Web.Tests.Catalogo;

// Lo que depende sólo de los datos al dibujar: los cuatro textos del estado de agenda (la 4.1 conecta el
// cálculo real sin tocar la tabla) y el vacío, que por HTTP no se ve en una base compartida.
public class TablaMedicosTests : BunitContext
{
    [Fact]
    [Trait("Story", "3.2")]
    [Trait("AC", "3")]
    [Trait("AC", "4")]
    [Trait("AC", "23")]
    public void TablaMedicos_SegunEstadoDeAgendaYVacio_MuestraElTextoQueCorresponde()
    {
        IReadOnlyList<MedicoListado> medicos =
        [
            new(1, "Alba", "Ana", "MP 1", "Clínica", SituacionDeAgenda.SinAgenda),
            new(2, "Bravo", "Beto", "MP 2", "Clínica", new(EstadoAgendaMedico.SinAgenda, new DateOnly(2026, 12, 1))),
            new(3, "Castro", "Carla", "MP 3", "Clínica", new(EstadoAgendaMedico.Vigente)),
            new(4, "Díaz", "Dante", "MP 4", "Clínica", new(EstadoAgendaMedico.PropuestaPendiente)),
        ];

        var tabla = Render<TablaMedicos>(p => p.Add(t => t.Medicos, medicos));

        var celdas = tabla.FindAll("tbody tr").Select(f => f.QuerySelectorAll("td").Last().TextContent.Trim()).ToList();
        Assert.Equal(["Sin agenda", "Sin agenda (rige desde 01/12)", "Vigente", "Propuesta pendiente"], celdas);

        var vacia = Render<TablaMedicos>(p => p.Add(t => t.Medicos, []));

        Assert.Contains("Todavía no hay médicos cargados. Usá «Nuevo médico» para dar de alta el primero.", vacia.Markup);
        Assert.Empty(vacia.FindAll("table"));
    }
}
