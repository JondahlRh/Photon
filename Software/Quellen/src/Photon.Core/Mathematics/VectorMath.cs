// -----------------------------------------------------------------------------
// Author:        Luca Lengl
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------

using System.Numerics;

namespace Photon.Core.Mathematics;

/// <summary>
/// Checks for <see cref="Vector3"/> values that are needed in several places of the core.
/// </summary>
public static class VectorMath
{
    /// <summary>
    /// Determines whether no component of the vector is NaN or infinite.
    /// </summary>
    /// <param name="value">The vector to check.</param>
    /// <returns><c>true</c> if all components are finite; otherwise, <c>false</c>.</returns>
    public static bool IsFinite(Vector3 value)
    {
        return float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
    }

    /// <summary>
    /// Determines whether all components are finite and not negative, as required for colors and intensities.
    /// </summary>
    /// <param name="value">The vector to check.</param>
    /// <returns><c>true</c> if all components are finite and zero or greater; otherwise, <c>false</c>.</returns>
    public static bool IsFiniteAndNotNegative(Vector3 value)
    {
        return IsFinite(value) && value.X >= 0f && value.Y >= 0f && value.Z >= 0f;
    }

    /// <summary>
    /// Determines whether the vector is too short to be normalized reliably.
    /// </summary>
    /// <param name="value">The vector to check.</param>
    /// <returns><c>true</c> if the length is below <see cref="FloatComparison.Epsilon"/>; otherwise, <c>false</c>.</returns>
    public static bool IsNearlyZero(Vector3 value)
    {
        return value.LengthSquared() < FloatComparison.Epsilon * FloatComparison.Epsilon;
    }
}
