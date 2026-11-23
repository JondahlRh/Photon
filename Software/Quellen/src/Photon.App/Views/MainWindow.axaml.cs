// -----------------------------------------------------------------------------
// Author:        XISander, BrackyDayn, MJZgh
// AI Assistance: Claude for project skeleton, ChatGpt Codex for help with understanding, merge conflict resolution and free camera input.
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using Photon.App.Imaging;
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

    /// <summary>The previous pointer position during camera dragging.</summary>
    private Point? _lastCameraPointerPosition;

    /// <summary>The camera rotation in degrees per pointer movement unit.</summary>
    private const double CameraDegreesPerPixel = 0.4;

    /// <summary>
    /// Initializes a new instance of the MainWindow class.
    /// </summary>
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);

        var viewModel = new MainWindowViewModel(
            Program.Loggers.CreateLogger<MainWindowViewModel>()
        );

        DataContext = viewModel;

        _preview =
            this.FindControl<ModelPreviewControl>("Preview")
            ?? throw new InvalidOperationException("The model preview control was not found.");

        _resultImage =
            this.FindControl<Image>("RenderResultImage")
            ?? throw new InvalidOperationException("The render result image was not found.");

        viewModel.PropertyChanged += ViewModelPropertyChanged;

        _preview.UpdateCamera(viewModel.CameraOrientation, (float)viewModel.CameraZoom);

        _preview.UpdateLight(viewModel.PreviewLight);
    }

    /// <summary>
    /// Updates the preview camera or displays a newly completed render.
    /// </summary>
    /// <param name="sender">The ViewModel reporting the change.</param>
    /// <param name="e">The changed property.</param>
    private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (
            e.PropertyName
            is nameof(MainWindowViewModel.CameraOrientation)
                or nameof(MainWindowViewModel.CameraZoom)
        )
        {
            _preview.UpdateCamera(viewModel.CameraOrientation, (float)viewModel.CameraZoom);
            return;
        }

        if (e.PropertyName == nameof(MainWindowViewModel.PreviewLight))
        {
            _preview.UpdateLight(viewModel.PreviewLight);
            return;
        }

        if (e.PropertyName != nameof(MainWindowViewModel.RenderResult))
        {
            return;
        }

        var buffer = viewModel.RenderResult;
        var bitmap = buffer is null ? null : FrameBufferBitmap.Create(buffer);
        var previous = _resultImage.Source as WriteableBitmap;

        _resultImage.Source = bitmap;
        previous?.Dispose();
    }

    /// <summary>Starts camera dragging with the right mouse button.</summary>
    /// <param name="sender">The preview control.</param>
    /// <param name="e">The pointer event arguments.</param>
    private void PreviewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(_preview).Properties.IsRightButtonPressed)
        {
            return;
        }

        _lastCameraPointerPosition = e.GetPosition(_preview);
        e.Pointer.Capture(_preview);
        e.Handled = true;
    }

    /// <summary>Orbits the camera while dragging, or rolls it when Shift is held.</summary>
    /// <param name="sender">The preview control.</param>
    /// <param name="e">The pointer event arguments.</param>
    private void PreviewPointerMoved(object? sender, PointerEventArgs e)
    {
        if (
            _lastCameraPointerPosition is not Point previous
            || DataContext is not MainWindowViewModel viewModel
        )
        {
            return;
        }
        Point current = e.GetPosition(_preview);
        Vector delta = current - previous;
        _lastCameraPointerPosition = current;

        float horizontal = (float)(delta.X * CameraDegreesPerPixel);
        float vertical = (float)(delta.Y * CameraDegreesPerPixel);

        if ((e.KeyModifiers & KeyModifiers.Shift) != 0)
        {
            viewModel.RotateCamera(0f, 0f, horizontal);
        }
        else
        {
            viewModel.RotateCamera(horizontal, vertical);
        }
        e.Handled = true;
    }

    /// <summary>Stops dragging when the right mouse button is released.</summary>
    /// <param name="sender">The preview control.</param>
    /// <param name="e">The pointer event arguments.</param>
    private void PreviewPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (
            _lastCameraPointerPosition is null
            || e.GetCurrentPoint(_preview).Properties.IsRightButtonPressed
        )
        {
            return;
        }
        _lastCameraPointerPosition = null;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    /// <summary>Clears the drag state when pointer capture is lost.</summary>
    /// <param name="sender">The preview control.</param>
    /// <param name="e">The pointer capture event arguments.</param>
    private void PreviewPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _lastCameraPointerPosition = null;
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
    /// Changes the camera zoom when scrolling over the preview.
    /// </summary>
    /// <param name="sender">The preview control.</param>
    /// <param name="e">The pointer wheel event arguments.</param>
    private void PreviewPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        double factor = Math.Pow(1.1, e.Delta.Y);

        viewModel.CameraZoom = Math.Clamp(viewModel.CameraZoom * factor, 0.1, 2.0);

        e.Handled = true;
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
