// -----------------------------------------------------------------------------
// Author:        BrackyDayn
// AI Assistance: ChatGpt Codex for summary, Exeptions and free camera orientation, inspiration from https://opentk.net/learn/chapter1/2-hello-triangle.html?tabs=onload-opentk4%2Conrender-opentk4%2Cresize-opentk4
// -----------------------------------------------------------------------------
using System;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Rendering;
using OpenTK.Graphics.ES30;
using Photon.App.Platform.OpenGL;
using Photon.Core.Scenes;

namespace Photon.App.Views;

/// <summary>
/// Displays a model using OpenGL inside an Avalonia control.
/// </summary>
public sealed class ModelPreviewControl : OpenGlControlBase, ICustomHitTest
{
    /// <summary>
    /// Renderer responsible for drawing the model.
    /// </summary>
    private TriangleRenderer? _renderer;

    /// <summary>The current camera rotation.</summary>
    private Quaternion _cameraOrientation = Quaternion.Identity;

    /// <summary>The current point light, or null when none is active.</summary>
    private PointLight? _light;

    /// <summary>The current camera zoom factor.</summary>
    private float _cameraZoom = 1f;

    /// <summary>
    /// The model currently assigned to the preview.
    /// </summary>
    private Model _model = new Model(Array.Empty<Mesh>());

    /// <summary>
    /// Whether the model needs uploading during the next render callback.
    /// </summary>
    private bool _modelNeedsUpload = true;

    /// <summary>
    /// Gets or sets the displayed model. Assign this property on the UI thread.
    /// </summary>
    public Model Geometry
    {
        get { return _model; }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _model = value;
            _modelNeedsUpload = true;
            RequestNextFrameRendering();
        }
    }

    /// <inheritdoc/>
    protected override void OnOpenGlInit(GlInterface gl)
    {
        GL.LoadBindings(new AvaloniaGlBinding(gl));

        bool isOpenGlEs = GlVersion.Type == GlProfileType.OpenGLES;

        if (GlVersion.Major < 3 || (!isOpenGlEs && GlVersion.Major == 3 && GlVersion.Minor < 2))
        {
            throw new NotSupportedException($"Unsupported OpenGL version: {GlVersion}");
        }

        _renderer = new TriangleRenderer();
        _renderer.Initialize(isOpenGlEs);

        _modelNeedsUpload = true;
    }

    /// <inheritdoc/>
    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        if (_renderer is null)
        {
            return;
        }

        // Upload only while Avalonia has made the OpenGL context current.
        if (_modelNeedsUpload)
        {
            _renderer.UploadModel(_model);
            _modelNeedsUpload = false;
        }

        // Convert logical Avalonia dimensions to physical pixels.
        double scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;

        int width = Math.Max(1, (int)(Bounds.Width * scaling));
        int height = Math.Max(1, (int)(Bounds.Height * scaling));

        _renderer.Render(fb, width, height, _cameraOrientation, _cameraZoom, _light);
    }

    /// <inheritdoc/>
    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        _renderer?.Deinitialize();
        _renderer = null;
        _modelNeedsUpload = true;
    }

    /// <inheritdoc/>
    protected override void OnOpenGlLost()
    {
        // GPU resources are already invalid after the context is lost.
        _renderer = null;
        _modelNeedsUpload = true;
    }

    /// <summary>
    /// Updates the camera orientation and requests a preview frame.
    /// <param name="orientation">The camera rotation.</param>
    /// <param name="zoom">The camera zoom factor.</param>
    /// </summary>
    public void UpdateCamera(Quaternion orientation, float zoom)
    {
        _cameraOrientation = orientation;
        _cameraZoom = zoom;
        RequestNextFrameRendering();
    }

    /// <summary>
    /// Updates the point light and requests a preview frame.
    /// </summary>
    /// <param name="light">The point light, or null to remove it.</param>
    public void UpdateLight(PointLight? light)
    {
        _light = light;
        RequestNextFrameRendering();
    }

    /// <summary>
    /// Accepts pointer input within the preview area.
    /// </summary>
    /// <param name="point">The pointer position in local coordinates.</param>
    /// <returns>Whether the point lies inside the preview.</returns>
    public bool HitTest(Point point)
    {
        return new Rect(Bounds.Size).Contains(point);
    }
}
