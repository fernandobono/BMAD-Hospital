namespace HospitalTurnos.Web.Personal;

/// <summary>
/// Un alcance de servicios por operación en las páginas interactivas (story 3.3, AC 94; arquitectura §5.1). El
/// alcance del circuito dura lo que la pestaña, y con él el HospitalDbContext: dos consultas a la vez chocarían y
/// un guardado que el dominio rechaza quedaría en memoria para el siguiente. Acá cada consulta o acción pide su caso
/// de uso a un alcance propio y lo descarta al terminar, igual que un pedido HTTP en las páginas estáticas.
/// </summary>
public sealed class AlcancePorOperacion(IServiceScopeFactory alcances)
{
    public async Task<TResultado> EjecutarAsync<TServicio, TResultado>(Func<TServicio, Task<TResultado>> operacion)
        where TServicio : notnull
    {
        await using var alcance = alcances.CreateAsyncScope();
        return await operacion(alcance.ServiceProvider.GetRequiredService<TServicio>());
    }

    public async Task EjecutarAsync<TServicio>(Func<TServicio, Task> operacion) where TServicio : notnull
    {
        await using var alcance = alcances.CreateAsyncScope();
        await operacion(alcance.ServiceProvider.GetRequiredService<TServicio>());
    }
}
