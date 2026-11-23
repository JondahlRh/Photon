// -----------------------------------------------------------------------------
// Author:        BrackyDayn
// AI Assistance: -
// -----------------------------------------------------------------------------
using System;
using Avalonia.OpenGL;
using OpenTK;

namespace Photon.App.Platform.OpenGL;

/// <summary>
/// Provides OpenTK with OpenGL functions from Avalonia's context.
/// </summary>
internal sealed class AvaloniaGlBinding : IBindingsContext
{
    /// <summary>
    /// Avalonia's OpenGL interface.
    /// </summary>
    private readonly GlInterface _gl;

    /// <summary>
    /// Creates a binding context using Avalonia's OpenGL interface.
    /// </summary>
    /// <param name="gl">The Avalonia OpenGL interface.</param>
    public AvaloniaGlBinding(GlInterface gl)
    {
        _gl = gl;
    }

    /// <inheritdoc/>
    public IntPtr GetProcAddress(string procName)
    {
        return _gl.GetProcAddress(procName);
    }
}
