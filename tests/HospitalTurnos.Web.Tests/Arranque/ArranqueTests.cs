namespace HospitalTurnos.Web.Tests.Arranque;

// Sin contenedor: en ninguno de los tres casos la app llega a tener una base válida.
public class ArranqueTests
{
    private const string BaseInaccesible = "Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x;Timeout=2";
    private const string CadenaCualquiera = "Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x";

    public static TheoryData<string?, string?, string> Casos => new()
    {
        { null, CadenaCualquiera, "Falta la cadena de conexión 'App'" },
        { CadenaCualquiera, null, "Falta la cadena de conexión 'Migraciones'" },
        { BaseInaccesible, BaseInaccesible, "No se pudieron aplicar las migraciones: la base no responde o rechazó al rol dueño (ConnectionStrings:Migraciones)." },
    };

    [Theory]
    [Trait("Story", "1.1")]
    [Trait("AC", "7")]
    [MemberData(nameof(Casos))]
    public async Task Arranque_SinCadenaOBaseInaccesible_FallaConMensajeQueNombraLaCadena(
        string? cadenaApp, string? cadenaMigraciones, string mensajeEsperado)
    {
        await using var fabrica = new HospitalWebApplicationFactory(cadenaApp, cadenaMigraciones);

        var error = Record.Exception(() => fabrica.CreateClient());

        Assert.NotNull(error);
        // El arranque puede llegar envuelto: se busca en toda la cadena de excepciones.
        var invalida = Cadena(error).OfType<InvalidOperationException>()
            .FirstOrDefault(e => e.Message.Contains(mensajeEsperado));
        Assert.True(invalida is not null,
            $"Se esperaba InvalidOperationException con «{mensajeEsperado}»; llegó: {error}");
    }

    private static IEnumerable<Exception> Cadena(Exception? e)
    {
        for (; e is not null; e = e.InnerException)
        {
            yield return e;
            if (e is AggregateException agregada)
                foreach (var interna in agregada.InnerExceptions.SelectMany(Cadena))
                    yield return interna;
        }
    }
}
