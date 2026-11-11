// -----------------------------------------------------------------------------
// Author:        MJZgh
// AI Assistance: Claude for logging Documentation
// -----------------------------------------------------------------------------

using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Photon.App.Logging;

/// <summary>
/// Writes log entries as text lines to a file. Safe to use from several threads.
/// </summary>
internal sealed class FileLoggerProvider : ILoggerProvider
{
    /// <summary>Serializes writes from all threads.</summary>
    private readonly object _lock = new();

    /// <summary>The writer for the log file.</summary>
    private readonly StreamWriter _writer;

    /// <summary>Whether the provider was disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileLoggerProvider"/> class.
    /// </summary>
    /// <param name="path">The log file path; missing directories are created.</param>
    public FileLoggerProvider(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        _writer = new StreamWriter(stream) { AutoFlush = true };
    }

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, this);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_lock)
        {
            _disposed = true;
            _writer.Dispose();
        }
    }

    /// <summary>Writes one entry as a line.</summary>
    /// <param name="level">The severity.</param>
    /// <param name="category">The logger category.</param>
    /// <param name="message">The formatted message.</param>
    /// <param name="exception">An optional exception.</param>
    private void Write(LogLevel level, string category, string message, Exception? exception)
    {
        var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _writer.WriteLine($"{time} [{level}] {category}: {message}");
            if (exception is not null)
            {
                _writer.WriteLine(exception);
            }
        }
    }

    /// <summary>A logger that forwards to its provider.</summary>
    private sealed class FileLogger : ILogger
    {
        /// <summary>The logger category.</summary>
        private readonly string _category;

        /// <summary>The owning provider.</summary>
        private readonly FileLoggerProvider _provider;

        /// <summary>Initializes a new instance of the <see cref="FileLogger"/> class.</summary>
        /// <param name="category">The logger category.</param>
        /// <param name="provider">The owning provider.</param>
        public FileLogger(string category, FileLoggerProvider provider)
        {
            _category = category;
            _provider = provider;
        }

        /// <inheritdoc/>
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        /// <inheritdoc/>
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            if (IsEnabled(logLevel))
            {
                _provider.Write(logLevel, _category, formatter(state, exception), exception);
            }
        }
    }
}
