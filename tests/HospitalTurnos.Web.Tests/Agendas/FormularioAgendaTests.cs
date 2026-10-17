using System.ComponentModel.DataAnnotations;
using HospitalTurnos.Dominio.Agendas;
using HospitalTurnos.Dominio.Excepciones;
using HospitalTurnos.Web.Components.Personal.Agendas;

namespace HospitalTurnos.Web.Tests.Agendas;

// El formulario de agenda (story 4.2) en C# puro: la revisión por fila, el cálculo de turnos (el del dominio, nunca
// otro), los botones del editor y la validación de campos. Sin base, sin pantalla: corren en milisegundos.
public class FormularioAgendaTests
{
    private const string Incompleta = "Completá la hora desde y la hora hasta, o quitá la franja.";

    private static FilaFranja Fila(DayOfWeek dia, string? desde, string? hasta) => new()
    {
        Dia = dia,
        Desde = desde is null ? null : TimeOnly.Parse(desde),
        Hasta = hasta is null ? null : TimeOnly.Parse(hasta),
    };

    private static FormularioAgenda Formulario(int? minutos, params FilaFranja[] filas) => new()
    {
        DuracionTurnoMinutos = minutos,
        VigenteDesde = new DateOnly(2026, 11, 2),
        Franjas = [.. filas],
    };

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "10")]
    [Trait("AC", "24")]
    public void Revisar_FranjasValidas_CalculaLosTurnosConElDominio()
    {
        var formulario = Formulario(20, Fila(DayOfWeek.Monday, "08:00", "12:00"), Fila(DayOfWeek.Wednesday, "08:00", "10:50"));
        var delDominio = new AgendaMedico(1, 20, new DateOnly(2026, 11, 2));
        delDominio.AgregarFranja(new Franja(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0)));
        delDominio.AgregarFranja(new Franja(DayOfWeek.Wednesday, new TimeOnly(8, 0), new TimeOnly(10, 50)));

        var revision = formulario.Revisar();

        Assert.Empty(revision.Problemas);
        Assert.Equal(20, revision.TurnosPorSemana);
        Assert.Equal(delDominio.TurnosPorSemana(), revision.TurnosPorSemana);
    }

    [Theory]
    [Trait("Story", "4.2")]
    [Trait("AC", "9")]
    [InlineData(null, "12:00", Incompleta)]
    [InlineData("08:00", null, Incompleta)]
    [InlineData("12:00", "08:00", "La franja tiene que terminar después de empezar.")]
    public void Revisar_FilaIncompletaOHorasInvalidas_MarcaLaFilaConSuMensaje(string? desde, string? hasta, string mensaje)
    {
        var revision = Formulario(20, Fila(DayOfWeek.Monday, desde, hasta)).Revisar();

        Assert.Equal(mensaje, Assert.Single(revision.Problemas, p => p.Key == 0).Value);
        Assert.Null(revision.TurnosPorSemana);
    }

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "9")]
    public void Revisar_FranjaSuperpuesta_MarcaLaQueChocaConElTextoDelDominio()
    {
        var revision = Formulario(20, Fila(DayOfWeek.Monday, "08:00", "12:00"), Fila(DayOfWeek.Monday, "10:00", "14:00")).Revisar();

        Assert.False(revision.Problemas.ContainsKey(0));
        Assert.Equal("La franja del lunes de 10:00 a 14:00 se superpone con la de 08:00 a 12:00.", revision.Problemas[1]);
        Assert.Null(revision.TurnosPorSemana);
    }

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "9")]
    [Trait("AC", "10")]
    public void Revisar_FranjaQueNoEsMultiplo_AvisaLosMinutosQueNoSeUsan()
    {
        var revision = Formulario(20, Fila(DayOfWeek.Monday, "08:00", "10:50")).Revisar();

        Assert.Empty(revision.Problemas);
        Assert.Equal("Los últimos 10 minutos de esta franja no se usan: no entra un turno completo.", revision.Avisos[0]);
        Assert.Equal(8, revision.TurnosPorSemana);
    }

    [Theory]
    [Trait("Story", "4.2")]
    [Trait("AC", "10")]
    [InlineData("duracion 5")]
    [InlineData("duracion vacia")]
    [InlineData("sin filas")]
    public void Revisar_DuracionInvalidaOSinFranjas_NoDaElCalculo(string caso)
    {
        var formulario = caso switch
        {
            "duracion 5" => Formulario(5, Fila(DayOfWeek.Monday, "08:00", "12:00")),
            "duracion vacia" => Formulario(null, Fila(DayOfWeek.Monday, "08:00", "12:00")),
            _ => Formulario(20),
        };

        var revision = formulario.Revisar();

        Assert.Null(revision.TurnosPorSemana);
        Assert.Equal(caso == "sin filas", revision.SinFranjas);
    }

    [Theory]
    [Trait("Story", "4.2")]
    [Trait("AC", "8")]
    [Trait("AC", "24")]
    [InlineData("agregar-franja", true, 3)]
    [InlineData("quitar-franja:1", true, 1)]
    [InlineData("actualizar", true, 2)]
    [InlineData("guardar", false, 2)]
    [InlineData("enviar", false, 2)]
    [InlineData("quitar-franja:9", null, 2)]     // null: acción del editor mal formada
    [InlineData("quitar-franja:x", null, 2)]
    public void AplicarAccionDeEdicion_SegunElBoton_CambiaLasFilasOLaDejaALaPagina(string accion, bool? deEdicion, int filas)
    {
        var formulario = Formulario(20, Fila(DayOfWeek.Monday, "08:00", "12:00"), Fila(DayOfWeek.Tuesday, "08:00", "12:00"));
        formulario.Accion = accion;

        if (deEdicion is null)
            Assert.Equal("No reconocemos esa acción. Recargá la página.",
                Assert.Throws<ReglaDeNegocioException>(() => formulario.AplicarAccionDeEdicion()).Message);
        else
            Assert.Equal(deEdicion, formulario.AplicarAccionDeEdicion());

        Assert.Equal(filas, formulario.Franjas.Count);
        if (accion == "agregar-franja")
            Assert.Equal((DayOfWeek.Monday, (TimeOnly?)null, (TimeOnly?)null),
                (formulario.Franjas[2].Dia, formulario.Franjas[2].Desde, formulario.Franjas[2].Hasta));
        if (accion == "quitar-franja:1")
            Assert.Equal(DayOfWeek.Monday, formulario.Franjas[0].Dia);   // queda la fila 0
    }

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "4")]
    [Trait("AC", "5")]
    [Trait("AC", "24")]
    public void DesdeYADatos_AgendaGuardada_IdaYVueltaConMedianoche()
    {
        var agenda = new AgendaMedico(7, 30, new DateOnly(2026, 11, 2));
        agenda.AgregarFranja(new Franja(DayOfWeek.Friday, new TimeOnly(20, 0), TimeOnly.MinValue));
        agenda.AgregarFranja(new Franja(DayOfWeek.Monday, new TimeOnly(14, 0), new TimeOnly(18, 0)));

        var formulario = FormularioAgenda.Desde(agenda);
        var datos = formulario.ADatos();
        var nueva = FormularioAgenda.Nueva(new DateOnly(2026, 10, 23));

        Assert.Equal([DayOfWeek.Monday, DayOfWeek.Friday], formulario.Franjas.Select(f => f.Dia));
        Assert.Equal(TimeOnly.MinValue, formulario.Franjas[1].Hasta);
        Assert.Equal((30, new DateOnly(2026, 11, 2)), (datos.DuracionTurnoMinutos, datos.VigenteDesde));
        Assert.Equal(["Monday 14:00 18:00", "Friday 20:00 00:00"],
            datos.Franjas.Select(f => $"{f.Dia} {f.Desde:HH\\:mm} {f.Hasta:HH\\:mm}"));
        Assert.Equal((20, new DateOnly(2026, 10, 23), 0), (nueva.DuracionTurnoMinutos, nueva.VigenteDesde, nueva.Franjas.Count));
    }

    [Theory]
    [Trait("Story", "4.2")]
    [Trait("AC", "11")]
    [InlineData("duracion vacia", "DuracionTurnoMinutos: Ingresá la duración del turno.")]
    [InlineData("duracion 61", "DuracionTurnoMinutos: La duración del turno tiene que estar entre 10 y 60 minutos.")]
    [InlineData("fecha vacia", "VigenteDesde: Elegí desde qué fecha rige.")]
    [InlineData("fila sin hasta", "Franjas: Completá la hora desde y la hora hasta de cada franja, o quitá las que sobran.")]
    // Cada campo da su mensaje aunque otro también esté mal (la fila no espera a que se corrija la duración).
    [InlineData("duracion vacia y fila sin hasta", "DuracionTurnoMinutos: Ingresá la duración del turno. | " +
        "Franjas: Completá la hora desde y la hora hasta de cada franja, o quitá las que sobran.")]
    public void Validar_CamposFaltantesOFueraDeRango_DevuelveLosMensajes(string caso, string esperado)
    {
        var formulario = Formulario(20, Fila(DayOfWeek.Monday, "08:00", "12:00"));
        if (caso.StartsWith("duracion vacia", StringComparison.Ordinal))
            formulario.DuracionTurnoMinutos = null;
        if (caso == "duracion 61")
            formulario.DuracionTurnoMinutos = 61;
        if (caso == "fecha vacia")
            formulario.VigenteDesde = null;
        if (caso.EndsWith("fila sin hasta", StringComparison.Ordinal))
            formulario.Franjas.Add(Fila(DayOfWeek.Tuesday, "08:00", null));
        var resultados = new List<ValidationResult>();

        Assert.False(Validator.TryValidateObject(formulario, new ValidationContext(formulario), resultados, validateAllProperties: true));

        Assert.Equal(esperado, string.Join(" | ",
            resultados.Select(r => $"{Assert.Single(r.MemberNames)}: {r.ErrorMessage}").Order(StringComparer.Ordinal)));
    }
}
