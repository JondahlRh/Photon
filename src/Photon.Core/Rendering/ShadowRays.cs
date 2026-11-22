// --------------------------------------------------------------------------
// Author: Luca Lengl
// AI Assistance: Claude for information, bugs and warnings
// --------------------------------------------------------------------------

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
        Ray shadowRay = Ray.CreateOffset(hit.Point, normal, light.Direction);

        // Only primitives between the point and the light cast a shadow, not those behind it.
        return !structure.IsOccluded(
            shadowRay,
            FloatComparison.Epsilon,
            light.Distance - FloatComparison.Epsilon
        );
    }
}
