// -----------------------------------------------------------------------------
// Author:        BrackyDayn
// AI Assistance: ChatGpt for Triangle coordiantes
// -----------------------------------------------------------------------------
using System.Numerics;
using Photon.Core.Mathematics;

namespace Photon.Application.ViewModels;

/// <summary>
/// Provides data for the main window.
/// </summary>
public sealed class MainWindowViewModel
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
}
