using HospitalTurnos.Aplicacion.Agendas;
using HospitalTurnos.Aplicacion.Auditoria;
using HospitalTurnos.Aplicacion.Tests.Ayudas;
using HospitalTurnos.Aplicacion.Tests.Catalogo;
using HospitalTurnos.Dominio.Personal;
using HospitalTurnos.Infraestructura.Identidad;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalTurnos.Aplicacion.Tests.Agendas;

/// <summary>
/// El "Given" de la propuesta del médico (story 4.2, dueña de estas ayudas; la 4.3 y la 4.4 les suman lo suyo).
/// Reusa DatosDeAgenda (4.1), DatosDeCatalogo (3.2) y UsuariosDePrueba (2.2) sin copiarlas.
/// </summary>
public static class DatosDePropuesta
{
    /// <summary>Un usuario con rol Médico vinculado a un médico propio, y su actor de auditoría.</summary>
    public static async Task<(long MedicoId, ActorAuditoria Actor, Guid UsuarioId)> MedicoConUsuarioAsync(
        this ServiceProvider proveedor, bool activo = true, string? email = null)
    {
        var usuario = await UsuariosDePrueba.CrearAsync(proveedor, [Roles.Medico], activo,
            nombre: $"Dra. Prueba {DatosDeAgenda.Marca()}");
        if (email is not null)
        {
            using var alcance = proveedor.CreateScope();
            var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioPersonal>>();
            var leido = (await usuarios.FindByIdAsync(usuario.Id.ToString()))!;
            Assert.True((await usuarios.SetEmailAsync(leido, email)).Succeeded);
        }
        var especialidad = await proveedor.EspecialidadPropiaAsync();
        var medicoId = await proveedor.CrearMedicoAsync("Ana", $"Prueba{DatosDeAgenda.Marca()}",
            $"T-{DatosDeAgenda.Marca().ToUpperInvariant()}", especialidad, usuarioPersonalId: usuario.Id);
        return (medicoId, UsuariosDePrueba.ActorDe(usuario, Roles.Medico), usuario.Id);
    }

    /// <summary>Los datos de una pantalla: (día, "HH:mm", "HH:mm") por franja.</summary>
    public static DatosAgenda Datos(int minutos, DateOnly vigenteDesde, params (DayOfWeek Dia, string Desde, string Hasta)[] franjas) =>
        new(minutos, vigenteDesde, [.. franjas.Select(f => new DatosFranja(f.Dia, TimeOnly.Parse(f.Desde), TimeOnly.Parse(f.Hasta)))]);

    /// <summary>IPropuestaAgenda resuelta del contenedor, un alcance por operación (como un pedido web).</summary>
    public static async Task<T> ConPropuestaAsync<T>(this ServiceProvider proveedor, Func<IPropuestaAgenda, Task<T>> operacion)
    {
        using var alcance = proveedor.CreateScope();
        return await operacion(alcance.ServiceProvider.GetRequiredService<IPropuestaAgenda>());
    }

    public static async Task ConPropuestaAsync(this ServiceProvider proveedor, Func<IPropuestaAgenda, Task> operacion)
    {
        using var alcance = proveedor.CreateScope();
        await operacion(alcance.ServiceProvider.GetRequiredService<IPropuestaAgenda>());
    }
}
