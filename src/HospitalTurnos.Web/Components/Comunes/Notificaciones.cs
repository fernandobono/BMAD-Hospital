namespace HospitalTurnos.Web.Components.Comunes;

/// <summary>
/// Las notificaciones de éxito del circuito (guía "Estados de la interfaz"; story 3.3, AC 20): arriba a la derecha,
/// 4 segundos, con el verbo en pasado ("Especialidad creada"). Una por circuito: sobrevive al pasar del alta a la
/// ficha. La espera usa el TimeProvider inyectado: se prueba adelantando el reloj, nunca esperando.
/// </summary>
public sealed class Notificaciones(TimeProvider reloj) : IDisposable
{
    public static readonly TimeSpan Duracion = TimeSpan.FromSeconds(4);

    private readonly List<Aviso> visibles = [];
    private readonly Dictionary<Guid, ITimer> temporizadores = [];
    private readonly Lock cerrojo = new();

    public sealed record Aviso(Guid Id, string Texto);

    /// <summary>Se dispara al aparecer o desaparecer una notificación (puede venir desde el hilo del temporizador).</summary>
    public event Action? Cambiaron;

    public IReadOnlyList<Aviso> Visibles
    {
        get { lock (cerrojo) return visibles.ToList(); }
    }

    public void Mostrar(string texto)
    {
        var aviso = new Aviso(Guid.NewGuid(), texto);
        lock (cerrojo)
        {
            visibles.Add(aviso);
            temporizadores[aviso.Id] = reloj.CreateTimer(_ => Quitar(aviso.Id), null, Duracion, Timeout.InfiniteTimeSpan);
        }
        Cambiaron?.Invoke();
    }

    private void Quitar(Guid id)
    {
        lock (cerrojo)
        {
            visibles.RemoveAll(a => a.Id == id);
            if (temporizadores.Remove(id, out var temporizador))
                temporizador.Dispose();
        }
        Cambiaron?.Invoke();
    }

    public void Dispose()
    {
        lock (cerrojo)
        {
            foreach (var temporizador in temporizadores.Values)
                temporizador.Dispose();
            temporizadores.Clear();
        }
    }
}
