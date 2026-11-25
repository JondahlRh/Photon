// -----------------------------------------------------------------------------
// Author:        Florian Hipp
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------

using System;

namespace Photon.Core.Rendering;

/// <summary>
/// Key figures of one finished render.
/// </summary>
/// <param name="Width">The image width in pixels.</param>
/// <param name="Height">The image height in pixels.</param>
/// <param name="TriangleCount">The number of triangles in the scene.</param>
/// <param name="BvhBuildTime">The time needed to build the acceleration structure.</param>
/// <param name="RenderTime">The time needed to trace all pixels.</param>
/// <param name="RayCount">The number of primary rays; shadow rays are not counted.</param>
public sealed record RenderStatistics(
    int Width,
    int Height,
    int TriangleCount,
    TimeSpan BvhBuildTime,
    TimeSpan RenderTime,
    long RayCount
)
{
    /// <summary>
    /// Gets the number of primary rays traced per second, or 0 if no time was measured.
    /// </summary>
    public double RaysPerSecond =>
        RenderTime > TimeSpan.Zero ? RayCount / RenderTime.TotalSeconds : 0d;
}
