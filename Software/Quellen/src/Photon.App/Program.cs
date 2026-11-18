// -----------------------------------------------------------------------------
// Author:        XISander, BrackyDayn
// AI Assistance: Claude for project skeleton; ChatGPT for macOS OpenGL configuration and help with understanding OpenGL
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
