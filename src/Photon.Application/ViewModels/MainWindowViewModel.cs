// -----------------------------------------------------------------------------
// Author:        BrackyDayn, Florian Hipp, MJZgh
// AI Assistance: ChatGpt for Triangle coordiantes; Claude for logging documentation
// -----------------------------------------------------------------------------
using System.Numerics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Photon.Core.Mathematics;

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

    /// <summary>Starts the rendering process</summary>
    [RelayCommand]
    private void StartRender()
    {
        LogRenderStarted(RenderWidth, RenderHeight, SamplesPerPixel, MaxRecursionDepth);
        StatusText = "Rendering...";
        ProgressPercent = 50;
    }

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
}
