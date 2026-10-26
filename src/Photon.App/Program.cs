// -----------------------------------------------------------------------------
// Author:        XISander
// AI Assistance: Claude for project skeleton
// -----------------------------------------------------------------------------

using Avalonia;

namespace Photon.App;

/// <summary>
/// Entry point of the desktop application.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Starts the Avalonia application with the classic desktop lifetime.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// Configures the Avalonia application. Also used by the visual designer.
    /// </summary>
    /// <returns>The configured application builder.</returns>
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
    }
}
