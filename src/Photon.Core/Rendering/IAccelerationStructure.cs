// -----------------------------------------------------------------------------
// Author:        Jondahl Rhenius
// AI Assistance: Copilot for information & documentation
// -----------------------------------------------------------------------------

using Photon.Core.Mathematics;
using Photon.Core.Scenes;

namespace Photon.Core.Rendering;

/// <summary>
/// Defines a ray-acceleration structure for scene geometry.
/// </summary>
public interface IAccelerationStructure
{
    /// <summary>
    /// Finds the closest primitive intersection within the specified distance range.
    /// </summary>
    /// <param name="ray">The ray to test against the scene geometry.</param>
    /// <param name="minDistance">The inclusive minimum distance for an accepted intersection.</param>
    /// <param name="maxDistance">The inclusive maximum distance for an accepted intersection.</param>
    /// <param name="hit">
    /// When this method returns <see langword="true"/>, contains the closest primitive hit.
    /// Otherwise, contains the default value.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if a primitive is hit within the distance range; otherwise,
    /// <see langword="false"/>. An empty acceleration structure returns <see langword="false"/>.
    /// </returns>
    bool Intersect(in Ray ray, float minDistance, float maxDistance, out HitInfo hit);
}
