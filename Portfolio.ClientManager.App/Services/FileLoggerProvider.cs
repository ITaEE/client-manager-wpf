using System.Globalization;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Portfolio.ClientManager.App.Services;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly object _writeLock = new();
    private readonly string _logFilePath;

    public FileLoggerProvider(string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);
        _logFilePath = Path.Combine(logDirectory, "client-manager.log");
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(_logFilePath, _writeLock, categoryName);

    public void Dispose()
    {
    }

    private sealed class FileLogger(string logFilePath, object writeLock, string categoryName) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => NoopScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var exceptionSuffix = exception is null ? string.Empty : $" ({exception.GetType().Name})";
            var line = string.Format(
                CultureInfo.InvariantCulture,
                "{0:O} [{1}] {2}: {3}{4}{5}",
                DateTimeOffset.UtcNow,
                logLevel,
                categoryName,
                formatter(state, exception),
                exceptionSuffix,
                Environment.NewLine);

            try
            {
                lock (writeLock)
                {
                    File.AppendAllText(logFilePath, line);
                }
            }
            catch (Exception loggingException) when (loggingException is IOException or UnauthorizedAccessException)
            {
                // Logging must never prevent the local application from continuing.
            }
        }
    }

    private sealed class NoopScope : IDisposable
    {
        public static NoopScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
