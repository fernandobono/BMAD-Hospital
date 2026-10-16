using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace HospitalTurnos.Web.Tests.Ayudas;

/// <summary>
/// Fake del registro técnico de TODA la aplicación: un ILogger por categoría que anota cada entrada
/// en una cola común. Sirve para contar cuántas veces se registró una excepción, venga de quien venga.
/// La cola es thread-safe porque la app registra desde varios hilos.
/// </summary>
public sealed class ProveedorDeRegistroEnMemoria : ILoggerProvider
{
    public ConcurrentQueue<(string Categoria, LogLevel Nivel, Exception? Excepcion, string Mensaje)> Entradas { get; } = new();

    public ILogger CreateLogger(string categoryName) => new Registro(categoryName, Entradas);

    public void Dispose()
    {
    }

    private sealed class Registro(
        string categoria,
        ConcurrentQueue<(string Categoria, LogLevel Nivel, Exception? Excepcion, string Mensaje)> entradas) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            entradas.Enqueue((categoria, logLevel, exception, formatter(state, exception)));
    }
}
