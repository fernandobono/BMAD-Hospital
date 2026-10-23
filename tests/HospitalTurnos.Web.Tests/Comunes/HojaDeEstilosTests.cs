using System.Globalization;
using System.Text.RegularExpressions;

namespace HospitalTurnos.Web.Tests.Comunes;

// Story 3.3, sección B (guía "Diseño visual"): todo el aspecto sale de una hoja única con tokens. Se prueba
// leyendo los archivos del repo, como ConfiguracionVersionadaTests.
public partial class HojaDeEstilosTests
{
    private static string Raiz()
    {
        var carpeta = new DirectoryInfo(AppContext.BaseDirectory);
        while (carpeta is not null && !File.Exists(Path.Combine(carpeta.FullName, "HospitalTurnos.slnx")))
            carpeta = carpeta.Parent;
        return carpeta?.FullName ?? throw new InvalidOperationException("No se encontró HospitalTurnos.slnx");
    }

    private static string Web => Path.Combine(Raiz(), "src", "HospitalTurnos.Web");
    private static string Tokens => File.ReadAllText(Path.Combine(Web, "wwwroot", "css", "tokens.css"));
    private static string App => File.ReadAllText(Path.Combine(Web, "wwwroot", "app.css"));

    [GeneratedRegex(@"#[0-9a-fA-F]{3,8}\b")]
    private static partial Regex Hexadecimal();

    [GeneratedRegex(@"--(?<nombre>[a-z0-9-]+):\s*(?<valor>[^;]+);")]
    private static partial Regex Variable();

    private static Dictionary<string, string> VariablesDeTokens() =>
        Variable().Matches(Tokens).ToDictionary(m => m.Groups["nombre"].Value, m => m.Groups["valor"].Value.Trim());

    private static string Relativa(string ruta) => Path.GetRelativePath(Raiz(), ruta);

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "6")]
    public void Tokens_DefinenColoresTipografiaEspaciadoYRadio()
    {
        var v = VariablesDeTokens();

        Assert.Equal("#1F5FA8", v["color-primario"], ignoreCase: true);
        Assert.Equal("#B45309", v["color-atencion"], ignoreCase: true);
        foreach (var color in new[] { "color-exito", "color-peligro", "color-info", "color-neutro" })
            Assert.Matches("^#[0-9a-fA-F]{6}$", v[color]);
        Assert.Contains("system-ui", v["fuente-base"]);
        Assert.Equal("1.75rem", v["texto-titulo"]);      // 28 px
        Assert.Equal("1.25rem", v["texto-subtitulo"]);   // 20 px
        Assert.Equal("1rem", v["texto-cuerpo"]);         // 16 px
        Assert.Equal("0.875rem", v["texto-ayuda"]);      // 14 px
        Assert.Equal(["0.5rem", "1rem", "1.5rem", "2rem"], new[] { v["esp-1"], v["esp-2"], v["esp-3"], v["esp-4"] });
        Assert.True(v.ContainsKey("radio"));
        Assert.True(v.ContainsKey("sombra"));
        Assert.Equal("var(--color-primario)", v["bs-primary"]);      // Bootstrap se personaliza desde los tokens
        var app = File.ReadAllText(Path.Combine(Web, "Components", "App.razor"));
        Assert.True(app.IndexOf("css/tokens.css", StringComparison.Ordinal) < app.IndexOf("\"app.css\"", StringComparison.Ordinal),
            "App.razor tiene que cargar tokens.css antes que app.css");
        Assert.True(app.IndexOf("bootstrap.min.css", StringComparison.Ordinal) < app.IndexOf("css/tokens.css", StringComparison.Ordinal),
            "tokens.css va después de Bootstrap para poder pisar sus --bs-*");
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "7")]
    public void NingunHexadecimal_FueraDeTokens()
    {
        var raiz = Path.Combine(Web, "wwwroot");
        var hojas = Directory.EnumerateFiles(raiz, "*.css", SearchOption.AllDirectories)
            .Where(r => !r.Contains($"{Path.DirectorySeparatorChar}lib{Path.DirectorySeparatorChar}"))
            .Where(r => Path.GetFileName(r) != "tokens.css");

        var conHexadecimal = hojas.Where(r => Hexadecimal().IsMatch(File.ReadAllText(r))).Select(Relativa).ToList();

        Assert.Empty(conHexadecimal);
        foreach (var clase in new[] { ".page-header", ".filter-bar", ".table-actions", ".badge-estado--vigente",
                     ".badge-estado--programada", ".badge-estado--pendiente", ".badge-estado--inactivo",
                     ".badge-estado--activo", ".badge-estado--info" })
            Assert.Contains(clase, App);
    }

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "8")]
    public void NingunComponente_UsaStyleEnLinea_NiHexadecimal()
    {
        var componentes = Path.Combine(Web, "Components");
        var razor = Directory.EnumerateFiles(componentes, "*.razor", SearchOption.AllDirectories).ToList();
        var aisladas = Directory.EnumerateFiles(componentes, "*.razor.css", SearchOption.AllDirectories).ToList();

        Assert.NotEmpty(razor);
        Assert.Empty(razor.Where(r => File.ReadAllText(r).Contains("style=\"", StringComparison.Ordinal)).Select(Relativa));
        Assert.Empty(razor.Where(r => Hexadecimal().IsMatch(File.ReadAllText(r))).Select(Relativa));
        Assert.Empty(aisladas.Where(r => Hexadecimal().IsMatch(File.ReadAllText(r))).Select(Relativa));
    }

    [GeneratedRegex(@"(?<selector>[^{}]+)\{(?<cuerpo>[^}]*)\}")]
    private static partial Regex Regla();

    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "9")]
    public void HojaDeEstilos_NoAnulaElFoco()
    {
        var reglas = Regla().Matches(App).Select(m => (Selector: m.Groups["selector"].Value.Trim(), Cuerpo: m.Groups["cuerpo"].Value)).ToList();

        var anulan = reglas.Where(r => Regex.IsMatch(r.Cuerpo, @"outline\s*:\s*(none|0)\b")).Select(r => r.Selector).ToList();
        Assert.Empty(anulan);
        Assert.Contains(reglas, r => r.Selector.Contains(":focus-visible")
            && Regex.IsMatch(r.Cuerpo, @"outline\s*:\s*3px solid var\(--color-primario\)"));
    }

    // En la tarjeta, las acciones no heredan el ancho de 1 % de la columna de la tabla: en un flex ese ancho es la base
    // y las empujaba fuera de la pantalla del celular (prueba en vivo, revisión de legibilidad de la pantalla).
    [Fact]
    [Trait("Story", "3.3")]
    [Trait("AC", "29")]
    public void Tarjeta_LasAccionesNoSeAchicanNiSalenDeLaPantalla()
    {
        var reglas = Regla().Matches(App).Select(m => (Selector: m.Groups["selector"].Value.Trim(), Cuerpo: m.Groups["cuerpo"].Value)).ToList();

        // El selector puede traer pegado el comentario que lo precede: se compara el final.
        Assert.Contains(reglas, r => r.Selector.EndsWith(".tarjeta-abm .table-actions", StringComparison.Ordinal)
            && Regex.IsMatch(r.Cuerpo, @"width\s*:\s*auto") && Regex.IsMatch(r.Cuerpo, @"flex-shrink\s*:\s*0"));
        Assert.Contains(reglas, r => r.Selector.EndsWith(".tarjeta-abm__datos", StringComparison.Ordinal)
            && Regex.IsMatch(r.Cuerpo, @"flex\s*:\s*1 1 auto") && Regex.IsMatch(r.Cuerpo, @"overflow-wrap\s*:\s*anywhere")
            && Regex.IsMatch(r.Cuerpo, @"align-items\s*:\s*flex-start"));   // el badge, del ancho de su texto
    }

    // Cada par texto/fondo que la hoja usa, por nombre de token (AC 10). Mínimo 4,5 para texto y 3 para bordes.
    public static TheoryData<string, string, double> Pares => new()
    {
        { "color-texto", "color-fondo", 4.5 },
        { "color-texto", "color-fondo-app", 4.5 },
        { "color-texto-ayuda", "color-fondo", 4.5 },
        { "color-sobre-primario", "color-primario", 4.5 },
        { "color-primario", "color-fondo", 4.5 },
        { "color-primario", "color-fondo-app", 4.5 },
        { "color-primario", "color-info-suave", 4.5 },
        { "color-sobre-primario", "color-peligro", 4.5 },
        { "color-peligro", "color-fondo", 4.5 },
        { "color-peligro", "color-peligro-suave", 4.5 },
        { "color-exito", "color-exito-suave", 4.5 },
        { "color-info", "color-info-suave", 4.5 },
        { "color-atencion", "color-atencion-suave", 4.5 },
        { "color-neutro", "color-neutro-suave", 4.5 },
        { "color-borde", "color-fondo", 3.0 },
    };

    [Theory]
    [Trait("Story", "3.3")]
    [Trait("AC", "10")]
    [MemberData(nameof(Pares))]
    public void ParesDeColor_CumplenContrasteAA(string texto, string fondo, double minimo)
    {
        var v = VariablesDeTokens();

        var contraste = Contraste(v[texto], v[fondo]);

        Assert.True(contraste >= minimo, $"{texto} sobre {fondo}: {contraste:0.00} < {minimo}");
    }

    // Fórmula de WCAG 2.2 (luminancia relativa).
    private static double Contraste(string a, string b)
    {
        var (la, lb) = (Luminancia(a), Luminancia(b));
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static double Luminancia(string hexadecimal)
    {
        var h = hexadecimal.TrimStart('#');
        double Canal(int i)
        {
            var c = int.Parse(h.Substring(i, 2), NumberStyles.HexNumber) / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Canal(0) + 0.7152 * Canal(2) + 0.0722 * Canal(4);
    }
}
