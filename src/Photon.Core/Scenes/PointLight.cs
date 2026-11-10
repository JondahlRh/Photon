// -----------------------------------------------------------------------------
// Author:        Luca Lengl
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------

using System.Numerics;
using Photon.Core.Mathematics;

namespace Photon.Core.Scenes;

/// <summary>
/// A light source without extent that shines equally in all directions and casts hard shadows.
/// </summary>
/// <remarks>
/// The intensity does not decrease with distance, as in the classic Phong model from the
/// project requirements. Attenuation can be added later without changing <see cref="ILightSource"/>.
/// </remarks>
public sealed class PointLight : ILightSource
{
    /// <summary>
    /// Initializes a new point light.
    /// </summary>
    /// <param name="position">The position in world coordinates.</param>
    /// <param name="intensity">The linear RGB intensity (Il), e.g. (1, 1, 1) for white light.</param>
    /// <exception cref="ArgumentException">Thrown if a value is not finite or an intensity component is negative.</exception>
    public PointLight(Vector3 position, Vector3 intensity)
    {
        if (!VectorMath.IsFinite(position))
        {
            throw new ArgumentException("Position must be finite.", nameof(position));
        }

        if (!VectorMath.IsFiniteAndNotNegative(intensity))
        {
            throw new ArgumentException(
                "Intensity must be finite and not negative.",
                nameof(intensity)
            );
        }

        Position = position;
        Intensity = intensity;
    }

    /// <summary>
    /// Gets the position in world coordinates.
    /// </summary>
    public Vector3 Position { get; }

    /// <summary>
    /// Gets the linear RGB intensity (Il).
    /// </summary>
    public Vector3 Intensity { get; }

    /// <inheritdoc/>
    public LightSample Illuminate(Vector3 point)
    {
        Vector3 toLight = Position - point;
        float distance = toLight.Length();

        // A point exactly at the light position gets no defined direction; treat it as unlit.
        if (distance < FloatComparison.Epsilon)
        {
            return new LightSample(Vector3.UnitY, 0f, Vector3.Zero);
        }

        return new LightSample(toLight / distance, distance, Intensity);
    }
}
