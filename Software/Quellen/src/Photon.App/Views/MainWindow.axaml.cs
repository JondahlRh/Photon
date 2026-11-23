// -----------------------------------------------------------------------------
// Author:        XISander, BrackyDayn, MJZgh, Florian
// AI Assistance: Claude for project skeleton, ChatGpt Codex for help with understanding and merge conflict resolution, Claude for Information and Bugs
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using Photon.App.Imaging;
using Photon.App.Platform.Skia;
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

    /// <summary>The control displaying the completed render.</summary>
    private readonly Image _resultImage;

    /// <summary>
    /// Initializes a new instance of the MainWindow class.
    /// </summary>
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);

        var viewModel = new MainWindowViewModel(
            Program.Loggers.CreateLogger<MainWindowViewModel>(),
            new SkiaImageExporter()
        );

        DataContext = viewModel;

        _preview =
            this.FindControl<ModelPreviewControl>("Preview")
            ?? throw new InvalidOperationException("The model preview control was not found.");

        _resultImage =
            this.FindControl<Image>("RenderResultImage")
            ?? throw new InvalidOperationException("The render result image was not found.");

        viewModel.PropertyChanged += ViewModelPropertyChanged;
    }

    /// <summary>
    /// Displays the image when a new render result is available.
    /// </summary>
    /// <param name="sender">The ViewModel reporting the change.</param>
    /// <param name="e">The changed property.</param>
    private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (
            e.PropertyName != nameof(MainWindowViewModel.RenderResult)
            || sender is not MainWindowViewModel viewModel
        )
        {
            return;
        }
        var buffer = viewModel.RenderResult;
        var bitmap = buffer is null ? null : FrameBufferBitmap.Create(buffer);
        var previous = _resultImage.Source as WriteableBitmap;

        _resultImage.Source = bitmap;

        previous?.Dispose();
    }

    /// <inheritdoc/>
    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.PropertyChanged -= ViewModelPropertyChanged;
        }

        var bitmap = _resultImage.Source as WriteableBitmap;
        _resultImage.Source = null;
        bitmap?.Dispose();

        base.OnClosed(e);
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

    /// <summary>
    /// Opens a save dialog and saves the render result as PNG
    /// </summary>
    /// <param name="sender">The save button.</param>
    /// <param name="e">The button click event arguments.</param>
    private async void SavePngClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        button.IsEnabled = false;

        try
        {
            if (!StorageProvider.CanSave)
            {
                throw new NotSupportedException("Saving files is unavailable on this platform.");
            }

            FilePickerSaveOptions options = new FilePickerSaveOptions
            {
                Title = "Save render result as PNG",
                SuggestedFileName = "render.png",
                DefaultExtension = "png",
                FileTypeChoices = new FilePickerFileType[]
                {
                    new FilePickerFileType("PNG image") { Patterns = new string[] { "*.png" } },
                },
            };

            IStorageFile? file = await StorageProvider.SaveFilePickerAsync(options);

            if (file is null)
            {
                return; // user cancelled
            }

            using (file)
            {
                string? path = file.TryGetLocalPath();

                if (path is null)
                {
                    throw new NotSupportedException("Please select a location on your computer.");
                }

                if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    path += ".png";
                }

                await viewModel.SavePngAsync(path);
            }
        }
        catch (Exception exception)
            when (exception
                    is IOException
                        or UnauthorizedAccessException
                        or NotSupportedException
                        or InvalidOperationException
                        or ArgumentException
            )
        {
            viewModel.StatusText = $"Save failed: {exception.Message}";
        }
        finally
        {
            button.IsEnabled = true;
        }
    }
}
