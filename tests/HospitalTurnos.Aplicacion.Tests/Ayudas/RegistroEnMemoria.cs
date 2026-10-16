using Microsoft.Extensions.Logging;

namespace HospitalTurnos.Aplicacion.Tests.Ayudas;

/// <summary>
/// Un "fake" del registro técnico, escrito a mano: en vez de escribir en la consola, guarda cada
/// entrada en una lista para que la prueba pueda espiar qué se registró y con qué nivel.
/// Copia del de Web.Tests (story 1.2): en el sprint 1 no hay proyecto de utilidades compartido.
/// La lista es concurrente porque el proceso de fondo escribe desde otro hilo que la prueba.
/// </summary>
public sealed class RegistroEnMemoria<T> : ILogger<T>
{
    private readonly List<(LogLevel Nivel, Exception? Excepcion, string Mensaje)> _entradas = [];

    public IReadOnlyList<(LogLevel Nivel, Exception? Excepcion, string Mensaje)> Entradas
    {
        get { lock (_entradas) return [.. _entradas]; }
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        lock (_entradas) _entradas.Add((logLevel, exception, formatter(state, exception)));
    }
}
