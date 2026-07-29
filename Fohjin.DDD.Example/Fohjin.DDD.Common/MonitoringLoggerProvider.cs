using Microsoft.Extensions.Logging;

namespace Fohjin.DDD.Common;

public class MonitoringLoggerProvider : ILoggerProvider
{
    public event Action<string>? LineLogged;

    public ILogger CreateLogger(string categoryName) =>
        new MonitoringLogger(categoryName, line => LineLogged?.Invoke(line));

    public void Dispose() { }

    private sealed class MonitoringLogger(string categoryName, Action<string> sink) : ILogger
    {
        private readonly string _categoryName = categoryName;
        private readonly Action<string> _sink = sink;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            _sink($"{DateTime.Now:HH:mm:ss.fff} [{logLevel}] {_categoryName}: {formatter(state, exception)}");
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
