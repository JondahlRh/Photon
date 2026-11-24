// -----------------------------------------------------------------------------
// Author:        XISander
// AI Assistance: Claude Code for implementation and documentation
// -----------------------------------------------------------------------------

using System.Numerics;
using Photon.Core.Mathematics;
using Photon.Core.Scenes;

namespace Photon.Core.Rendering;

/// <summary>
/// Calculates the color that a viewer sees at a surface point.
/// </summary>
public interface ILightingModel
{
    /// <summary>
    /// Calculates the color at the hit point of a ray.
    /// </summary>
    /// <param name="ray">The ray that hit the surface; its reverse direction points to the viewer.</param>
    /// <param name="hit">The surface point, its normals and its material.</param>
    /// <param name="lighting">The ambient light and the light sources of the scene.</param>
    /// <param name="occluders">The scene geometry, used to test whether a light is blocked.</param>
    /// <returns>The linear RGB color at the hit point.</returns>
    Vector3 Shade(in Ray ray, in HitInfo hit, Lighting lighting, IAccelerationStructure occluders);
}
