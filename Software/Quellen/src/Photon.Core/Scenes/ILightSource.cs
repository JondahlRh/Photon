// -----------------------------------------------------------------------------
// Author:        Luca Lengl
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------

using System.Numerics;

namespace Photon.Core.Scenes;

/// <summary>
/// Common interface for light sources, e.g. point lights and later area lights.
/// </summary>
public interface ILightSource
{
    /// <summary>
    /// Calculates the light that arrives at a surface point, ignoring shadows.
    /// </summary>
    /// <param name="point">The surface point in world coordinates.</param>
    /// <returns>Direction, distance and intensity of the incoming light.</returns>
    LightSample Illuminate(Vector3 point);
}
