// -----------------------------------------------------------------------------
// Author:        Jondahl Rhenius, Luca Lengl
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------

using System.Numerics;

namespace Photon.Core.Rendering;

/// <summary>
/// Immutable settings for one render.
/// </summary>
/// <param name="BackgroundColor">Linear RGB color for rays that hit nothing.</param>
/// <param name="MaxDepth">Maximum recursion depth for reflection and refraction.</param>
public sealed record RenderSettings(Vector3 BackgroundColor, int MaxDepth = 5)
{
    /// <summary>
    /// Gets default settings with a black background.
    /// </summary>
    public static RenderSettings Default { get; } = new(Vector3.Zero);
}
