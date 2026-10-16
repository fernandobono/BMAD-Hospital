using System.Net;
using System.Text.RegularExpressions;
using HospitalTurnos.Infraestructura.Identidad;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalTurnos.Web.Tests.Personal;

/// <summary>
/// Lo que hace un navegador con la pantalla de ingreso, con HttpClient: GET de la página, tomar los
/// campos ocultos (token antifalsificación y _handler del formulario) y POST del formulario. Las cookies
/// viajan solas (HandleCookies). Sin JavaScript: S0 es un formulario de renderizado estático.
/// </summary>
public static partial class FormularioHttp
{
    public const string CookiePersonal = ".HospitalTurnos.Personal";
    public const string RutaIngreso = "/personal/ingreso";
    public const string MensajeGenerico = "Usuario o contraseña incorrectos.";

    public static HttpClient Cliente(WebApplicationFactory<Program> app, string baseAddress = "https://localhost",
        bool seguirRedirecciones = false) =>
        app.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(baseAddress),
            AllowAutoRedirect = seguirRedirecciones,
            HandleCookies = true,
        });

    /// <summary>Valor de un input del HTML por su atributo name (el orden de los atributos no importa).</summary>
    public static string? ValorDe(string html, string nombre)
    {
        foreach (Match input in Input().Matches(html))
        {
            var etiqueta = input.Value;
            if (!etiqueta.Contains($"name=\"{nombre}\"", StringComparison.Ordinal))
                continue;
            var valor = Valor().Match(etiqueta);
            return valor.Success ? WebUtility.HtmlDecode(valor.Groups[1].Value) : "";
        }
        return null;
    }

    /// <summary>GET de una página con formulario y el token antifalsificación que trae.</summary>
    public static async Task<string> TokenDeAsync(HttpClient cliente, string ruta)
    {
        var html = await (await cliente.GetAsync(ruta)).Content.ReadAsStringAsync();
        return ValorDe(html, "__RequestVerificationToken")
            ?? throw new InvalidOperationException($"{ruta} no trae token antifalsificación");
    }

    /// <summary>Lo que hace el navegador al enviar S0. returnUrl va en la dirección, como la deja el 302.</summary>
    public static async Task<HttpResponseMessage> IngresarAsync(HttpClient cliente, string usuario, string contrasena,
        string? returnUrl = null, bool conToken = true)
    {
        var ruta = returnUrl is null ? RutaIngreso : $"{RutaIngreso}?ReturnUrl={Uri.EscapeDataString(returnUrl)}";
        var html = await (await cliente.GetAsync(ruta)).Content.ReadAsStringAsync();
        var campos = new Dictionary<string, string>
        {
            ["_handler"] = ValorDe(html, "_handler") ?? throw new InvalidOperationException("S0 sin _handler"),
            ["Input.Usuario"] = usuario,
            ["Input.Contrasena"] = contrasena,
        };
        if (conToken)
            campos["__RequestVerificationToken"] = ValorDe(html, "__RequestVerificationToken")
                ?? throw new InvalidOperationException("S0 sin token antifalsificación");
        return await cliente.PostAsync(ruta, new FormUrlEncodedContent(campos));
    }

    /// <summary>El botón Salir del menú: POST con el token que trae la página de la sección.</summary>
    public static async Task<HttpResponseMessage> SalirAsync(HttpClient cliente, string desde, bool conToken = true)
    {
        var campos = new Dictionary<string, string>();
        if (conToken)
            campos["__RequestVerificationToken"] = await TokenDeAsync(cliente, desde);
        return await cliente.PostAsync("/personal/salir", new FormUrlEncodedContent(campos));
    }

    /// <summary>
    /// A dónde redirige, como ruta y consulta. La cookie y Blazor mandan el Location absoluto
    /// (https://localhost/...); Results.Redirect lo manda relativo. Se compara siempre la ruta.
    /// </summary>
    public static string Destino(HttpResponseMessage respuesta)
    {
        var location = respuesta.Headers.Location
            ?? throw new InvalidOperationException($"{(int)respuesta.StatusCode} sin Location");
        return location.IsAbsoluteUri ? location.PathAndQuery : location.OriginalString;
    }

    /// <summary>Los Set-Cookie de la cookie del personal (vacío si no vino).</summary>
    public static List<string> CookiesPersonal(HttpResponseMessage respuesta) =>
        respuesta.Headers.TryGetValues("Set-Cookie", out var valores)
            ? valores.Where(v => v.StartsWith(CookiePersonal + "=", StringComparison.Ordinal)).ToList()
            : [];

    /// <summary>Texto del elemento role="alert" (decodificado), o null si no hay.</summary>
    public static string? Alerta(string html)
    {
        var alerta = AlertaRegex().Match(html);
        return alerta.Success ? WebUtility.HtmlDecode(Etiquetas().Replace(alerta.Groups[1].Value, "")).Trim() : null;
    }

    /// <summary>Un usuario propio del test (p-xxxxxxxx), para todo flujo que cambia su estado.</summary>
    public static async Task<UsuarioPersonal> CrearUsuarioAsync(WebApplicationFactory<Program> app, string[] roles,
        string contrasena, bool activo = true, string nombre = "Persona de Prueba")
    {
        using var alcance = app.Services.CreateScope();
        var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>();
        var usuario = new UsuarioPersonal
        {
            UserName = $"p-{Guid.NewGuid().ToString("N")[..8]}",
            NombreCompleto = nombre,
            Activo = activo,
        };
        Exito(await usuarios.CreateAsync(usuario, contrasena));
        if (roles.Length > 0)
            Exito(await usuarios.AddToRolesAsync(usuario, roles));
        return usuario;
    }

    /// <summary>Falla la contraseña n veces directo contra Identity (más rápido que n formularios).</summary>
    public static async Task FallarAsync(WebApplicationFactory<Program> app, Guid id, int veces)
    {
        using var alcance = app.Services.CreateScope();
        var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>();
        var usuario = (await usuarios.FindByIdAsync(id.ToString()))!;
        for (var i = 0; i < veces; i++)
            Exito(await usuarios.AccessFailedAsync(usuario));
    }

    public static async Task<UsuarioPersonal> RecargarAsync(WebApplicationFactory<Program> app, Guid id)
    {
        using var alcance = app.Services.CreateScope();
        return (await alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>()
            .FindByIdAsync(id.ToString()))!;
    }

    private static void Exito(IdentityResult resultado)
    {
        if (!resultado.Succeeded)
            throw new InvalidOperationException(string.Join("; ", resultado.Errors.Select(e => e.Description)));
    }

    [GeneratedRegex("<input[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex Input();

    [GeneratedRegex("value=\"([^\"]*)\"", RegexOptions.IgnoreCase)]
    private static partial Regex Valor();

    [GeneratedRegex("<[^>]*role=\"alert\"[^>]*>(.*?)</", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex AlertaRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex Etiquetas();
}
