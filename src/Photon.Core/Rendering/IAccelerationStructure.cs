// --------------------------------------------------------------------------
// Author: Luca Lengl
// AI Assistance: Claude for information, bugs and warnings
// --------------------------------------------------------------------------

using Photon.Core.Mathematics;
using Photon.Core.Scenes;

namespace Photon.Core.Rendering;

/// <summary>
/// Finds intersections of rays with all primitives of a scene without the caller
/// having to test every primitive itself, e.g. a bounding volume hierarchy.
/// </summary>
public interface IAccelerationStructure
{
    /// <summary>
    /// Finds the nearest intersection of a ray with any primitive within a distance range.
    /// </summary>
    /// <param name="ray">The ray in world coordinates.</param>
    /// <param name="minDistance">The smallest accepted distance; should be at least <see cref="FloatComparison.Epsilon"/>.</param>
    /// <param name="maxDistance">The largest accepted distance.</param>
    /// <param name="hit">The nearest hit if one was found; otherwise, the default value.</param>
    /// <returns><c>true</c> if the ray hits a primitive within the range; otherwise, <c>false</c>.</returns>
    bool FindClosestHit(in Ray ray, float minDistance, float maxDistance, out HitInfo hit);

    /// <summary>
    /// Checks whether any primitive blocks a ray within a distance range, e.g. for shadow rays.
    /// The search stops at the first hit because the nearest one is not needed.
    /// </summary>
    /// <param name="ray">The ray in world coordinates.</param>
    /// <param name="minDistance">The smallest accepted distance; should be at least <see cref="FloatComparison.Epsilon"/>.</param>
    /// <param name="maxDistance">The largest accepted distance, e.g. the distance to a light.</param>
    /// <returns><c>true</c> if any primitive lies within the range; otherwise, <c>false</c>.</returns>
    bool IsOccluded(in Ray ray, float minDistance, float maxDistance);
}
