// -----------------------------------------------------------------------------
// Author:        Jondahl Rhenius
// AI Assistance: Autocompletion, Copilot for Information
// -----------------------------------------------------------------------------

using System.Numerics;
using Photon.Core.Imaging;
using Photon.Core.Mathematics;
using Photon.Core.Scenes;

namespace Photon.Core.Rendering;

/// <summary>
/// A ray tracer that renders a scene into a buffer. This is the main entry point for rendering.
/// </summary>
public sealed class RayTracer : IRenderer
{
    /// <inheritdoc/>
    public void Render(
        Scene scene,
        RenderSettings settings,
        FrameBuffer target,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(target);

        if (scene.Camera.Width != target.Width || scene.Camera.Height != target.Height)
        {
            throw new ArgumentException(
                "The buffer size does not match the camera.",
                nameof(target)
            );
        }

        var accelerationStructure = new Bvh(scene);

        for (int y = 0; y < target.Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (int x = 0; x < target.Width; x++)
            {
                var ray = scene.Camera.CreatePrimaryRay(x, y);

                var foundHit = accelerationStructure.Intersect(
                    ray,
                    FloatComparison.Epsilon,
                    float.MaxValue,
                    out HitInfo closestHitInfo
                );
                if (!foundHit)
                {
                    target.SetPixel(x, y, settings.BackgroundColor);
                    continue;
                }

                // Remap the normalized normal from [-1, 1] to RGB values in [0, 1].
                var color = (closestHitInfo.ShadingNormal + Vector3.One) * 0.5f;
                target.SetPixel(x, y, color);
            }

            target.MarkRowCompleted(y);
        }
    }
}
