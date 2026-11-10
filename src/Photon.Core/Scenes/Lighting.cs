// -----------------------------------------------------------------------------
// Author:        Luca Lengl
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------

using System.Numerics;
using Photon.Core.Mathematics;

namespace Photon.Core.Scenes;

/// <summary>
/// The lighting of a scene: ambient light and all light sources. Loaded and saved
/// independently of the model and the view.
/// </summary>
public sealed class Lighting
{
    /// <summary>
    /// The light sources.
    /// </summary>
    private readonly ILightSource[] _lights;

    /// <summary>
    /// Initializes a new lighting setup.
    /// </summary>
    /// <param name="ambientIntensity">The linear RGB intensity of the ambient light (Ia).</param>
    /// <param name="lights">The light sources.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="lights"/> or one of its elements is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the ambient intensity is negative or not finite.</exception>
    public Lighting(Vector3 ambientIntensity, IEnumerable<ILightSource> lights)
    {
        ArgumentNullException.ThrowIfNull(lights);

        if (!VectorMath.IsFiniteAndNotNegative(ambientIntensity))
        {
            throw new ArgumentException(
                "Ambient intensity must be finite and not negative.",
                nameof(ambientIntensity)
            );
        }

        _lights = lights.ToArray();
        foreach (ILightSource light in _lights)
        {
            ArgumentNullException.ThrowIfNull(light, nameof(lights));
        }

        AmbientIntensity = ambientIntensity;
    }

    /// <summary>
    /// Gets the linear RGB intensity of the ambient light (Ia).
    /// </summary>
    public Vector3 AmbientIntensity { get; }

    /// <summary>
    /// Gets the light sources.
    /// </summary>
    public IReadOnlyList<ILightSource> Lights => _lights;
}
