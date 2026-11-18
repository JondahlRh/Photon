// -----------------------------------------------------------------------------
// Author:        XISander
// AI Assistance: Claude for project skeleton
// -----------------------------------------------------------------------------

using System.Numerics;

namespace Photon.Core.Mathematics;

/// <summary>
/// Represents a ray in world space with an origin and a normalized direction.
/// </summary>
public readonly record struct Ray
{
    /// <summary>
    /// Initializes a new ray. The direction is normalized automatically.
    /// </summary>
    /// <param name="origin">The starting point of the ray in world coordinates.</param>
    /// <param name="direction">The direction of the ray; must not be a zero vector.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="direction"/> has zero length.</exception>
    public Ray(Vector3 origin, Vector3 direction)
    {
        if (direction.LengthSquared() <= FloatComparison.Epsilon * FloatComparison.Epsilon)
        {
            throw new ArgumentException("Direction must not be zero.", nameof(direction));
        }

        Origin = origin;
        Direction = Vector3.Normalize(direction);
    }

    /// <summary>
    /// Gets the starting point of the ray in world coordinates.
    /// </summary>
    public Vector3 Origin { get; }

    /// <summary>
    /// Gets the normalized direction of the ray.
    /// </summary>
    public Vector3 Direction { get; }

    /// <summary>
    /// Calculates the point along the ray at the given distance.
    /// </summary>
    /// <param name="distance">The distance from the origin along the direction.</param>
    /// <returns>The point <c>P(t) = O + t * D</c>.</returns>
    public Vector3 PointAt(float distance)
    {
        // Parametric ray equation: P(t) = O + t * D
        return Origin + (distance * Direction);
    }

    /// <summary>
    /// Creates a secondary ray (e.g. shadow ray) that starts slightly above the surface
    /// to avoid self-intersection caused by rounding errors (ray acne).
    /// </summary>
    /// <param name="hitPoint">The surface point the ray starts from.</param>
    /// <param name="surfaceNormal">The normalized surface normal at <paramref name="hitPoint"/>.</param>
    /// <param name="direction">The direction of the secondary ray.</param>
    /// <returns>A ray whose origin is offset by <see cref="FloatComparison.Epsilon"/> along the normal.</returns>
    public static Ray CreateOffset(Vector3 hitPoint, Vector3 surfaceNormal, Vector3 direction)
    {
        return new Ray(hitPoint + (surfaceNormal * FloatComparison.Epsilon), direction);
    }
}
