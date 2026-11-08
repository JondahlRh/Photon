// -----------------------------------------------------------------------------
// Author:        BrackyDayn
// AI Assistance: Generated with AI assistance
// -----------------------------------------------------------------------------
using System;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using OpenTK.Graphics.ES30;
using Photon.App.Platform.OpenGL;
using Photon.Core.Geometry;

namespace Photon.App.Views;

/// <summary>
/// Displays a model using OpenGL inside an Avalonia control.
/// </summary>
public sealed class ModelPreviewControl : OpenGlControlBase
{
    /// <summary>
    /// Renderer responsible for drawing the model.
    /// </summary>
    private TriangleRenderer? _renderer;

    /// <summary>
    /// The model currently assigned to the preview.
    /// </summary>
    private Model _model;

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

        _renderer.Render(fb, width, height);
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
}
