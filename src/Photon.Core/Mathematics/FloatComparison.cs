// -----------------------------------------------------------------------------
// Author:        XISander
// AI Assistance: Claude for project skeleton
// -----------------------------------------------------------------------------

namespace Photon.Core.Mathematics;

/// <summary>
/// Provides tolerance-based comparisons for floating-point values.
/// </summary>
public static class FloatComparison
{
    /// <summary>
    /// Default tolerance used for floating-point comparisons and ray offsets.
    /// </summary>
    public const float Epsilon = 1e-5f;

    /// <summary>
    /// Determines whether two values are equal within the given tolerance.
    /// </summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <param name="tolerance">The maximum allowed absolute difference.</param>
    /// <returns><c>true</c> if the values differ by at most <paramref name="tolerance"/>; otherwise, <c>false</c>.</returns>
    public static bool NearlyEqual(float a, float b, float tolerance = Epsilon)
    {
        return MathF.Abs(a - b) <= tolerance;
    }
}
