using Bunit;
using HospitalTurnos.Dominio.Agendas;
using HospitalTurnos.Web.Components.Personal.Administracion;

namespace HospitalTurnos.Web.Tests.Catalogo;

// El estado de agenda de A1 como badge (story 3.3, AC 61; guía "Estados de negocio como badges"): cada SituacionDeAgenda
// del cálculo de la 4.1 tiene su texto y su color; "Sin agenda (rige desde dd/MM)" ya no aparece. Antes era texto
// plano (story 3.2, AC 4 y 23): el contrato con la 4.1 sigue siendo la SituacionDeAgenda completa.
public class TablaMedicosTests : BunitContext
{
    public static TheoryData<SituacionDeAgenda, string, string, string?> Situaciones => new()
    {
        { SituacionDeAgenda.SinAgenda, "Sin agenda", "inactivo", null },
        { new(EstadoAgendaMedico.SinAgenda, new DateOnly(2026, 11, 16)), "Programada · desde 16/11/2026", "programada", null },
        { new(EstadoAgendaMedico.Vigente), "Vigente", "vigente", null },
        { new(EstadoAgendaMedico.Vigente, CambiaEl: new DateOnly(2026, 11, 16)), "Vigente", "vigente", "Cambia el 16/11/2026" },
        { new(EstadoAgendaMedico.PropuestaPendiente), "Pendiente de aprobación", "pendiente", null },
    };

    [Theory]
    [Trait("Story", "3.3")]
    [Trait("AC", "61")]
    [Trait("Story", "3.2")]
    [Trait("AC", "4")]
    [Trait("AC", "23")]
    [MemberData(nameof(Situaciones))]
    public void Medicos_BadgeDeAgendaPorSituacion(SituacionDeAgenda situacion, string texto, string clase, string? ayuda)
    {
        var celda = Render(TablaMedicos.Agenda(situacion));

        var badge = celda.Find(".badge-estado");
        Assert.Equal(texto, badge.TextContent.Trim());
        Assert.Contains($"badge-estado--{clase}", badge.ClassList);
        Assert.Equal(ayuda, celda.FindAll(".tabla-abm__ayuda").SingleOrDefault()?.TextContent.Trim());
        Assert.DoesNotContain("rige desde", celda.Markup);
    }
}
