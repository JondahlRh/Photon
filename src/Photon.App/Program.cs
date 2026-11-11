// -----------------------------------------------------------------------------
// Author:        XISander, BrackyDayn, MJZgh
// AI Assistance: Claude for project skeleton and logging documentation; ChatGPT for macOS OpenGL configuration and help with understanding OpenGL
// -----------------------------------------------------------------------------

using Avalonia;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Photon.App.Logging;

namespace Photon.App;

/// <summary>
/// Entry point of the desktop application.
/// </summary>
internal static partial class Program
{
    /// <summary>
    /// The application-wide logger factory. Stays a <see cref="NullLoggerFactory"/>
    /// until <see cref="Main"/> runs (e.g. in the visual designer).
    /// </summary>
    internal static ILoggerFactory Loggers { get; private set; } = NullLoggerFactory.Instance;

    /// <summary>
    /// Starts the Avalonia application with the classic desktop lifetime.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    [STAThread]
    public static void Main(string[] args)
    {
        using var loggerFactory = LoggingSetup.Create();
        Loggers = loggerFactory;
        var logger = loggerFactory.CreateLogger("Photon.App.Program");
        LogStarting(logger);

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception exception)
        {
            // log unhandled exceptions centrally.
            LogUnhandledException(logger, exception);
            throw;
        }
        finally
        {
            LogStopped(logger);
        }
    }

    /// <summary>Logs the application start.</summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Photon starting")]
    private static partial void LogStarting(ILogger logger);

    /// <summary>Logs the application stop.</summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Photon stopped")]
    private static partial void LogStopped(ILogger logger);

    /// <summary>Logs an unhandled exception.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The unhandled exception.</param>
    [LoggerMessage(Level = LogLevel.Critical, Message = "Unhandled exception")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception);

    /// <summary>
    /// Configures the Avalonia application and selects OpenGL on macOS.
    /// Also used by the visual designer.
    /// </summary>
    /// <returns>The configured application builder.</returns>
    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>().UsePlatformDetect();

        if (OperatingSystem.IsMacOS())
        {
            builder.With(
                new AvaloniaNativePlatformOptions
                {
                    RenderingMode = new[] { AvaloniaNativeRenderingMode.OpenGl },
                }
            );
        }

        return builder.LogToTrace();
    }
}
