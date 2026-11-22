// --------------------------------------------------------------------------
// Author: Luca Lengl
// AI Assistance: Claude for information, bugs and warnings
// --------------------------------------------------------------------------

using Photon.Core.Mathematics;
using Photon.Core.Scenes;

namespace Photon.Core.Rendering;

/// <summary>
/// An acceleration structure that simply tests every primitive. Used as a reference
/// for faster structures and until the bounding volume hierarchy is available.
/// </summary>
public sealed class BruteForceAccelerationStructure : IAccelerationStructure
{
    /// <summary>
    /// The primitives that are tested one after another.
    /// </summary>
    private readonly IPrimitive[] _primitives;

    /// <summary>
    /// Initializes a new brute force structure.
    /// </summary>
    /// <param name="primitives">The primitives of the scene.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="primitives"/> or one of its elements is null.</exception>
    public BruteForceAccelerationStructure(IEnumerable<IPrimitive> primitives)
    {
        ArgumentNullException.ThrowIfNull(primitives);

        _primitives = primitives.ToArray();
        foreach (IPrimitive primitive in _primitives)
        {
            ArgumentNullException.ThrowIfNull(primitive, nameof(primitives));
        }
    }

    /// <inheritdoc/>
    public bool FindClosestHit(in Ray ray, float minDistance, float maxDistance, out HitInfo hit)
    {
        hit = default;
        bool found = false;
        float closestDistance = maxDistance;

        foreach (IPrimitive primitive in _primitives)
        {
            if (primitive.Intersect(ray, minDistance, closestDistance, out HitInfo candidate))
            {
                closestDistance = candidate.Distance;
                hit = candidate;
                found = true;
            }
        }

        return found;
    }

    /// <inheritdoc/>
    public bool IsOccluded(in Ray ray, float minDistance, float maxDistance)
    {
        foreach (IPrimitive primitive in _primitives)
        {
            // Any hit blocks the ray, so there is no need to search for the nearest one.
            if (primitive.Intersect(ray, minDistance, maxDistance, out _))
            {
                return true;
            }
        }

        return false;
    }
}
