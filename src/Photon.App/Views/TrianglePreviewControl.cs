// -----------------------------------------------------------------------------
// Author:        BrackyDayn
// AI Assistance: ChatGpt for help with OnOpenGlInit and summary
// -----------------------------------------------------------------------------
using System;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using OpenTK.Graphics.ES30;
using Photon.App.Platform.OpenGL;
using Photon.Core.Mathematics;

namespace Photon.App.Views;

/// <summary>
/// Displays a triangle using OpenGL inside an Avalonia control.
/// </summary>
public sealed class TrianglePreviewControl : OpenGlControlBase
{
    /// <summary>
    /// Renderer responsible for drawing the triangle.
    /// </summary>
    private TriangleRenderer? _renderer;

    /// <summary>
    /// Gets or sets the triangle displayed by the preview.
    /// </summary>
    public Triangle Geometry { get; set; }

    /// <inheritdoc/>
    protected override void OnOpenGlInit(GlInterface gl)
    {
        // Load OpenGL functions using Avalonia's context.
        GL.LoadBindings(new AvaloniaGlBinding(gl));

        bool isOpenGlEs = GlVersion.Type == GlProfileType.OpenGLES;

        // The demo requires OpenGL ES 3.0 or desktop OpenGL 3.2+.
        if (GlVersion.Major < 3 || (!isOpenGlEs && GlVersion.Major == 3 && GlVersion.Minor < 2))
        {
            throw new NotSupportedException($"Unsupported OpenGL version: {GlVersion}");
        }

        _renderer = new TriangleRenderer();

        _renderer.Initialize(Geometry, isOpenGlEs);
    }

    /// <inheritdoc/>
    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        if (_renderer is null)
        {
            return;
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
    }

    /// <inheritdoc/>
    protected override void OnOpenGlLost()
    {
        // GPU resources are lost together with the context.
        _renderer = null;
    }
}
