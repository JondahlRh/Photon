// -----------------------------------------------------------------------------
// Author:        Luca Lengl
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------

using Photon.Core.Mathematics;

namespace Photon.Core.Scenes;

/// <summary>
/// Common interface for everything a ray can hit, e.g. triangles and later spheres.
/// </summary>
public interface IPrimitive
{
    /// <summary>
    /// Gets the axis-aligned box that fully contains the primitive.
    /// </summary>
    Aabb Bounds { get; }

    /// <summary>
    /// Calculates the nearest intersection of a ray with the primitive within a distance range.
    /// </summary>
    /// <param name="ray">The ray in world coordinates.</param>
    /// <param name="minDistance">The smallest accepted distance; should be at least <see cref="FloatComparison.Epsilon"/>.</param>
    /// <param name="maxDistance">The largest accepted distance, e.g. the nearest hit found so far.</param>
    /// <param name="hit">The hit description if an intersection was found; otherwise, the default value.</param>
    /// <returns><c>true</c> if the ray hits the primitive within the range; otherwise, <c>false</c>.</returns>
    bool Intersect(in Ray ray, float minDistance, float maxDistance, out HitInfo hit);
}
