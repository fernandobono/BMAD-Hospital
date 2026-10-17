using HospitalTurnos.Dominio.Agendas;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Infraestructura.Identidad;
using HospitalTurnos.Infraestructura.Persistencia;
using HospitalTurnos.Web.Tests.Catalogo;
using HospitalTurnos.Web.Tests.Personal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace HospitalTurnos.Web.Tests.Agendas;

/// <summary>
/// El "Given" de M2 en las pruebas de Web (story 4.2, dueña de estas ayudas; la 4.3 les suma
/// VincularMedicoConEmailAsync y la 4.4 las reusa). Usuarios y médicos propios: la base es compartida.
/// </summary>
public static class DatosDePropuesta
{
    public const string Clave = "Clave-Medica-2026";

    /// <summary>Un usuario médico propio, ya ingresado, y (si vinculado) su médico.</summary>
    public static async Task<(HttpClient Cliente, long? MedicoId, UsuarioPersonal Usuario)> MedicoIngresadoAsync(
        this PersonalWebApplicationFactory app, bool vinculado = true, bool seguirRedirecciones = false)
    {
        var marca = Guid.NewGuid().ToString("N")[..8];
        var usuario = await FormularioHttp.CrearUsuarioAsync(app, [Roles.Medico], Clave, nombre: $"Dra. Prueba {marca}");
        long? medicoId = null;
        if (vinculado)
        {
            var especialidad = await app.CrearEspecialidadAsync(nombre: $"Esp{marca}");
            medicoId = await app.CrearMedicoAsync("Ana", $"Prueba{marca}", $"T-{marca.ToUpperInvariant()}", especialidad, usuario.Id);
        }
        var cliente = FormularioHttp.Cliente(app, seguirRedirecciones: seguirRedirecciones);
        await FormularioHttp.IngresarAsync(cliente, usuario.UserName!, Clave);
        return (cliente, medicoId, usuario);
    }

    /// <summary>Guarda una agenda ya armada con los métodos del dominio.</summary>
    public static async Task<long> GuardarAgendaAsync(this PersonalWebApplicationFactory app, AgendaMedico agenda)
    {
        using var alcance = app.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<HospitalDbContext>();
        contexto.AgendasMedico.Add(agenda);
        await contexto.SaveChangesAsync();
        return agenda.Id;
    }

    public static async Task<List<AgendaMedico>> AgendasDeAsync(this PersonalWebApplicationFactory app, long medicoId)
    {
        using var alcance = app.Services.CreateScope();
        return await alcance.ServiceProvider.GetRequiredService<HospitalDbContext>().AgendasMedico.AsNoTracking()
            .Include(a => a.Franjas).Where(a => a.MedicoId == medicoId).OrderBy(a => a.Id).ToListAsync();
    }

    /// <summary>
    /// Un reloj falso corrido N días desde el de la app (nunca se adelanta app.Reloj: cortaría la sesión). Mismo nombre
    /// e idea que la ayuda privada de EstadoDeAgendaEnA1HttpTests (4.1), que no se toca.
    /// </summary>
    public static FakeTimeProvider RelojCorridoDias(FakeTimeProvider base_, int dias)
    {
        var reloj = new FakeTimeProvider(base_.GetUtcNow().AddDays(dias));
        reloj.SetLocalTimeZone(base_.LocalTimeZone);
        return reloj;
    }

    /// <summary>"Hoy" del hospital, según el reloj de la app.</summary>
    public static DateOnly Hoy(this PersonalWebApplicationFactory app) => DateOnly.FromDateTime(app.Reloj.GetLocalNow().DateTime);
}
