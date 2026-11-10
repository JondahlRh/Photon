// -----------------------------------------------------------------------------
// Author:        Luca Lengl
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------

using System.Numerics;

namespace Photon.Core.Scenes;

/// <summary>
/// The light that arrives at a surface point from one light source.
/// </summary>
/// <param name="Direction">The normalized direction from the surface point towards the light (L).</param>
/// <param name="Distance">The distance to the light; a shadow ray only needs to be tested up to here.</param>
/// <param name="Intensity">The linear RGB intensity of the light at the point (Il).</param>
public readonly record struct LightSample(Vector3 Direction, float Distance, Vector3 Intensity);
