// -----------------------------------------------------------------------------
// Author:        BrackyDayn, Florian Hipp, MJZgh
// AI Assistance: ChatGpt for Triangle coordiantes, Summary and Exeptions; Claude for logging documentation; ChatGPT Codex for merge conflict resolution; inspiration from https://docs.avaloniaui.net/docs/how-to/mvvm-how-to
// -----------------------------------------------------------------------------
using System;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Photon.Core.Imaging;
using Photon.Core.InputOutput;
using Photon.Core.Mathematics;
using Photon.Core.Rendering;
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
    /// The model most recently imported successfully.
    /// </summary>
    private Model? _loadedModel;

    /// <summary>The renderer used to generate images.</summary>
    private readonly RayTracer _renderer = new RayTracer();

    /// <summary>The most recent completed render.</summary>
    [ObservableProperty]
    private FrameBuffer? _renderResult;

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

        StatusText = "Importing model...";

        try
        {
            ImportResult result = await Task.Run(() => ObjMtlImporter.Load(path));
            Model model = result.Model;

                    _loadedModel = model;

            ModelPath = path;
            StatusText = $"Loaded {Path.GetFileName(path)}: {model.TriangleCount} triangles.";

            foreach (string warning in result.Warnings)
            {
                LogLoadedModelWarning(warning);
            }

            LogLoadedModel(path, model.TriangleCount);

            return model;
        }
        catch (Exception exception)
        {
            StatusText = $"Import failed: {exception.Message}";
            LogLoadingModelException(exception.Message);
            throw;
        }
    }

    /// <summary>
    /// Creates a scene using the imported model and a fixed camera.
    /// </summary>
    /// <returns>The scene to render.</returns>
    private Scene CreateRenderScene()
    {
        Model model =
            _loadedModel ?? throw new InvalidOperationException("Import model before rendering");

        int width = RenderWidth;
        int height = RenderHeight;

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        Matrix4x4 viewToWorld =
            Matrix4x4.CreateScale(4f / height)
            * Matrix4x4.CreateTranslation(0f, 0f, 5f)
            * Matrix4x4.CreateRotationX(-MathF.PI / 6f)
            * Matrix4x4.CreateRotationY(MathF.PI / 4f);

        var camera = new Camera(viewToWorld, width, height, new Vector3(0f, 0f, height));

        var lighting = new Lighting(Vector3.Zero, []);

        return new Scene(model, lighting, camera);
    }

    /// <summary>Renders the imported model without blocking the UI.</summary>
    [RelayCommand]
    private async Task StartRenderAsync()
    {
        if (_loadedModel is null)
        {
            StatusText = "Import a model before rendering.";
            return;
        }

        HasRenderResult = false;
        IsResultVisible = false;
        ProgressPercent = 0;
        StatusText = "Rendering...";
        LogSimpleMessage("Rendering...");

        try
        {
            Scene scene = CreateRenderScene();
            var target = new FrameBuffer(scene.Camera.Width, scene.Camera.Height);

            LogRenderStarted(RenderWidth, RenderHeight, SamplesPerPixel, MaxRecursionDepth);

            await Task.Run(() =>
                _renderer.Render(scene, RenderSettings.Default, target, CancellationToken.None)
            );

            RenderResult = target;
            ProgressPercent = 100;
            HasRenderResult = true;
            IsResultVisible = true;
            StatusText = "Render finished.";
            LogSimpleMessage("Render finished.");
        }
        catch (Exception exception)
            when (exception is ArgumentException or InvalidOperationException or OverflowException)
        {
            LogRenderFailed(exception);
            StatusText = $"Render failed: {exception.Message}";
        }
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

    /// <summary>Logs a failed render operation.</summary>
    /// <param name="exception">The exception that caused the failure.</param>
    [LoggerMessage(Level = LogLevel.Error, Message = "Rendering failed.")]
    private partial void LogRenderFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loading model {path}")]
    private partial void LogLoadingModelPath(string path);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Loaded {path}: {triangleCount} triangles."
    )]
    private partial void LogLoadedModel(string path, int triangleCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "{message}")]
    private partial void LogSimpleMessage(string message);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{warning}")]
    private partial void LogLoadedModelWarning(string warning);

    [LoggerMessage(Level = LogLevel.Error, Message = "{exception}")]
    private partial void LogLoadingModelException(string exception);
}
