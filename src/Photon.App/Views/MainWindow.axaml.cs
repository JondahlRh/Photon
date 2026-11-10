// -----------------------------------------------------------------------------
// Author:        XISander, BrackyDayn
// AI Assistance: Claude for project skeleton, ChatGpt Codex for help with understanding.
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Photon.Application.ViewModels;
using Photon.Core.Scenes;

namespace Photon.App.Views;

/// <summary>
/// The main window of the application.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Control displaying the imported model.
    /// </summary>
    private readonly ModelPreviewControl _preview;

    /// <summary>
    /// Initializes a new instance of the MainWindow class.
    /// </summary>
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);

        DataContext = new MainWindowViewModel();

        _preview =
            this.FindControl<ModelPreviewControl>("Preview")
            ?? throw new InvalidOperationException("The model preview control was not found.");
    }

    /// <summary>
    /// Opens an OBJ file picker and displays the imported model.
    /// </summary>
    /// <param name="sender">The import button.</param>
    /// <param name="e">The button click event arguments.</param>
    private async void ImportModelClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        button.IsEnabled = false;

        try
        {
            if (!StorageProvider.CanOpen)
            {
                throw new NotSupportedException("Opening files is unavailable on this platform.");
            }

            FilePickerOpenOptions options = new FilePickerOpenOptions
            {
                Title = "Import an OBJ model",
                AllowMultiple = false,
                FileTypeFilter = new FilePickerFileType[]
                {
                    new FilePickerFileType("Wavefront OBJ")
                    {
                        Patterns = new string[] { "*.obj", "*.OBJ" },
                    },
                },
            };

            IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(options);

            if (files.Count == 0)
            {
                return;
            }

            using IStorageFile file = files[0];

            string? path = file.TryGetLocalPath();

            if (path is null)
            {
                throw new NotSupportedException("Please select a file stored on your computer.");
            }

            Model model = await viewModel.LoadModelAsync(path);

            // The awaited operation resumes on the UI thread.
            _preview.Geometry = model;
        }
        catch (Exception exception)
            when (exception
                    is IOException
                        or UnauthorizedAccessException
                        or FormatException
                        or NotSupportedException
                        or ArgumentException
            )
        {
            viewModel.StatusText = $"Import failed: {exception.Message}";
        }
        finally
        {
            button.IsEnabled = true;
        }
    }
}
