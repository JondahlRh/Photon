// -----------------------------------------------------------------------------
// Author:        BrackyDayn, Florian Hipp
// AI Assistance: ChatGpt for Triangle coordiantes
// -----------------------------------------------------------------------------
using System.Numerics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Photon.Core.Mathematics;

namespace Photon.Application.ViewModels;

/// <summary>
/// Provides data for the main window.
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    /// <summary>
    /// Triangle for opengl preview
    /// </summary>
    public Triangle PreviewTriangle { get; } =
        new Triangle(
            new Vector3(-0.6f, -0.5f, 0f),
            new Vector3(0.6f, -0.5f, 0f),
            new Vector3(0f, 0.6f, 0f)
        );

    /// <summary>Camera position X coordinate.</summary>
    [ObservableProperty]
    private float _cameraPosX = 0f;

    /// <summary>Camera position Y coordinate.</summary>
    [ObservableProperty]
    private float _cameraPosY = 2f;

    /// <summary>Camera position Z coordinate.</summary>
    [ObservableProperty]
    private float _cameraPosZ = 5f;

    /// <summary>Target position X coordinate.</summary>
    [ObservableProperty]
    private float _targetPosX = 0f;

    /// <summary>Target position Y coordinate.</summary>
    [ObservableProperty]
    private float _targetPosY = 0f;

    /// <summary>Target position Z coordinate.</summary>
    [ObservableProperty]
    private float _targetPosZ = 0f;

    /// <summary>Up vector X coordinate.</summary>
    [ObservableProperty]
    private float _upPosX = 0f;

    /// <summary>Up vector Y coordinate.</summary>
    [ObservableProperty]
    private float _upPosY = 1f;

    /// <summary>Up vector Z coordinate.</summary>
    [ObservableProperty]
    private float _upPosZ = 0f;

    /// <summary>Field of view in degrees.</summary>
    [ObservableProperty]
    private float _fieldOfView = 60f;

    /// <summary>Render target width in pixels.</summary>
    [ObservableProperty]
    private int _renderWidth = 1280;

    /// <summary>Render target height in pixels.</summary>
    [ObservableProperty]
    private int _renderHeight = 720;

    /// <summary>Number of samples per pixel.</summary>
    [ObservableProperty]
    private int _samplesPerPixel = 16;

    /// <summary>Maximum ray recursion depth.</summary>
    [ObservableProperty]
    private int _maxRecursionDepth = 5;

    /// <summary>
    /// The text shown in the status bar.
    /// </summary>
    [ObservableProperty]
    private string _statusText = "Ready";

    /// <summary>
    /// The render progress from 0 to 100.
    /// </summary>
    [ObservableProperty]
    private double _progressPercent;

    /// <summary>
    /// Starts the render. For now this only updates the status.
    /// </summary>
    [RelayCommand]
    private void StartRender()
    {
        StatusText = "Rendering...";
        ProgressPercent = 50;
    }
}
