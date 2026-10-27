// -----------------------------------------------------------------------------
// Author:        XISander, BrackyDayn
// AI Assistance: Claude for project skeleton, ChatGpt for MainWindow
// -----------------------------------------------------------------------------

using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Photon.Application.ViewModels;

namespace Photon.App.Views;

/// <summary>
/// The main window of the application.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the MainWindow class.
    /// </summary>
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);

        var viewModel = new MainWindowViewModel();
        DataContext = viewModel;

        var preview =
            this.FindControl<TrianglePreviewControl>("Preview")
            ?? throw new InvalidOperationException("The triangle preview control was not found.");

        preview.Geometry = viewModel.PreviewTriangle;
    }
}
