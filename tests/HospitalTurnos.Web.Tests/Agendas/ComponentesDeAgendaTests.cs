using System.Reflection;
using Bunit;
using HospitalTurnos.Dominio.Agendas;
using HospitalTurnos.Web.Components.Personal.Agendas;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace HospitalTurnos.Web.Tests.Agendas;

// Lo que depende sólo de los datos al dibujar (story 4.2): el editor y el resumen que comparten M2, la 4.3 y la 4.4,
// y su contrato (AC 24). Sin servicios ni base. El editor se dibuja dentro de un EditForm, como lo usan las páginas.
public class ComponentesDeAgendaTests : BunitContext
{
    private static readonly DateOnly Lunes2 = new(2026, 11, 2);

    private static FilaFranja Fila(DayOfWeek dia, string desde, string hasta) =>
        new() { Dia = dia, Desde = TimeOnly.Parse(desde), Hasta = TimeOnly.Parse(hasta) };

    private static FormularioAgenda Formulario(int minutos, params FilaFranja[] filas) =>
        new() { DuracionTurnoMinutos = minutos, VigenteDesde = Lunes2, Franjas = [.. filas] };

    private IRenderedComponent<EditForm> DibujarEditor(FormularioAgenda input, int? antes = null, DateOnly? minimo = null)
    {
        RenderFragment<EditContext> contenido = _ => b =>
        {
            b.OpenComponent<EditorDeAgenda>(0);
            b.AddAttribute(1, nameof(EditorDeAgenda.Input), input);
            b.AddAttribute(2, nameof(EditorDeAgenda.TurnosPorSemanaAntes), antes);
            b.AddAttribute(3, nameof(EditorDeAgenda.MinimoVigenteDesde), minimo);
            b.CloseComponent();
        };
        return Render<EditForm>(p => p.Add(f => f.Model, input).Add(f => f.ChildContent, contenido));
    }

    private static AgendaMedico Agenda(int minutos, params (DayOfWeek Dia, string Desde, string Hasta)[] franjas)
    {
        var agenda = new AgendaMedico(1, minutos, Lunes2);
        foreach (var (dia, desde, hasta) in franjas)
            agenda.AgregarFranja(new Franja(dia, TimeOnly.Parse(desde), TimeOnly.Parse(hasta)));
        return agenda;
    }

    private static string Limpio(string texto) => string.Join(' ', texto.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "7")]
    [Trait("AC", "22")]
    [Trait("AC", "24")]
    public void EditorDeAgenda_ConFranjas_DibujaCamposConEtiquetaYBotones()
    {
        var editor = DibujarEditor(Formulario(20, Fila(DayOfWeek.Monday, "08:00", "12:00"), Fila(DayOfWeek.Tuesday, "08:00", "12:00")));

        var texto = Limpio(editor.Find("form").TextContent);
        foreach (var etiqueta in new[] { "Duración del turno (minutos)", "Rige desde", "Día", "Desde", "Hasta" })
            Assert.Contains(etiqueta, texto);
        Assert.Equal("Duración del turno (minutos)", editor.Find("label[for=duracion-turno]").TextContent.Trim());
        Assert.Equal("Rige desde", editor.Find("label[for=vigente-desde]").TextContent.Trim());
        Assert.Contains("Para terminar a medianoche, poné 00:00.", texto);

        var quitar = editor.FindAll("button").Where(b => b.TextContent.Trim() == "Quitar").ToList();
        Assert.Equal(["quitar-franja:0", "quitar-franja:1"], quitar.Select(b => b.GetAttribute("value")));
        Assert.All(quitar, b => Assert.Equal("Input.Accion", b.GetAttribute("name")));
        var agregar = editor.FindAll("button").Single(b => b.TextContent.Trim() == "+ Agregar franja");
        var actualizar = editor.FindAll("button")
            .Single(b => b.TextContent.Trim() == "Actualizar el cálculo" && !b.ClassList.Contains("visually-hidden"));
        Assert.Equal(("Input.Accion", "agregar-franja"), (agregar.GetAttribute("name"), agregar.GetAttribute("value")));
        Assert.Equal(("Input.Accion", "actualizar"), (actualizar.GetAttribute("name"), actualizar.GetAttribute("value")));
        // Enter envía con el primer botón del formulario: tiene que ser "actualizar", nunca quitar ni enviar.
        Assert.Equal("actualizar", editor.FindAll("button[type=submit]").First().GetAttribute("value"));
        Assert.All(editor.FindAll("button"), b => Assert.True(b.HasAttribute("formnovalidate")));
        Assert.DoesNotContain(editor.FindAll("[name]"), e =>
            e.GetAttribute("name")!.Contains("MedicoId", StringComparison.OrdinalIgnoreCase)
            || e.GetAttribute("name")!.Contains("AgendaId", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [Trait("Story", "4.2")]
    [Trait("AC", "9")]
    [Trait("AC", "24")]
    [InlineData("superpuesta")]
    [InlineData("no multiplo")]
    public void EditorDeAgenda_FilaConProblema_LaMarcaConColorYTexto(string caso)
    {
        var editor = caso == "superpuesta"
            ? DibujarEditor(Formulario(20, Fila(DayOfWeek.Monday, "08:00", "12:00"), Fila(DayOfWeek.Monday, "10:00", "14:00")))
            : DibujarEditor(Formulario(20, Fila(DayOfWeek.Monday, "08:00", "10:50")));

        var filas = editor.FindAll("tbody tr");
        if (caso == "superpuesta")
        {
            Assert.False(filas[0].ClassList.Contains("table-danger"));
            Assert.True(filas[1].ClassList.Contains("table-danger"));
            Assert.Contains("La franja del lunes de 10:00 a 14:00 se superpone con la de 08:00 a 12:00.", filas[1].TextContent);
            return;
        }
        Assert.False(Assert.Single(filas).ClassList.Contains("table-danger"));
        Assert.Contains("Los últimos 10 minutos de esta franja no se usan: no entra un turno completo.", filas[0].TextContent);
    }

    [Theory]
    [Trait("Story", "4.2")]
    [Trait("AC", "7")]
    [Trait("AC", "10")]
    [Trait("AC", "24")]
    [InlineData("valido con antes", "Con esta agenda ofrecés 20 turnos por semana (antes: 36).")]
    [InlineData("valido sin antes", "Con esta agenda ofrecés 20 turnos por semana.")]
    [InlineData("superposicion", "Corregí lo marcado para ver cuántos turnos ofrecés.")]
    [InlineData("sin filas", "Agregá franjas para ver cuántos turnos ofrecés por semana.")]
    [InlineData("duracion invalida sin filas", "Corregí lo marcado para ver cuántos turnos ofrecés.")]   // la duración manda
    public void EditorDeAgenda_SegunLaRevision_MuestraElCalculoYLaFechaMinima(string caso, string esperado)
    {
        var formulario = caso switch
        {
            "superposicion" => Formulario(20, Fila(DayOfWeek.Monday, "08:00", "12:00"), Fila(DayOfWeek.Monday, "10:00", "14:00")),
            "sin filas" => Formulario(20),
            "duracion invalida sin filas" => Formulario(5),
            _ => Formulario(20, Fila(DayOfWeek.Monday, "08:00", "12:00"), Fila(DayOfWeek.Wednesday, "08:00", "10:50")),
        };

        var editor = DibujarEditor(formulario, caso == "valido con antes" ? 36 : null, new DateOnly(2026, 10, 23));

        var texto = Limpio(editor.Find("form").TextContent);
        Assert.Contains(esperado, texto);
        Assert.Contains("(mín. 23/10/2026)", texto);
    }

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "3")]
    [Trait("AC", "24")]
    public void ResumenDeAgenda_EnLinea_MuestraDuracionFranjasYTurnosPorSemana()
    {
        var agenda = Agenda(20, (DayOfWeek.Friday, "20:00", "00:00"), (DayOfWeek.Monday, "08:00", "12:00"),
            (DayOfWeek.Wednesday, "08:30", "12:00"));
        var unaHora = Agenda(60, (DayOfWeek.Monday, "08:00", "09:00"));

        var resumen = Render<ResumenDeAgenda>(p => p.Add(r => r.Agenda, agenda));
        var singular = Render<ResumenDeAgenda>(p => p.Add(r => r.Agenda, unaHora));

        var renglones = resumen.FindAll("div.renglon").Select(r => r.TextContent.Trim()).ToList();
        Assert.Equal(["turnos de 20 min · Lun 8–12 · Mié 8:30–12 · Vie 20–24", "34 turnos/semana"], renglones);
        Assert.Equal("1 turno/semana", singular.FindAll("div.renglon").Last().TextContent.Trim());
    }

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "24")]
    public void ResumenDeAgenda_EnLista_UnaFranjaPorRenglonDeLunesADomingo()
    {
        var agenda = Agenda(20, (DayOfWeek.Sunday, "08:00", "12:00"), (DayOfWeek.Monday, "08:00", "12:00"));

        var resumen = Render<ResumenDeAgenda>(p => p.Add(r => r.Agenda, agenda).Add(r => r.Forma, FormaDeResumen.EnLista));
        var vacia = Render<ResumenDeAgenda>(p => p.Add(r => r.Agenda, Agenda(20)).Add(r => r.Forma, FormaDeResumen.EnLista));

        Assert.Equal(["Lun 8–12", "Dom 8–12"], resumen.FindAll("li").Select(li => li.TextContent.Trim()));
        Assert.Equal(["Lun 8–12", "Dom 8–12"],
            agenda.Franjas.OrderBy(f => ((int)f.Dia + 6) % 7).Select(ResumenDeAgenda.TextoDeFranja));
        Assert.Contains("turnos de 20 min", resumen.Markup);
        Assert.Contains("Sin franjas", vacia.Find("div").TextContent);
        Assert.Empty(vacia.FindAll("li"));
    }

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "24")]
    public void ComponentesCompartidos_Contrato_NombresParametrosYUbicacionFijos()
    {
        const string Espacio = "HospitalTurnos.Web.Components.Personal.Agendas";
        static string Parametros(Type tipo) => string.Join(", ", tipo
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
            .OrderBy(p => p.Name)
            .Select(p => $"{p.Name}:{Nombre(p)}{(p.GetCustomAttribute<EditorRequiredAttribute>() is null ? "" : "!")}"));
        static string Nombre(PropertyInfo p)
        {
            var nulo = new NullabilityInfoContext().Create(p).ReadState == NullabilityState.Nullable;
            var subyacente = Nullable.GetUnderlyingType(p.PropertyType);
            return (subyacente ?? p.PropertyType).Name + (nulo || subyacente is not null ? "?" : "");
        }

        Assert.All(new[] { typeof(EditorDeAgenda), typeof(ResumenDeAgenda), typeof(FormularioAgenda), typeof(FormaDeResumen) },
            t => Assert.Equal(Espacio, t.Namespace));
        Assert.Equal("Input:FormularioAgenda!, MinimoVigenteDesde:DateOnly?, TurnosPorSemanaAntes:Int32?", Parametros(typeof(EditorDeAgenda)));
        Assert.Equal("Agenda:AgendaMedico!, CompararCon:AgendaMedico?, Forma:FormaDeResumen", Parametros(typeof(ResumenDeAgenda)));
        Assert.Equal(["EnLinea", "EnLista"], Enum.GetNames<FormaDeResumen>());

        var formulario = typeof(FormularioAgenda);
        Assert.False(formulario.IsSealed);
        Assert.Equal(typeof(bool), formulario.GetMethod(nameof(FormularioAgenda.AplicarAccionDeEdicion))!.ReturnType);
        Assert.Equal(typeof(RevisionDeAgenda), formulario.GetMethod(nameof(FormularioAgenda.Revisar))!.ReturnType);
        Assert.Equal("DatosAgenda", formulario.GetMethod(nameof(FormularioAgenda.ADatos))!.ReturnType.Name);
        Assert.True(formulario.GetMethod(nameof(FormularioAgenda.Desde), [typeof(AgendaMedico)])!.IsStatic);
        Assert.True(formulario.GetMethod(nameof(FormularioAgenda.Nueva), [typeof(DateOnly)])!.IsStatic);
        Assert.Equal(typeof(string), typeof(ResumenDeAgenda).GetMethod(nameof(ResumenDeAgenda.TextoDeFranja), [typeof(Franja)])!.ReturnType);

        var turnos = typeof(AgendaMedico).GetMethod(nameof(AgendaMedico.TurnosPorSemana), Type.EmptyTypes)!;
        Assert.True(turnos.IsPublic);
        Assert.Equal(typeof(int), turnos.ReturnType);
    }

    // El modelo de la 4.4 (FormularioCorreccion) hereda del de M2 y suma su motivo (C-1 de la revisión).
    private sealed class FormularioQueHereda : FormularioAgenda
    {
        public string? Motivo { get; set; }
    }

    [Fact]
    [Trait("Story", "4.2")]
    [Trait("AC", "24")]
    public void EditorDeAgenda_ModeloHeredado_DibujaLosMismosNombresDeCampo()
    {
        var input = new FormularioQueHereda
        {
            DuracionTurnoMinutos = 20, VigenteDesde = Lunes2, Motivo = "Licencia", Franjas = [Fila(DayOfWeek.Monday, "08:00", "12:00")],
        };

        var editor = DibujarEditor(input);

        var nombres = editor.FindAll("[name]").Select(e => e.GetAttribute("name")).Distinct().ToList();
        foreach (var nombre in new[] { "Input.DuracionTurnoMinutos", "Input.VigenteDesde", "Input.Franjas[0].Dia",
                     "Input.Franjas[0].Desde", "Input.Franjas[0].Hasta", "Input.Accion" })
            Assert.Contains(nombre, nombres);
        Assert.DoesNotContain("Motivo", editor.Markup);

        input.Accion = "agregar-franja";
        Assert.True(input.AplicarAccionDeEdicion());
        Assert.Equal(2, input.Franjas.Count);
    }

    [Theory]
    [Trait("Story", "4.2")]
    [Trait("AC", "24")]
    [InlineData("cambia el martes")]
    [InlineData("cambia la duracion")]
    [InlineData("sin comparar")]
    public void ResumenDeAgenda_ConCompararCon_MarcaLoQueCambia(string caso)
    {
        var agenda = Agenda(20, (DayOfWeek.Tuesday, "08:00", "12:00"), (DayOfWeek.Thursday, "14:00", "18:00"));
        var comparar = caso switch
        {
            "cambia el martes" => Agenda(20, (DayOfWeek.Tuesday, "14:00", "18:00"), (DayOfWeek.Thursday, "14:00", "18:00")),
            "cambia la duracion" => Agenda(30, (DayOfWeek.Tuesday, "08:00", "12:00"), (DayOfWeek.Thursday, "14:00", "18:00")),
            _ => null,
        };

        var resumen = Render<ResumenDeAgenda>(p => p.Add(r => r.Agenda, agenda).Add(r => r.Forma, FormaDeResumen.EnLista)
            .Add(r => r.CompararCon, comparar));

        var items = resumen.FindAll("li").Select(li => li.TextContent.Trim()).ToList();
        var duracion = resumen.FindAll("div.renglon").First().TextContent.Trim();
        Assert.Equal(caso switch
        {
            "cambia el martes" => ["Mar 8–12 ← cambia", "Jue 14–18"],
            _ => (string[])["Mar 8–12", "Jue 14–18"],
        }, items);
        Assert.Equal(caso == "cambia la duracion" ? "turnos de 20 min ← cambia" : "turnos de 20 min", duracion);
    }
}
