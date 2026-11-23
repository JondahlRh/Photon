// -----------------------------------------------------------------------------
// Author:        Luca Lengl
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------

using System.Numerics;

namespace Photon.Core.Scenes;

/// <summary>
/// Describes where and how a ray hit a primitive.
/// </summary>
public readonly struct HitInfo
{
    /// <summary>
    /// Initializes a new hit description.
    /// </summary>
    /// <param name="distance">The distance from the ray origin to the hit point.</param>
    /// <param name="point">The hit point in world coordinates.</param>
    /// <param name="geometricNormal">The normalized normal of the flat surface.</param>
    /// <param name="shadingNormal">The normalized interpolated normal used for lighting.</param>
    /// <param name="material">The material of the surface that was hit.</param>
    public HitInfo(
        float distance,
        Vector3 point,
        Vector3 geometricNormal,
        Vector3 shadingNormal,
        Material material
    )
    {
        Distance = distance;
        Point = point;
        GeometricNormal = geometricNormal;
        ShadingNormal = shadingNormal;
        Material = material;
    }

    /// <summary>
    /// Gets the distance from the ray origin to the hit point.
    /// </summary>
    public float Distance { get; }

    /// <summary>
    /// Gets the hit point in world coordinates.
    /// </summary>
    public Vector3 Point { get; }

    /// <summary>
    /// Gets the normalized normal of the flat surface. Secondary rays are offset along it.
    /// </summary>
    public Vector3 GeometricNormal { get; }

    /// <summary>
    /// Gets the normalized interpolated normal (Phong shading). Used only for lighting.
    /// </summary>
    public Vector3 ShadingNormal { get; }

    /// <summary>
    /// Gets the material of the surface that was hit.
    /// </summary>
    public Material Material { get; }
}
