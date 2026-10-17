using HospitalTurnos.Dominio.Agendas;
using HospitalTurnos.Dominio.Excepciones;
using HospitalTurnos.Dominio.Tests.Ayudas;

namespace HospitalTurnos.Dominio.Tests.Agendas;

public class EstadosDeAgendaTests
{
    private static readonly TimeProvider Reloj = AgendaBuilder.RelojPorDefecto();   // viernes 16/10/2026 12:00

    private static Franja F(DayOfWeek dia, string desde, string hasta) =>
        new(dia, TimeOnly.Parse(desde), TimeOnly.Parse(hasta));

    /// <summary>Todo lo que una transición prohibida no puede tocar.</summary>
    private static string Foto(AgendaMedico a) =>
        $"{a.Estado}|{a.DuracionTurnoMinutos}|{a.VigenteDesde:dd/MM/yyyy}|{a.MotivoRechazo}|{a.EnviadaEl:O}|" +
        $"{a.AprobadaEl:O}|{a.RechazadaEl:O}|" +
        string.Join(",", a.Franjas.Select(f => $"{f.Dia} {f.Desde:HH\\:mm}-{f.Hasta:HH\\:mm}"));

    [Theory]
    [Trait("Story", "4.1")]
    [Trait("AC", "9")]
    [InlineData(2026, 10, 23)]
    [InlineData(2026, 11, 30)]
    public void Enviar_ConSieteDiasOMas_PasaAEnviadaConMarcaUtc(int anio, int mes, int dia)
    {
        var agenda = AgendaBuilder.UnaAgenda().Desde(new DateOnly(anio, mes, dia))
            .ConFranja(DayOfWeek.Monday, "08:00", "12:00").Borrador();

        agenda.Enviar(Reloj);

        Assert.Equal(EstadoPropuesta.Enviada, agenda.Estado);
        Assert.Equal(Reloj.GetUtcNow(), agenda.EnviadaEl);
        Assert.Equal(TimeSpan.Zero, agenda.EnviadaEl!.Value.Offset);
    }

    [Theory]
    [Trait("Story", "4.1")]
    [Trait("AC", "9")]
    [InlineData(2026, 10, 22)]
    [InlineData(2026, 10, 16)]
    public void Enviar_ConMenosDeSieteDias_LanzaConLaFechaMinima(int anio, int mes, int dia)
    {
        var agenda = AgendaBuilder.UnaAgenda().Desde(new DateOnly(anio, mes, dia))
            .ConFranja(DayOfWeek.Monday, "08:00", "12:00").Borrador();

        var error = Assert.Throws<ReglaDeNegocioException>(() => agenda.Enviar(Reloj));

        Assert.Equal("La agenda tiene que regir desde el 23/10/2026 o después: 7 días desde hoy.", error.Message);
        Assert.Equal(EstadoPropuesta.Borrador, agenda.Estado);
        Assert.Null(agenda.EnviadaEl);
    }

    [Theory]
    [Trait("Story", "4.1")]
    [Trait("AC", "9")]
    [InlineData(23, true)]
    [InlineData(22, false)]
    public void Enviar_CercaDeMedianoche_UsaLaFechaLocalDelHospital(int dia, bool seAcepta)
    {
        // 02:30 UTC del sábado 17 = 23:30 del viernes 16 en Córdoba: el mínimo es el 23/10, no el 24/10.
        var reloj = RelojDePrueba.EnUtc(new DateTimeOffset(2026, 10, 17, 2, 30, 0, TimeSpan.Zero));
        var agenda = AgendaBuilder.UnaAgenda().Desde(new DateOnly(2026, 10, dia))
            .ConFranja(DayOfWeek.Monday, "08:00", "12:00").Borrador();

        if (seAcepta)
        {
            agenda.Enviar(reloj);
            Assert.Equal(EstadoPropuesta.Enviada, agenda.Estado);
        }
        else
        {
            var error = Assert.Throws<ReglaDeNegocioException>(() => agenda.Enviar(reloj));
            Assert.Equal("La agenda tiene que regir desde el 23/10/2026 o después: 7 días desde hoy.", error.Message);
        }
    }

    [Fact]
    [Trait("Story", "4.1")]
    [Trait("AC", "9")]
    public void Enviar_SinFranjas_Lanza()
    {
        var agenda = AgendaBuilder.UnaAgenda().Borrador();

        var error = Assert.Throws<ReglaDeNegocioException>(() => agenda.Enviar(Reloj));

        Assert.Equal("Agregá al menos una franja antes de enviar la propuesta.", error.Message);
        Assert.Equal(EstadoPropuesta.Borrador, agenda.Estado);
    }

    [Fact]
    [Trait("Story", "4.1")]
    [Trait("AC", "10")]
    public void Rechazar_ConMotivo_PasaARechazadaConElMotivoRecortado()
    {
        var agenda = AgendaBuilder.UnaAgenda().Enviada(Reloj);

        agenda.Rechazar("  El consultorio 3 está ocupado.  ", Reloj);

        Assert.Equal(EstadoPropuesta.Rechazada, agenda.Estado);
        Assert.Equal("El consultorio 3 está ocupado.", agenda.MotivoRechazo);
        Assert.Equal(Reloj.GetUtcNow(), agenda.RechazadaEl);
    }

    public static TheoryData<string, string?> MotivosRechazo => new()
    {
        { "", "Escribí el motivo del rechazo." },
        { "   ", "Escribí el motivo del rechazo." },
        { new string('a', 501), "El motivo tiene como máximo 500 caracteres." },
        { new string('a', 500), null },   // el borde se acepta
    };

    [Theory]
    [Trait("Story", "4.1")]
    [Trait("AC", "10")]
    [MemberData(nameof(MotivosRechazo))]
    public void Rechazar_MotivoVacioOLargo_LanzaYSigueEnviada(string motivo, string? mensaje)
    {
        var agenda = AgendaBuilder.UnaAgenda().Enviada(Reloj);

        if (mensaje is null)
        {
            agenda.Rechazar(motivo, Reloj);
            Assert.Equal(EstadoPropuesta.Rechazada, agenda.Estado);
            return;
        }
        var error = Assert.Throws<ReglaDeNegocioException>(() => agenda.Rechazar(motivo, Reloj));
        Assert.Equal(mensaje, error.Message);
        Assert.Equal(EstadoPropuesta.Enviada, agenda.Estado);
        Assert.Null(agenda.RechazadaEl);
    }

    [Theory]
    [Trait("Story", "4.1")]
    [Trait("AC", "11")]
    [InlineData(10, 20)]
    [InlineData(11, 2)]   // el mismo día de VigenteDesde
    public void Aprobar_Enviada_PasaAAprobadaConMarca(int mes, int dia)
    {
        var agenda = AgendaBuilder.UnaAgenda().Desde(new DateOnly(2026, 11, 2)).Enviada(Reloj);
        var relojAprobacion = RelojDePrueba.En(2026, mes, dia);

        agenda.Aprobar(relojAprobacion);

        Assert.Equal(EstadoPropuesta.Aprobada, agenda.Estado);
        Assert.Equal(relojAprobacion.GetUtcNow(), agenda.AprobadaEl);
    }

    [Fact]
    [Trait("Story", "4.1")]
    [Trait("AC", "11")]
    public void Aprobar_VigenteDesdeYaPasada_LanzaConflictoYSigueEnviada()
    {
        var agenda = AgendaBuilder.UnaAgenda().Desde(new DateOnly(2026, 11, 2)).Enviada(Reloj);

        var error = Assert.Throws<ConflictoException>(() => agenda.Aprobar(RelojDePrueba.En(2026, 11, 3)));

        Assert.Equal("La agenda iba a regir desde el 02/11/2026, que ya pasó: rechazala para que el médico elija otra fecha.",
            error.Message);
        Assert.Equal(EstadoPropuesta.Enviada, agenda.Estado);
        Assert.Null(agenda.AprobadaEl);
    }

    [Theory]
    [Trait("Story", "4.1")]
    [Trait("AC", "12")]
    [InlineData("duracion")]
    [InlineData("vigencia")]
    [InlineData("agregar")]
    [InlineData("quitar")]
    public void CorregirRechazada_CualquierEdicionValida_VuelveABorradorConElMotivo(string edicion)
    {
        var agenda = AgendaBuilder.UnaAgenda().Rechazada(Reloj, "X");
        var rechazadaEl = agenda.RechazadaEl;

        switch (edicion)
        {
            case "duracion": agenda.CambiarDuracion(30); Assert.Equal(30, agenda.DuracionTurnoMinutos); break;
            case "vigencia": agenda.CambiarVigenteDesde(new DateOnly(2026, 11, 9)); Assert.Equal(new DateOnly(2026, 11, 9), agenda.VigenteDesde); break;
            case "agregar": agenda.AgregarFranja(F(DayOfWeek.Wednesday, "08:00", "12:00")); Assert.Equal(2, agenda.Franjas.Count); break;
            case "quitar": agenda.QuitarFranja(DayOfWeek.Monday, new TimeOnly(8, 0)); Assert.Empty(agenda.Franjas); break;
        }

        Assert.Equal(EstadoPropuesta.Borrador, agenda.Estado);
        Assert.Equal("X", agenda.MotivoRechazo);
        Assert.Equal(rechazadaEl, agenda.RechazadaEl);
    }

    [Theory]
    [Trait("Story", "4.1")]
    [Trait("AC", "12")]
    [InlineData("duracion")]
    [InlineData("superpuesta")]
    public void CorregirRechazada_EdicionInvalida_SigueRechazadaSinCambios(string edicion)
    {
        var agenda = AgendaBuilder.UnaAgenda().Rechazada(Reloj, "X");
        var antes = Foto(agenda);

        Assert.Throws<ReglaDeNegocioException>(() =>
        {
            if (edicion == "duracion") agenda.CambiarDuracion(5);
            else agenda.AgregarFranja(F(DayOfWeek.Monday, "10:00", "14:00"));
        });

        Assert.Equal(EstadoPropuesta.Rechazada, agenda.Estado);
        Assert.Equal(antes, Foto(agenda));
    }

    [Fact]
    [Trait("Story", "4.1")]
    [Trait("AC", "8")]
    [Trait("AC", "9")]
    [Trait("AC", "10")]
    [Trait("AC", "11")]
    [Trait("AC", "12")]
    public void RecorridoCompleto_RechazoYReenvio_TerminaAprobada()
    {
        var estados = new List<EstadoPropuesta>();
        var agenda = AgendaBuilder.UnaAgenda().ConFranja(DayOfWeek.Monday, "08:00", "12:00").Borrador();
        estados.Add(agenda.Estado);
        agenda.Enviar(Reloj); estados.Add(agenda.Estado);
        agenda.Rechazar("motivo", Reloj); estados.Add(agenda.Estado);
        agenda.CambiarDuracion(30); estados.Add(agenda.Estado);
        agenda.Enviar(Reloj); estados.Add(agenda.Estado);
        Assert.Null(agenda.MotivoRechazo);
        agenda.Aprobar(Reloj); estados.Add(agenda.Estado);

        Assert.Equal([EstadoPropuesta.Borrador, EstadoPropuesta.Enviada, EstadoPropuesta.Rechazada,
            EstadoPropuesta.Borrador, EstadoPropuesta.Enviada, EstadoPropuesta.Aprobada], estados);
    }

    private const string YaEnviada = "La propuesta ya fue enviada.";
    private const string CorregirAntes = "Corregí la propuesta antes de reenviarla.";
    private const string AprobadaNoSeModifica = "Una agenda aprobada no se modifica: armá una propuesta nueva.";
    private const string SoloAprobarEnviada = "Sólo se puede aprobar una propuesta enviada.";
    private const string SoloRechazarEnviada = "Sólo se puede rechazar una propuesta enviada.";
    private const string EsperandoAprobacion = "La propuesta está esperando aprobación: no se puede modificar.";

    public static TheoryData<string, string, string> TransicionesNoPrevistas => new()
    {
        { "Enviada", "Enviar", YaEnviada },
        { "Rechazada", "Enviar", CorregirAntes },
        { "Aprobada", "Enviar", AprobadaNoSeModifica },
        { "Borrador", "Aprobar", SoloAprobarEnviada },
        { "Rechazada", "Aprobar", SoloAprobarEnviada },
        { "Aprobada", "Aprobar", SoloAprobarEnviada },
        { "Borrador", "Rechazar", SoloRechazarEnviada },
        { "Rechazada", "Rechazar", SoloRechazarEnviada },
        { "Aprobada", "Rechazar", SoloRechazarEnviada },
        { "Enviada", "CambiarDuracion", EsperandoAprobacion },
        { "Enviada", "CambiarVigenteDesde", EsperandoAprobacion },
        { "Enviada", "AgregarFranja", EsperandoAprobacion },
        { "Enviada", "QuitarFranja", EsperandoAprobacion },
        { "Aprobada", "CambiarDuracion", AprobadaNoSeModifica },
        { "Aprobada", "AgregarFranja", AprobadaNoSeModifica },
        // Estado primero, datos después: esta rechazada tampoco cumple los 7 días, pero sale el 409 del estado.
        { "RechazadaVencida", "Enviar", CorregirAntes },
    };

    [Theory]
    [Trait("Story", "4.1")]
    [Trait("AC", "13")]
    [MemberData(nameof(TransicionesNoPrevistas))]
    public void TransicionNoPrevista_EnCadaEstado_LanzaConflictoSinCambiar(string estado, string intento, string mensaje)
    {
        var agenda = estado switch
        {
            "Borrador" => AgendaBuilder.UnaAgenda().ConFranja(DayOfWeek.Monday, "08:00", "12:00").Borrador(),
            "Enviada" => AgendaBuilder.UnaAgenda().Enviada(Reloj),
            "Rechazada" => AgendaBuilder.UnaAgenda().Rechazada(Reloj),
            "Aprobada" => AgendaBuilder.UnaAgenda().Aprobada(Reloj),
            "RechazadaVencida" => AgendaBuilder.UnaAgenda().Desde(new DateOnly(2026, 10, 20))
                .Rechazada(RelojDePrueba.En(2026, 10, 10)),
            _ => throw new ArgumentOutOfRangeException(nameof(estado)),
        };
        var antes = Foto(agenda);

        var error = Assert.Throws<ConflictoException>(() =>
        {
            switch (intento)
            {
                case "Enviar": agenda.Enviar(Reloj); break;
                case "Aprobar": agenda.Aprobar(Reloj); break;
                case "Rechazar": agenda.Rechazar("motivo", Reloj); break;
                case "CambiarDuracion": agenda.CambiarDuracion(30); break;
                case "CambiarVigenteDesde": agenda.CambiarVigenteDesde(new DateOnly(2026, 11, 9)); break;
                case "AgregarFranja": agenda.AgregarFranja(F(DayOfWeek.Friday, "08:00", "12:00")); break;
                case "QuitarFranja": agenda.QuitarFranja(DayOfWeek.Monday, new TimeOnly(8, 0)); break;
            }
        });

        Assert.Equal(mensaje, error.Message);
        Assert.Equal(antes, Foto(agenda));
    }
}
