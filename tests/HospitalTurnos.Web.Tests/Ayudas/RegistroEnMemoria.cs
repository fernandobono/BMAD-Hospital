using Microsoft.Extensions.Logging;

namespace HospitalTurnos.Web.Tests.Ayudas;

/// <summary>
/// Un "fake" del registro técnico, escrito a mano: en vez de escribir en la consola, guarda cada
/// entrada en una lista para que la prueba pueda espiar qué se registró y con qué nivel.
/// </summary>
public sealed class RegistroEnMemoria<T> : ILogger<T>
{
    public List<(LogLevel Nivel, Exception? Excepcion, string Mensaje)> Entradas { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        Entradas.Add((logLevel, exception, formatter(state, exception)));
}
