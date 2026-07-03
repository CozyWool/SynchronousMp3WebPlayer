using System.Text;

namespace SynchronousMp3WebPlayer.Helpers;

public sealed class FileLoggerProvider(string logsDirectory) : ILoggerProvider
{
    private readonly object _lock = new();

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, logsDirectory, _lock);
    }

    public void Dispose()
    {
    }

    private sealed class FileLogger(string categoryName, string logsDirectory, object fileLock) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel,
                                EventId eventId,
                                TState state,
                                Exception? exception,
                                Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var builder = new StringBuilder()
                          .Append(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"))
                          .Append(" [")
                          .Append(logLevel)
                          .Append("] ")
                          .Append(categoryName)
                          .Append(": ")
                          .Append(formatter(state, exception));

            if (exception is not null)
            {
                builder.AppendLine()
                       .Append(exception);
            }

            builder.AppendLine();

            lock (fileLock)
            {
                Directory.CreateDirectory(logsDirectory);
                File.AppendAllText(GetLogFilePath(logsDirectory), builder.ToString());
            }
        }

        private static string GetLogFilePath(string logsDirectory)
        {
            return Path.Combine(logsDirectory, $"app-{DateTimeOffset.Now:yyyy-MM-dd}.log");
        }
    }
}
