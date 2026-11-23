// -----------------------------------------------------------------------------
// Author:        MJZgh
// AI Assistance: Claude for logging Documentation
// -----------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Photon.App.Logging;

/// <summary>
/// Configures where log entries are written: console and a log file.
/// </summary>
internal static class LoggingSetup
{
    /// <summary>The path of the log file.</summary>
    private static readonly string _logFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Photon",
        "logs",
        "photon.log"
    );

    /// <summary>
    /// Creates the logger factory used by the whole application.
    /// </summary>
    /// <returns>The configured logger factory; the caller disposes it.</returns>
    public static ILoggerFactory Create()
    {
        return LoggerFactory.Create(builder =>
            builder
                .SetMinimumLevel(LogLevel.Information)
                .AddSimpleConsole(options => options.SingleLine = true)
                .AddProvider(new FileLoggerProvider(_logFilePath))
        );
    }
}
