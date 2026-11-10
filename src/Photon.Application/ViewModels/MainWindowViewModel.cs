// -----------------------------------------------------------------------------
// Author:        BrackyDayn, Florian Hipp
// AI Assistance: ChatGpt for Triangle coordiantes
// -----------------------------------------------------------------------------
using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Photon.Core.InputOutput;
using Photon.Core.Mathematics;
using Photon.Core.Scenes;

namespace Photon.Application.ViewModels;

/// <summary>
/// Provides data for the main window
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

    /// <summary>Camera position X</summary>
    [ObservableProperty]
    private float _cameraPosX = 0f;

    /// <summary>Camera position Y</summary>
    [ObservableProperty]
    private float _cameraPosY = 2f;

    /// <summary>Camera position Z</summary>
    [ObservableProperty]
    private float _cameraPosZ = 5f;

    /// <summary>Target position X</summary>
    [ObservableProperty]
    private float _targetPosX = 0f;

    /// <summary>Target position Y</summary>
    [ObservableProperty]
    private float _targetPosY = 0f;

    /// <summary>Target position Z</summary>
    [ObservableProperty]
    private float _targetPosZ = 0f;

    /// <summary>Up vector X</summary>
    [ObservableProperty]
    private float _upPosX = 0f;

    /// <summary>Up vector Y</summary>
    [ObservableProperty]
    private float _upPosY = 1f;

    /// <summary>Up vector Z</summary>
    [ObservableProperty]
    private float _upPosZ = 0f;

    /// <summary>Field of view in degrees</summary>
    [ObservableProperty]
    private float _fieldOfView = 60f;

    /// <summary>Render target width in pixels</summary>
    [ObservableProperty]
    private int _renderWidth = 1280;

    /// <summary>Render target height in pixels</summary>
    [ObservableProperty]
    private int _renderHeight = 720;

    /// <summary>Number of samples per pixel</summary>
    [ObservableProperty]
    private int _samplesPerPixel = 16;

    /// <summary>Maximum ray recursion depth</summary>
    [ObservableProperty]
    private int _maxRecursionDepth = 5;

    /// <summary>Status bar text</summary>
    [ObservableProperty]
    private string _statusText = "Ready";

    /// <summary>Render Progress</summary>
    [ObservableProperty]
    private double _progressPercent;

    /// <summary>
    /// Path of the model most recently imported successfully.
    /// </summary>
    [ObservableProperty]
    private string _modelPath = string.Empty;

    /// <summary>
    /// Imports a model without blocking the UI while parsing.
    /// </summary>
    /// <param name="path">The selected OBJ file path.</param>
    /// <returns>The imported model.</returns>
    public async Task<Model> LoadModelAsync(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        StatusText = "Importing model...";

        Model model = await Task.Run(() => ObjMtlImporter.Load(path));

        ModelPath = path;
        StatusText = $"Loaded {Path.GetFileName(path)}: {model.TriangleCount} triangles.";

        return model;
    }

    /// <summary>Starts the rendering process</summary>
    [RelayCommand]
    private void StartRender()
    {
        StatusText = "Rendering...";
        ProgressPercent = 50;
    }
}
