// -----------------------------------------------------------------------------
// Author:        Jondahl Rhenius, Florian Hipp
// AI Assistance: Autocompletion, Copilot for Information, Claude for Information and Bug
// -----------------------------------------------------------------------------

using System.Diagnostics;
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
    /// <summary>
    /// Calculates the color at each visible surface point.
    /// </summary>
    private readonly ILightingModel _lightingModel;

    /// <summary>
    /// Initializes a new ray tracer that lights surfaces with <see cref="PhongLighting"/>.
    /// </summary>
    public RayTracer()
        : this(new PhongLighting()) { }

    /// <summary>
    /// Initializes a new ray tracer with the given lighting model.
    /// </summary>
    /// <param name="lightingModel">Calculates the color at each visible surface point.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="lightingModel"/> is null.</exception>
    public RayTracer(ILightingModel lightingModel)
    {
        ArgumentNullException.ThrowIfNull(lightingModel);
        _lightingModel = lightingModel;
    }

    /// <inheritdoc/>
    public RenderStatistics Render(
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

        var bvhStopwatch = Stopwatch.StartNew();
        var accelerationStructure = new Bvh(scene);
        bvhStopwatch.Stop();

        var renderStopwatch = Stopwatch.StartNew();

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

                Vector3 color = _lightingModel.Shade(
                    ray,
                    closestHitInfo,
                    scene.Lighting,
                    accelerationStructure
                );
                target.SetPixel(x, y, color);
            }

            target.MarkRowCompleted(y);
        }

        return new RenderStatistics(
            target.Width,
            target.Height,
            scene.Model.TriangleCount,
            bvhStopwatch.Elapsed,
            renderStopwatch.Elapsed,
            (long)target.Width * target.Height
        );
    }
}
