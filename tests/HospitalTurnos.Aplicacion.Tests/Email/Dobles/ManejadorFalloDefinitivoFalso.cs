using HospitalTurnos.Aplicacion.Email;

namespace HospitalTurnos.Aplicacion.Tests.Email.Dobles;

/// <summary>Un "fake" de manejador de fallo definitivo: guarda cada aviso; puede fallar la primera vez.</summary>
public sealed class ManejadorFalloDefinitivoFalso(string tipo, bool fallaLaPrimeraVez = false) : IManejadorFalloDefinitivo
{
    private readonly List<FalloDefinitivoEmail> _recibidos = [];
    private int _invocaciones;

    public string Tipo { get; } = tipo;

    public int Invocaciones => Volatile.Read(ref _invocaciones);

    public IReadOnlyList<FalloDefinitivoEmail> Recibidos
    {
        get { lock (_recibidos) return [.. _recibidos]; }
    }

    public Task ManejarAsync(FalloDefinitivoEmail fallo, CancellationToken ct)
    {
        if (Interlocked.Increment(ref _invocaciones) == 1 && fallaLaPrimeraVez)
            throw new InvalidOperationException("El manejador falló");
        lock (_recibidos) _recibidos.Add(fallo);
        return Task.CompletedTask;
    }
}
