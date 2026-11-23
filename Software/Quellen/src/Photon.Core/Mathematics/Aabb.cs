// -----------------------------------------------------------------------------
// Author:        Luca Lengl, Jondahl Rhenius
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------

using System.Numerics;

namespace Photon.Core.Mathematics;

/// <summary>
/// Represents an axis-aligned bounding box defined by its minimum and maximum corner.
/// </summary>
public readonly record struct Aabb
{
    /// <summary>
    /// Initializes a new bounding box from two corners.
    /// </summary>
    /// <param name="min">The corner with the smallest coordinates.</param>
    /// <param name="max">The corner with the largest coordinates.</param>
    public Aabb(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>
    /// Gets a box that contains nothing. Including a point or box in it yields that point or box.
    /// </summary>
    public static Aabb Empty { get; } =
        new(new Vector3(float.PositiveInfinity), new Vector3(float.NegativeInfinity));

    /// <summary>
    /// Gets the corner with the smallest coordinates.
    /// </summary>
    public Vector3 Min { get; }

    /// <summary>
    /// Gets the corner with the largest coordinates.
    /// </summary>
    public Vector3 Max { get; }

    /// <summary>
    /// Gets a value indicating whether the box contains no point at all.
    /// </summary>
    public bool IsEmpty => Min.X > Max.X || Min.Y > Max.Y || Min.Z > Max.Z;

    /// <summary>
    /// Gets the center point of the box.
    /// </summary>
    public Vector3 Center => (Min + Max) * 0.5f;

    /// <summary>
    /// Gets the edge lengths of the box along the three axes.
    /// </summary>
    public Vector3 Size => Max - Min;

    /// <summary>
    /// Creates the smallest box that contains the three given points.
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <param name="c">The third point.</param>
    /// <returns>The bounding box of the three points.</returns>
    public static Aabb FromPoints(Vector3 a, Vector3 b, Vector3 c)
    {
        return new Aabb(Vector3.Min(a, Vector3.Min(b, c)), Vector3.Max(a, Vector3.Max(b, c)));
    }

    /// <summary>
    /// Returns the smallest box that contains this box and the given point.
    /// </summary>
    /// <param name="point">The point to include.</param>
    /// <returns>The enlarged bounding box.</returns>
    public Aabb Include(Vector3 point)
    {
        return new Aabb(Vector3.Min(Min, point), Vector3.Max(Max, point));
    }

    /// <summary>
    /// Returns the smallest box that contains this box and the given box.
    /// </summary>
    /// <param name="other">The box to include.</param>
    /// <returns>The enlarged bounding box.</returns>
    public Aabb Include(Aabb other)
    {
        return new Aabb(Vector3.Min(Min, other.Min), Vector3.Max(Max, other.Max));
    }

    /// <summary>
    /// Tests whether a ray hits the box within the given distance range (slab method).
    /// </summary>
    /// <param name="ray">The ray to test.</param>
    /// <param name="inverseDirection">
    /// The component-wise inverse of the ray direction (<c>1 / direction</c>), computed once per ray.
    /// </param>
    /// <param name="minDistance">The smallest accepted distance along the ray.</param>
    /// <param name="maxDistance">The largest accepted distance along the ray.</param>
    /// <returns><c>true</c> if the ray enters the box within the range; otherwise, <c>false</c>.</returns>
    public bool Intersects(
        in Ray ray,
        Vector3 inverseDirection,
        float minDistance,
        float maxDistance
    )
    {
        float enter = minDistance;
        float exit = maxDistance;

        return IntersectsSlab(ray.Origin.X, inverseDirection.X, Min.X, Max.X, ref enter, ref exit)
            && IntersectsSlab(ray.Origin.Y, inverseDirection.Y, Min.Y, Max.Y, ref enter, ref exit)
            && IntersectsSlab(ray.Origin.Z, inverseDirection.Z, Min.Z, Max.Z, ref enter, ref exit);
    }

    /// <summary>
    /// Clips a ray interval against one axis-aligned slab.
    /// </summary>
    /// <param name="origin">The ray origin component on this axis.</param>
    /// <param name="inverseDirection">The inverse ray direction component on this axis.</param>
    /// <param name="min">The lower slab boundary.</param>
    /// <param name="max">The upper slab boundary.</param>
    /// <param name="enter">The current nearest entry distance; updated on success.</param>
    /// <param name="exit">The current farthest exit distance; updated on success.</param>
    /// <returns><c>true</c> if the ray interval still overlaps the slab.</returns>
    private static bool IntersectsSlab(
        float origin,
        float inverseDirection,
        float min,
        float max,
        ref float enter,
        ref float exit
    )
    {
        if (float.IsInfinity(inverseDirection))
        {
            return origin >= min && origin <= max;
        }

        float first = (min - origin) * inverseDirection;
        float second = (max - origin) * inverseDirection;
        enter = MathF.Max(enter, MathF.Min(first, second));
        exit = MathF.Min(exit, MathF.Max(first, second));

        return enter <= exit;
    }
}
