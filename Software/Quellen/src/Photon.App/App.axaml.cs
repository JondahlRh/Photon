// -----------------------------------------------------------------------------
// Author:        XISander
// AI Assistance: Claude for project skeleton
// -----------------------------------------------------------------------------

using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Photon.App.Views;

namespace Photon.App;

/// <summary>
/// The Avalonia application. Loads the XAML resources and opens the main window.
/// </summary>
/// <remarks>
/// The base class is fully qualified because the <c>Photon.Application</c> namespace
/// would otherwise hide <see cref="Avalonia.Application"/>.
/// </remarks>
public partial class App : Avalonia.Application
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc/>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
