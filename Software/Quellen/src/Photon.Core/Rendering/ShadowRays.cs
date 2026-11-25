// -----------------------------------------------------------------------------
// Author:        Luca Lengl
// AI Assistance: Claude for information, bugs and warnings
// -----------------------------------------------------------------------------
using System.Numerics;
using Photon.Core.Mathematics;
using Photon.Core.Scenes;

namespace Photon.Core.Rendering;

/// <summary>
/// Shadow rays: checks whether a light source reaches a surface point.
/// </summary>
public static class ShadowRays
{
    /// <summary>
    /// The shadow ray offset relative to the largest coordinate of the surface point.
    /// </summary>
    private const float RelativeOffset = 1e-4f;

    /// <summary>
    /// Checks whether the way from a surface point to a light source is free.
    /// Lights that are blocked must not contribute to the lighting of the point.
    /// </summary>
    /// <param name="structure">The acceleration structure with all primitives of the scene.</param>
    /// <param name="hit">The surface point that is lit.</param>
    /// <param name="light">The light that arrives at the point, from <see cref="ILightSource.Illuminate"/>.</param>
    /// <returns><c>true</c> if no primitive lies between the point and the light; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="structure"/> is null.</exception>
    public static bool IsLightVisible(
        IAccelerationStructure structure,
        in HitInfo hit,
        in LightSample light
    )
    {
        ArgumentNullException.ThrowIfNull(structure);

        // A point at the light position has no usable direction to the light.
        if (light.Distance <= FloatComparison.Epsilon)
        {
            return false;
        }

        // The geometric normal depends on the winding order and can point away from the light.
        // The offset must go to the side of the light, otherwise the shadow ray starts below
        // the surface and hits its own triangle (ray acne).
        Vector3 normal =
            Vector3.Dot(hit.GeometricNormal, light.Direction) >= 0f
                ? hit.GeometricNormal
                : -hit.GeometricNormal;
        float offset = GetOffset(hit.Point);
        Ray shadowRay = Ray.CreateOffset(hit.Point, normal, light.Direction, offset);

        // Only primitives between the point and the light cast a shadow, not those behind it.
        return !structure.IsOccluded(shadowRay, FloatComparison.Epsilon, light.Distance - offset);
    }

    /// <summary>
    /// Calculates how far a shadow ray starts above the surface.
    /// </summary>
    /// <param name="point">The surface point the shadow ray starts from.</param>
    /// <returns>The offset along the normal; at least <see cref="FloatComparison.Epsilon"/>.</returns>
    private static float GetOffset(Vector3 point)
    {
        // A float has about 7 significant digits, so the rounding error of a hit point grows
        // with the size of its coordinates: at 1000 units it is already about 1e-4. A fixed
        // offset of Epsilon then ends below the surface and the shadow ray hits its own
        // triangle (shadow acne). Scaling the offset with the coordinates keeps it above.
        float largestCoordinate = MathF.Max(
            MathF.Abs(point.X),
            MathF.Max(MathF.Abs(point.Y), MathF.Abs(point.Z))
        );
        return MathF.Max(FloatComparison.Epsilon, RelativeOffset * largestCoordinate);
    }
}
