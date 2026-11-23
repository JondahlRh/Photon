// -----------------------------------------------------------------------------
// Author:        BrackyDayn, Florian Hipp, MJZgh
// AI Assistance: ChatGpt for Triangle coordiantes, Summary and Exeptions; Claude for logging documentation; ChatGPT Codex for merge conflict resolution and free camera rotation; inspiration from https://docs.avaloniaui.net/docs/how-to/mvvm-how-to
// -----------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Drawing;
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

    /// <summary>The point light currently being edited.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLight))]
    private LightViewModel? _light;

    /// <summary>
    /// Gets whether a point light has been added.
    /// </summary>
    public bool HasLight => Light is not null;

    /// <summary>The latest valid point light for the preview.</summary>
    [ObservableProperty]
    private PointLight? _previewLight;

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

    /// <summary>
    /// Horizontal camera rotation around the model, in degrees.
    /// </summary>
    [ObservableProperty]
    private double _cameraYaw = 135.0;

    /// <summary>
    /// Vertical camera inclination, in degrees.
    /// </summary>
    [ObservableProperty]
    private double _cameraPitch = 35.26439;

    /// <summary>
    /// The camera orientation around the model.
    /// </summary>
    [ObservableProperty]
    private Quaternion _cameraOrientation = Quaternion.CreateFromRotationMatrix(
        Matrix4x4.CreateRotationX(-35.26439f * MathF.PI / 180f)
            * Matrix4x4.CreateRotationY(135f * MathF.PI / 180f)
    );

    /// <summary>
    /// The camera zoom factor; one represents the initial framing.
    /// </summary>
    [ObservableProperty]
    private double _cameraZoom = 1.0;

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
    /// Creates a scene with a camera fitted to the imported model's bounds.
    /// </summary>
    /// <returns>The scene to render.</returns>
    private Scene CreateRenderScene()
    {
        Model model =
            _loadedModel ?? throw new InvalidOperationException("Import model before rendering");

        var camera = Camera.FrameBounds(
            model.Bounds,
            RenderWidth,
            RenderHeight,
            CameraOrientation,
            (float)CameraZoom
        );

        PointLight? light = PreviewLight;
        ILightSource[] lights = light is null
            ? Array.Empty<ILightSource>()
            : new ILightSource[] { light };

        var lighting = new Lighting(new Vector3(0.15f), lights);

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

    /// <summary>
    /// Rotates the camera around its own axes.
    /// </summary>
    /// <param name="horizontalDegrees">The horizontal rotation.</param>
    /// <param name="verticalDegrees">The vertical rotation.</param>
    /// <param name="rollDegrees">The rotation around the viewing axis.</param>
    public void RotateCamera(float horizontalDegrees, float verticalDegrees, float rollDegrees = 0f)
    {
        float radians = MathF.PI / 180f;

        Matrix4x4 rotation =
            Matrix4x4.CreateRotationX(verticalDegrees * radians)
            * Matrix4x4.CreateRotationY(horizontalDegrees * radians)
            * Matrix4x4.CreateRotationZ(rollDegrees * radians);

        Quaternion change = Quaternion.CreateFromRotationMatrix(rotation);

        CameraOrientation = Quaternion.Normalize(CameraOrientation * change);
    }

    /// <summary>Switches from the result view back to the scene editor.</summary>
    [RelayCommand]
    private void EditScene() => IsResultVisible = false;

    /// <summary>
    /// Restores the initial camera orientation.
    /// </summary>
    [RelayCommand]
    private void ResetCamera()
    {
        CameraOrientation = Quaternion.CreateFromRotationMatrix(
            Matrix4x4.CreateRotationX(-35.26439f * MathF.PI / 180f)
                * Matrix4x4.CreateRotationY(135f * MathF.PI / 180f)
        );
        CameraZoom = 1.0;
    }

    /// <summary>
    /// Adds a white point light near the loaded model.
    /// </summary>
    [RelayCommand]
    private void AddLight()
    {
        if (_loadedModel is null)
        {
            StatusText = "Import a model before adding a light.";
            return;
        }

        if (HasLight)
        {
            return;
        }

        float radius = MathF.Max(0.01f, _loadedModel.Bounds.Size.Length() * 0.5f);

        Vector3 offset = Vector3.Transform(new Vector3(0.6f, 0.6f, 1f) * radius, CameraOrientation);

        Vector3 position = _loadedModel.Bounds.Center + offset;

        Light = new LightViewModel
        {
            PositionX = position.X,
            PositionY = position.Y,
            PositionZ = position.Z,
            Intensity = 1f,
        };

        StatusText = "Point light added.";
    }

    /// <summary>
    /// Removes the current point light.
    /// </summary>
    [RelayCommand]
    private void RemoveLight()
    {
        Light = null;
        StatusText = "Point light removed.";
    }

    /// <summary>
    /// Updates subscriptions when the edited light changes.
    /// </summary>
    /// <param name="oldValue">The previously edited light.</param>
    /// <param name="newValue">The newly edited light.</param>
    partial void OnLightChanged(LightViewModel? oldValue, LightViewModel? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.PropertyChanged -= LightPropertyChanged;
        }

        if (newValue is not null)
        {
            newValue.PropertyChanged += LightPropertyChanged;
        }

        UpdatePreviewLight();
    }

    /// <summary>
    /// Refreshes the preview light after an editor value changes.
    /// </summary>
    /// <param name="sender">The edited light.</param>
    /// <param name="e">The changed property.</param>
    private void LightPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdatePreviewLight();
    }

    /// <summary>
    /// Publishes valid light values or reports invalid input.
    /// </summary>
    private void UpdatePreviewLight()
    {
        try
        {
            PreviewLight = Light?.ToPointLight();

            if (Light is not null)
            {
                StatusText = "light updated.";
            }
        }
        catch (ArgumentException exception)
        {
            StatusText = $"Invalid light: {exception.Message}";
        }
    }

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
