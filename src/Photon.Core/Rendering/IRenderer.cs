// -----------------------------------------------------------------------------
// Author:        Jondahl Rhenius, Luca Lengl
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------

using Photon.Core.Imaging;
using Photon.Core.Scenes;

namespace Photon.Core.Rendering;

/// <summary>
/// Computes an image of a scene. Implementations are synchronous and may work in parallel internally.
/// </summary>
public interface IRenderer
{
    /// <summary>
    /// Renders the scene into the target buffer and marks each row as completed when it is done.
    /// </summary>
    /// <param name="scene">The immutable scene to render.</param>
    /// <param name="settings">The immutable render settings.</param>
    /// <param name="target">The buffer that receives the image; its size must match the camera.</param>
    /// <param name="cancellationToken">Checked before each row.</param>
    /// <exception cref="ArgumentException">The buffer size does not match the camera.</exception>
    /// <exception cref="OperationCanceledException">The render was cancelled; finished rows stay in the buffer.</exception>
    void Render(Scene scene, RenderSettings settings, FrameBuffer target, CancellationToken cancellationToken);
}
