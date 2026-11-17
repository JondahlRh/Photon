// -----------------------------------------------------------------------------
// Author:        BrackyDayn, Florian Hipp, MJZgh
// AI Assistance: ChatGpt for Triangle coordiantes, Summary and Exeptions; Claude for logging documentation; ChatGPT Codex for merge conflict resolution; inspiration from https://docs.avaloniaui.net/docs/how-to/mvvm-how-to
// -----------------------------------------------------------------------------
using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Photon.Core.InputOutput;
using Photon.Core.Mathematics;
using Photon.Core.Scenes;

namespace Photon.Application.ViewModels;

/// <summary>
/// Provides data for the main window
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    /// <summary>The logger of this view model.</summary>
    private readonly ILogger<MainWindowViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class without a logger.
    /// </summary>
    public MainWindowViewModel()
        : this(null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="logger">The logger; <see cref="NullLogger{T}"/> is used when <c>null</c>.</param>
    public MainWindowViewModel(ILogger<MainWindowViewModel>? logger)
    {
        _logger = logger ?? NullLogger<MainWindowViewModel>.Instance;
    }

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

    /// <summary>True wenn die Render-Ansicht (Vollbild, ohne Seitenleiste) angezeigt wird.</summary>
    [ObservableProperty]
    private bool _isResultVisible;

    /// <summary>True sobald ein Renderergebnis existiert (steuert "Show Result").</summary>
    [ObservableProperty]
    private bool _hasRenderResult;

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
        LogLoadingModelPath(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        //StatusText = "Importing model...";

        Model model = await Task.Run(() => ObjMtlImporter.Load(path));

        ModelPath = path;
        //StatusText = $"Loaded {Path.GetFileName(path)}: {model.TriangleCount} triangles.";
        LogLoadedModel(path, model.TriangleCount);

        return model;
    }

    /// <summary>Starts the rendering process (test: simulates progress).</summary>
    [RelayCommand]
    private async Task StartRenderAsync()
    {
        LogRenderStarted(RenderWidth, RenderHeight, SamplesPerPixel, MaxRecursionDepth);

        HasRenderResult = false;
        IsResultVisible = false;
        ProgressPercent = 0;
        StatusText = "Rendering...";

        for (int i = 1; i <= 100; i++)
        {
            ProgressPercent = i;
            await Task.Delay(30); // ca. 3 Sekunden insgesamt
        }

        StatusText = "Render finished.";
        HasRenderResult = true;
        IsResultVisible = true;
    }

    /// <summary>Switches from the result view back to the scene editor.</summary>
    [RelayCommand]
    private void EditScene() => IsResultVisible = false;

    /// <summary>Switches to the last render result.</summary>
    [RelayCommand]
    private void ShowResult() => IsResultVisible = true;

    /// <summary>Logs the start of a render run.</summary>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="samples">The samples per pixel.</param>
    /// <param name="depth">The maximum recursion depth.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Render started: {Width}x{Height}, {Samples} samples, max depth {Depth}"
    )]
    private partial void LogRenderStarted(int width, int height, int samples, int depth);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loading model {path}")]
    private partial void LogLoadingModelPath(String path);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Loaded {path}: {triangleCount} triangles."
    )]
    private partial void LogLoadedModel(String path, int triangleCount);
}
