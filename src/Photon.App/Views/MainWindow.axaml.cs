// -----------------------------------------------------------------------------
// Author:        XISander
// AI Assistance: Claude for project skeleton
// -----------------------------------------------------------------------------

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Photon.App.Views;

/// <summary>
/// The main window of the application.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
