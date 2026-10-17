using HospitalTurnos.Dominio.Agendas;
using HospitalTurnos.Dominio.Excepciones;
using HospitalTurnos.Dominio.Tests.Ayudas;

namespace HospitalTurnos.Dominio.Tests.Agendas;

public class EdicionDeAgendaTests
{
    private static Franja F(DayOfWeek dia, string desde, string hasta) =>
        new(dia, TimeOnly.Parse(desde), TimeOnly.Parse(hasta));

    private static string Resumen(AgendaMedico agenda) =>
        string.Join(" | ", agenda.Franjas.Select(f => $"{f.Dia} {f.Desde:HH\\:mm}-{f.Hasta:HH\\:mm}"));

    [Theory]
    [Trait("Story", "4.1")]
    [Trait("AC", "4")]
    [InlineData("08:00", "12:00", "10:00", "14:00", "La franja del lunes de 10:00 a 14:00 se superpone con la de 08:00 a 12:00.")]
    [InlineData("08:00", "12:00", "08:00", "12:00", "La franja del lunes de 08:00 a 12:00 se superpone con la de 08:00 a 12:00.")]
    [InlineData("20:00", "00:00", "23:00", "23:30", "La franja del lunes de 23:00 a 23:30 se superpone con la de 20:00 a 24:00.")]
    public void AgregarFranja_SuperpuestaEnElMismoDia_LanzaYNoLaAgrega(string desdeCargada, string hastaCargada,
        string desdeNueva, string hastaNueva, string mensaje)
    {
        var agenda = AgendaBuilder.UnaAgenda().ConFranja(DayOfWeek.Monday, desdeCargada, hastaCargada).Borrador();
        var antes = Resumen(agenda);

        var error = Assert.Throws<ReglaDeNegocioException>(
            () => agenda.AgregarFranja(F(DayOfWeek.Monday, desdeNueva, hastaNueva)));

        Assert.Equal(mensaje, error.Message);
        Assert.Equal(antes, Resumen(agenda));
    }

    [Fact]
    [Trait("Story", "4.1")]
    [Trait("AC", "4")]
    public void AgregarFranja_MananaYTardeElMismoDia_AgregaLasDos()
    {
        var agenda = AgendaBuilder.UnaAgenda().Borrador();

        agenda.AgregarFranja(F(DayOfWeek.Monday, "08:00", "12:00"));
        agenda.AgregarFranja(F(DayOfWeek.Monday, "14:00", "18:00"));
        agenda.AgregarFranja(F(DayOfWeek.Tuesday, "08:00", "12:00"));

        Assert.Equal(3, agenda.Franjas.Count);
    }

    [Fact]
    [Trait("Story", "4.1")]
    [Trait("AC", "5")]
    public void QuitarFranja_SegunDiaYHora_SacaSoloEsaOLanzaSiNoEsta()
    {
        var agenda = AgendaBuilder.UnaAgenda()
            .ConFranja(DayOfWeek.Monday, "08:00", "12:00").ConFranja(DayOfWeek.Monday, "14:00", "18:00").Borrador();

        agenda.QuitarFranja(DayOfWeek.Monday, new TimeOnly(14, 0));

        Assert.Equal("Monday 08:00-12:00", Resumen(agenda));
        var error = Assert.Throws<NoEncontradoException>(() => agenda.QuitarFranja(DayOfWeek.Tuesday, new TimeOnly(8, 0)));
        Assert.Equal("Esa franja no está en la agenda.", error.Message);
        Assert.Equal("Monday 08:00-12:00", Resumen(agenda));
    }

    [Theory]
    [Trait("Story", "4.1")]
    [Trait("AC", "6")]
    [InlineData(10)]
    [InlineData(60)]
    public void Duracion_EnLosBordes_SeAcepta(int minutos)
    {
        var creada = new AgendaMedico(1, minutos, new DateOnly(2026, 11, 2));
        var cambiada = AgendaBuilder.UnaAgenda().Borrador();
        cambiada.CambiarDuracion(minutos);

        Assert.Equal(minutos, creada.DuracionTurnoMinutos);
        Assert.Equal(minutos, cambiada.DuracionTurnoMinutos);
    }

    [Theory]
    [Trait("Story", "4.1")]
    [Trait("AC", "6")]
    [InlineData(9)]
    [InlineData(61)]
    [InlineData(0)]
    public void Duracion_FueraDeRango_LanzaYNoCambia(int minutos)
    {
        const string mensaje = "La duración del turno tiene que estar entre 10 y 60 minutos.";
        var borrador = AgendaBuilder.UnaAgenda().DeMinutos(20).Borrador();

        var alCrear = Assert.Throws<ReglaDeNegocioException>(() => new AgendaMedico(1, minutos, new DateOnly(2026, 11, 2)));
        var alCambiar = Assert.Throws<ReglaDeNegocioException>(() => borrador.CambiarDuracion(minutos));

        Assert.Equal(mensaje, alCrear.Message);
        Assert.Equal(mensaje, alCambiar.Message);
        Assert.Equal(20, borrador.DuracionTurnoMinutos);
    }

    [Fact]
    [Trait("Story", "4.1")]
    [Trait("AC", "7")]
    [Trait("AC", "8")]
    public void NuevaAgenda_RecienCreada_QuedaEnBorradorSinFranjasNiMarcas()
    {
        var agenda = new AgendaMedico(42, 20, new DateOnly(2026, 11, 2));

        Assert.Equal(EstadoPropuesta.Borrador, agenda.Estado);
        Assert.Empty(agenda.Franjas);
        Assert.Null(agenda.MotivoRechazo);
        Assert.Null(agenda.EnviadaEl);
        Assert.Null(agenda.AprobadaEl);
        Assert.Null(agenda.RechazadaEl);
        Assert.Equal(42, agenda.MedicoId);

        agenda.CambiarVigenteDesde(new DateOnly(2026, 11, 9));   // no mira los 7 días: eso es al enviar

        Assert.Equal(new DateOnly(2026, 11, 9), agenda.VigenteDesde);
    }
}
