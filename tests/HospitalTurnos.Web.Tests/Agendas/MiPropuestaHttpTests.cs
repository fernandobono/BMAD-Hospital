using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using HospitalTurnos.Dominio.Agendas;
using HospitalTurnos.Infraestructura.Identidad;
using HospitalTurnos.Web.Tests.Personal;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using static HospitalTurnos.Web.Tests.Agendas.DatosDePropuesta;
using static HospitalTurnos.Web.Tests.Personal.FormularioHttp;

namespace HospitalTurnos.Web.Tests.Agendas;

// M2 con HttpClient sobre la app real (token y cookies incluidos), como lo usa un navegador: es SSR sin JavaScript
// propio. "Hoy" es la fecha local de app.Reloj, que nunca se adelanta; las agendas que tienen que regir hoy se envían
// y aprueban con un reloj corrido 30 días hacia atrás. medico1, admin1 y recepcion1 sólo ingresan y navegan.
[Collection("Postgres")]
public partial class MiPropuestaHttpTests(PostgresFixture fixture)
{
    private const string M2 = "/personal/medico/mi-propuesta";
    private const string Formulario = "propuesta";

    private static async Task<string> HtmlAsync(HttpResponseMessage respuesta) =>
        WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());

    private static string Fecha(DateOnly fecha) => fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    // Los campos que manda el navegador: duración, fecha (yyyy-MM-dd), cada fila y el botón pulsado.
    private static Dictionary<string, string> Campos(string accion, string duracion, DateOnly? desde,
        params (string Dia, string Desde, string Hasta)[] filas)
    {
        var campos = new Dictionary<string, string>
        {
            ["Input.DuracionTurnoMinutos"] = duracion,
            ["Input.VigenteDesde"] = desde?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "",
            ["Input.Accion"] = accion,
        };
        for (var i = 0; i < filas.Length; i++)
        {
            campos[$"Input.Franjas[{i}].Dia"] = filas[i].Dia;
            campos[$"Input.Franjas[{i}].Desde"] = filas[i].Desde;
            campos[$"Input.Franjas[{i}].Hasta"] = filas[i].Hasta;
        }
        return campos;
    }

    private static int FilasDe(string html) => FilaDia().Matches(html).Count;

    private static AgendaMedico Agenda(long medicoId, DateOnly desde, int minutos = 20, params Franja[] franjas)
    {
        var agenda = new AgendaMedico(medicoId, minutos, desde);
        foreach (var franja in franjas.Length > 0 ? franjas : [new Franja(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0))])
            agenda.AgregarFranja(franja);
        return agenda;
    }

    // Una aprobada que rige hoy: enviada y aprobada 30 días antes, desde hoy − 20.
    private static AgendaMedico VigenteHoy(PersonalWebApplicationFactory app, long medicoId)
    {
        var haceUnMes = RelojCorridoDias(app.Reloj, -30);
        var agenda = Agenda(medicoId, app.Hoy().AddDays(-20));
        agenda.Enviar(haceUnMes);
        agenda.Aprobar(haceUnMes);
        return agenda;
    }

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "1")]
    [Trait("AC", "2")]
    public async Task GetMiPropuesta_SinVinculo_MuestraElAvisoSinFormulario()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var (cliente, _, _) = await app.MedicoIngresadoAsync(vinculado: false);

        var respuesta = await cliente.GetAsync(M2);
        var html = await HtmlAsync(respuesta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("<h1>Mi propuesta de agenda</h1>", html);
        Assert.Contains("Tu usuario no está vinculado a un médico. Pedíselo al administrador.", html);
        Assert.DoesNotContain($"value=\"{Formulario}\"", html);
        Assert.DoesNotContain("Esta sección se habilita", html);

        // AC 21: si igual llega un envío (armado a mano o de una pestaña vieja), el cartel con 403, no un 400 en blanco.
        var envio = await EnviarAsync(cliente, M2, Formulario, Campos("guardar", "20", app.Hoy().AddDays(30), ("Monday", "08:00", "12:00")));
        Assert.Equal(HttpStatusCode.Forbidden, envio.StatusCode);
        Assert.Equal("Tu usuario no está vinculado a un médico. Pedíselo al administrador.", Alerta(await HtmlAsync(envio)));
    }

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "1")]
    [Trait("AC", "3")]
    [Trait("AC", "4")]
    [Trait("AC", "7")]
    public async Task GetMiPropuesta_SinAgendas_OfreceUnaPropuestaNuevaVacia()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var (cliente, medico, _) = await app.MedicoIngresadoAsync();
        var minimo = app.Hoy().AddDays(7);

        var respuesta = await cliente.GetAsync(M2);
        var html = await HtmlAsync(respuesta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("<h1>Mi propuesta de agenda</h1>", html);
        Assert.Contains("Todavía no tenés una agenda vigente.", html);
        Assert.Contains("Nueva propuesta", html);
        Assert.Equal("20", ValorDe(html, "Input.DuracionTurnoMinutos"));
        Assert.Equal(minimo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), ValorDe(html, "Input.VigenteDesde"));
        Assert.Contains($"(mín. {Fecha(minimo)})", html);
        Assert.Equal(0, FilasDe(html));
        Assert.Contains("Guardar borrador", html);
        Assert.Contains("Enviar para aprobación", html);
        Assert.Empty(await app.AgendasDeAsync(medico!.Value));
    }

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "3")]
    [Trait("AC", "4")]
    [Trait("AC", "10")]
    public async Task GetMiPropuesta_ConVigente_LaMuestraArribaYCopiaSusFranjas()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var (cliente, medico, _) = await app.MedicoIngresadoAsync();
        await app.GuardarAgendaAsync(VigenteHoy(app, medico!.Value));

        var html = await HtmlAsync(await cliente.GetAsync(M2));

        Assert.Contains("Mi agenda vigente:", html);
        Assert.Contains("turnos de 20 min · Lun 8–12", html);
        Assert.Contains("12 turnos/semana", html);
        Assert.StartsWith("08:00", ValorDe(html, "Input.Franjas[0].Desde"));   // el name real que dibuja Blazor
        Assert.Equal("20", ValorDe(html, "Input.DuracionTurnoMinutos"));
        Assert.Contains("ofrecés 12 turnos por semana (antes: 12).", html);
    }

    [Theory]
    [Trait("Story", "4.2")]
    [Trait("AC", "12")]
    [Trait("AC", "13")]
    [InlineData(true)]
    [InlineData(false)]   // un borrador puede no tener franjas (AC 12): el navegador no manda ninguna Input.Franjas[i]
    public async Task PostMiPropuesta_GuardarBorrador_RedirigeYAnuncia(bool conFranja)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var (cliente, medico, _) = await app.MedicoIngresadoAsync();
        var campos = conFranja
            ? Campos("guardar", "20", app.Hoy().AddDays(30), ("Monday", "08:00", "12:00"))
            : Campos("guardar", "20", app.Hoy().AddDays(30));

        var respuesta = await EnviarAsync(cliente, M2, Formulario, campos);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal($"{M2}?resultado=borrador", Destino(respuesta));
        var html = await HtmlAsync(await cliente.GetAsync(Destino(respuesta)));
        Assert.Equal("Borrador guardado.", Estado(html));
        Assert.Contains("Estado: Borrador", html);
        var agenda = Assert.Single(await app.AgendasDeAsync(medico!.Value));
        Assert.Equal(EstadoPropuesta.Borrador, agenda.Estado);
        Assert.Equal(conFranja ? [DayOfWeek.Monday] : (DayOfWeek[])[], agenda.Franjas.Select(f => f.Dia));

        // AC 13: guardar lo mismo otra vez no guarda y lo dice; un ?resultado= desconocido no muestra nada.
        var otraVez = await EnviarAsync(cliente, M2, Formulario, campos);
        Assert.Equal($"{M2}?resultado=sin-cambios", Destino(otraVez));
        Assert.Equal("No había cambios para guardar.", Estado(await HtmlAsync(await cliente.GetAsync(Destino(otraVez)))));
        Assert.Null(Estado(await HtmlAsync(await cliente.GetAsync($"{M2}?resultado=<b>hola</b>"))));
    }

    [Theory]
    [Trait("Story", "4.2")]
    [Trait("AC", "8")]
    [Trait("AC", "9")]
    [InlineData("agregar-franja")]
    [InlineData("agregar-franja sin filas")]   // la tabla vacía: el navegador no manda ninguna Input.Franjas[i]
    [InlineData("quitar-franja:0")]
    [InlineData("actualizar")]
    public async Task PostMiPropuesta_BotonesDeEdicion_CambianLasFilasSinGuardar(string caso)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var (cliente, medico, _) = await app.MedicoIngresadoAsync();
        var desde = app.Hoy().AddDays(30);
        var accion = caso.Split(' ')[0];
        var campos = caso switch
        {
            "agregar-franja sin filas" => Campos(accion, "20", desde),
            "agregar-franja" => Campos(accion, "20", desde, ("Monday", "08:00", "12:00")),
            "quitar-franja:0" => Campos(accion, "20", desde, ("Monday", "08:00", "12:00"), ("Tuesday", "14:00", "18:00")),
            _ => Campos(accion, "20", desde, ("Monday", "08:00", "12:00"), ("Monday", "10:00", "14:00")),
        };

        var respuesta = await EnviarAsync(cliente, M2, Formulario, campos);
        var html = await HtmlAsync(respuesta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        switch (caso)
        {
            case "agregar-franja":
                Assert.Equal(2, FilasDe(html));
                break;
            case "agregar-franja sin filas":
                Assert.Equal(1, FilasDe(html));
                break;
            case "quitar-franja:0":
                Assert.Equal(1, FilasDe(html));
                Assert.StartsWith("14:00", ValorDe(html, "Input.Franjas[0].Desde"));   // quedó la segunda
                break;
            default:
                Assert.Equal(2, FilasDe(html));
                Assert.Contains("La franja del lunes de 10:00 a 14:00 se superpone con la de 08:00 a 12:00.", html);
                break;
        }
        Assert.Equal("20", ValorDe(html, "Input.DuracionTurnoMinutos"));
        Assert.Empty(await app.AgendasDeAsync(medico!.Value));
    }

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "6")]
    [Trait("AC", "15")]
    public async Task PostMiPropuesta_Enviar_RedirigeYQuedaEnSoloLectura()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var (cliente, medico, _) = await app.MedicoIngresadoAsync();
        await app.GuardarAgendaAsync(Agenda(medico!.Value, app.Hoy().AddDays(30)));
        var minimo = app.Hoy().AddDays(7);

        var respuesta = await EnviarAsync(cliente, M2, Formulario, Campos("enviar", "20", minimo, ("Monday", "08:00", "12:00")));

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal($"{M2}?resultado=enviada", Destino(respuesta));
        var html = await HtmlAsync(await cliente.GetAsync(Destino(respuesta)));
        Assert.Equal("Propuesta enviada para aprobación.", Estado(html));
        Assert.Contains("Estado: Enviada", html);
        Assert.Contains($"Esperando aprobación desde el {Fecha(app.Hoy())}.", html);
        Assert.Contains($"Rige desde el {Fecha(minimo)}.", html);
        Assert.Contains("turnos de 20 min · Lun 8–12", html);
        Assert.Null(ValorDe(html, "Input.DuracionTurnoMinutos"));
        Assert.DoesNotContain("name=\"Input.", html);
        Assert.DoesNotContain("Enviar para aprobación</button>", html);
        Assert.Equal(EstadoPropuesta.Enviada, Assert.Single(await app.AgendasDeAsync(medico.Value)).Estado);
    }

    [Theory]
    [Trait("Story", "4.2")]
    [Trait("AC", "14")]
    [Trait("AC", "16")]
    [Trait("AC", "18")]
    [InlineData("fecha antes del minimo")]
    [InlineData("franjas superpuestas")]
    [InlineData("rechazada sin cambios")]
    [InlineData("pestaña vieja")]
    public async Task PostMiPropuesta_ReglaQueFalla_RespondeConElCodigoYElCartel(string caso)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var (cliente, medicoId, _) = await app.MedicoIngresadoAsync();
        var medico = medicoId!.Value;
        var hoy = app.Hoy();
        var desde = hoy.AddDays(30);
        if (caso == "rechazada sin cambios")
        {
            var rechazada = Agenda(medico, desde);
            rechazada.Enviar(app.Reloj);
            rechazada.Rechazar("El consultorio 3 está ocupado.", app.Reloj);
            await app.GuardarAgendaAsync(rechazada);
        }
        var (campos, codigo, mensaje) = caso switch
        {
            "fecha antes del minimo" => (Campos("enviar", "30", hoy.AddDays(6), ("Monday", "08:00", "12:00")), HttpStatusCode.BadRequest,
                $"La agenda tiene que regir desde el {Fecha(hoy.AddDays(7))} o después: 7 días desde hoy."),
            "franjas superpuestas" => (Campos("guardar", "30", desde, ("Monday", "08:00", "12:00"), ("Monday", "10:00", "14:00")),
                HttpStatusCode.BadRequest, "La franja del lunes de 10:00 a 14:00 se superpone con la de 08:00 a 12:00."),
            "rechazada sin cambios" => (Campos("enviar", "20", desde, ("Monday", "08:00", "12:00")), HttpStatusCode.Conflict,
                "Corregí la propuesta antes de reenviarla."),
            _ => (Campos("guardar", "30", desde, ("Monday", "08:00", "12:00")), HttpStatusCode.Conflict,
                "Ya tenés una propuesta enviada esperando aprobación: hasta que el administrador la revise no podés modificarla ni armar otra."),
        };

        HttpResponseMessage respuesta;
        if (caso == "pestaña vieja")
        {
            // La pantalla abierta (con su token) y, recién después, una enviada guardada para el médico: el POST es de una pestaña vieja.
            var token = await TokenDeAsync(cliente, M2);
            var enviada = Agenda(medico, desde);
            enviada.Enviar(app.Reloj);
            await app.GuardarAgendaAsync(enviada);
            campos["_handler"] = Formulario;
            campos["__RequestVerificationToken"] = token;
            respuesta = await cliente.PostAsync(M2, new FormUrlEncodedContent(campos));
        }
        else
            respuesta = await EnviarAsync(cliente, M2, Formulario, campos);
        var html = await HtmlAsync(respuesta);

        Assert.Equal(codigo, respuesta.StatusCode);
        Assert.Equal(mensaje, Alerta(html));
        Assert.Equal(campos["Input.DuracionTurnoMinutos"], ValorDe(html, "Input.DuracionTurnoMinutos"));
        var agendas = await app.AgendasDeAsync(medico);
        Assert.Equal(caso switch
        {
            "rechazada sin cambios" => [EstadoPropuesta.Rechazada],
            "pestaña vieja" => [EstadoPropuesta.Enviada],
            _ => (EstadoPropuesta[])[],
        }, agendas.Select(a => a.Estado));
        Assert.All(agendas, a => Assert.Equal(20, a.DuracionTurnoMinutos));
    }

    [Theory]
    [Trait("Story", "4.2")]
    [Trait("AC", "11")]
    [InlineData("duracion vacia", "Ingresá la duración del turno.")]
    [InlineData("duracion 61", "La duración del turno tiene que estar entre 10 y 60 minutos.")]
    [InlineData("rige desde vacia", "Elegí desde qué fecha rige.")]
    [InlineData("fila sin hasta", "Completá la hora desde y la hora hasta de cada franja, o quitá las que sobran.")]
    public async Task PostMiPropuesta_CamposInvalidos_MuestraMensajesSinGuardar(string caso, string mensaje)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var (cliente, medico, _) = await app.MedicoIngresadoAsync();
        var desde = app.Hoy().AddDays(30);
        var campos = caso switch
        {
            "duracion vacia" => Campos("guardar", "", desde, ("Monday", "08:00", "12:00")),
            "duracion 61" => Campos("guardar", "61", desde, ("Monday", "08:00", "12:00")),
            "rige desde vacia" => Campos("guardar", "20", null, ("Monday", "08:00", "12:00")),
            _ => Campos("guardar", "20", desde, ("Monday", "08:00", "")),
        };

        var respuesta = await EnviarAsync(cliente, M2, Formulario, campos);
        var html = await HtmlAsync(respuesta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains(mensaje, html);
        Assert.Null(Alerta(html));
        Assert.Empty(await app.AgendasDeAsync(medico!.Value));
    }

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "5")]
    [Trait("AC", "13")]
    [Trait("AC", "24")]
    public async Task GetMiPropuesta_RechazadaYCorregida_MuestraEstadoMotivoYFecha()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var (cliente, medico, _) = await app.MedicoIngresadoAsync();
        var desde = app.Hoy().AddDays(30);
        var rechazada = Agenda(medico!.Value, desde);
        rechazada.Enviar(app.Reloj);
        rechazada.Rechazar("El consultorio 3 está ocupado.", app.Reloj);
        await app.GuardarAgendaAsync(rechazada);
        var marca = TimeZoneInfo.ConvertTime(rechazada.RechazadaEl!.Value, app.Reloj.LocalTimeZone);
        var linea = $"Rechazada el {marca.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)} a las {marca.ToString("HH:mm", CultureInfo.InvariantCulture)}.";

        var antes = await HtmlAsync(await cliente.GetAsync(M2));
        var guardado = await EnviarAsync(cliente, M2, Formulario, Campos("guardar", "30", desde, ("Monday", "08:00", "12:00")));
        var despues = await HtmlAsync(await cliente.GetAsync(M2));

        Assert.Contains("Tu propuesta", antes);
        Assert.Contains("Estado: Rechazada", antes);
        Assert.Contains("Motivo del rechazo: «El consultorio 3 está ocupado.»", antes);
        Assert.Contains(linea, antes);
        Assert.Equal($"{M2}?resultado=borrador", Destino(guardado));
        Assert.Contains("Estado: Borrador", despues);
        Assert.Contains("Motivo del rechazo: «El consultorio 3 está ocupado.»", despues);
        Assert.Contains(linea, despues);
        Assert.Equal("30", ValorDe(despues, "Input.DuracionTurnoMinutos"));
    }

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "3")]
    public async Task GetMiPropuesta_AprobadaQueTodaviaNoRige_LoAvisa()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var (cliente, medico, _) = await app.MedicoIngresadoAsync();
        var desde = app.Hoy().AddDays(30);
        var aprobada = Agenda(medico!.Value, desde, 30, new Franja(DayOfWeek.Thursday, new TimeOnly(14, 0), new TimeOnly(18, 0)));
        aprobada.Enviar(app.Reloj);
        aprobada.Aprobar(app.Reloj);
        await app.GuardarAgendaAsync(aprobada);

        var html = await HtmlAsync(await cliente.GetAsync(M2));

        Assert.Contains("Todavía no tenés una agenda vigente.", html);
        Assert.Contains($"Tu agenda nueva ya está aprobada y rige desde el {Fecha(desde)}.", html);
        Assert.Contains("turnos de 30 min · Jue 14–18", html);
        Assert.Contains("Nueva propuesta", html);
        Assert.StartsWith("14:00", ValorDe(html, "Input.Franjas[0].Desde"));
        Assert.Equal("30", ValorDe(html, "Input.DuracionTurnoMinutos"));
        Assert.Equal(app.Hoy().AddDays(7).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), ValorDe(html, "Input.VigenteDesde"));
    }

    [Theory]
    [Trait("Story", "4.2")]
    [Trait("AC", "20")]
    [InlineData("admin1")]
    [InlineData("recepcion1")]
    [InlineData(null)]
    public async Task RutasDePropuesta_SinRolMedicoOSinSesion_NoEntran(string? usuario)
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        if (usuario is null)
        {
            var anonimo = await Cliente(app).GetAsync(M2);
            Assert.Equal(HttpStatusCode.Redirect, anonimo.StatusCode);
            Assert.Equal("/personal/ingreso?ReturnUrl=%2Fpersonal%2Fmedico%2Fmi-propuesta", Destino(anonimo));
            return;
        }
        var fijo = PersonalWebApplicationFactory.Fijos.Single(f => f.Usuario == usuario);
        var cliente = Cliente(app, seguirRedirecciones: true);
        await IngresarAsync(cliente, fijo.Usuario, fijo.Contrasena);

        var respuesta = await cliente.GetAsync(M2);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Contains("No tenés permiso para entrar a esta sección.", await HtmlAsync(respuesta));
    }

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "21")]
    public async Task PostMiPropuesta_MedicoDesactivadoConSesion_Responde403ConCartel()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var (cliente, medico, usuario) = await app.MedicoIngresadoAsync();
        var token = await TokenDeAsync(cliente, M2);
        using (var alcance = app.Services.CreateScope())
        {
            var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>();
            var leido = (await usuarios.FindByIdAsync(usuario.Id.ToString()))!;
            leido.Activo = false;
            Assert.True((await usuarios.UpdateAsync(leido)).Succeeded);
        }
        var campos = Campos("guardar", "20", app.Hoy().AddDays(30), ("Monday", "08:00", "12:00"));
        campos["_handler"] = Formulario;
        campos["__RequestVerificationToken"] = token;

        var respuesta = await cliente.PostAsync(M2, new FormUrlEncodedContent(campos));
        var html = await HtmlAsync(respuesta);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("Sólo un médico activo puede armar o enviar su propuesta de agenda.", Alerta(html));
        Assert.Empty(await app.AgendasDeAsync(medico!.Value));
    }

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "23")]
    public async Task PostMiPropuesta_SinTokenAntiforgery_Responde400SinGuardar()
    {
        await using var app = new PersonalWebApplicationFactory(fixture);
        var (cliente, medico, _) = await app.MedicoIngresadoAsync();

        var respuesta = await EnviarAsync(cliente, M2, Formulario,
            Campos("guardar", "20", app.Hoy().AddDays(30), ("Monday", "08:00", "12:00")), conToken: false);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Empty(await app.AgendasDeAsync(medico!.Value));
    }

    [GeneratedRegex("name=\"Input\\.Franjas\\[\\d+\\]\\.Dia\"")]
    private static partial Regex FilaDia();
}
