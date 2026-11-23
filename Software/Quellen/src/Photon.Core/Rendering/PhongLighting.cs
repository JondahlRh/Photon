// -----------------------------------------------------------------------------
// Author:        XISander
// AI Assistance: Claude Code for implementation and documentation
// -----------------------------------------------------------------------------

using System.Numerics;
using Photon.Core.Mathematics;
using Photon.Core.Scenes;

namespace Photon.Core.Rendering;

/// <summary>
/// Local illumination with ambient light, diffuse reflection (Lambert) and highlights (Phong).
/// The interpolated shading normal of the hit is used, so curved meshes look smooth (Phong shading).
/// </summary>
/// <remarks>
/// <c>I = Kd * Ia + sum over visible lights of Il * (Kd * max(0, N·L) + Ks * max(0, R·V)^Ns)</c>,
/// clamped to [0, 1] per channel. The ambient coefficient <c>Ka</c> of the material is not used.
/// </remarks>
public sealed class PhongLighting : ILightingModel
{
    /// <inheritdoc/>
    public Vector3 Shade(
        in Ray ray,
        in HitInfo hit,
        Lighting lighting,
        IAccelerationStructure occluders
    )
    {
        ArgumentNullException.ThrowIfNull(lighting);
        ArgumentNullException.ThrowIfNull(occluders);

        Material material = hit.Material;
        Vector3 toViewer = -ray.Direction;

        // Seen from the back, the surface is lit from the back: turn the normal to the viewer.
        // The geometric normal decides the side, because interpolated normals near silhouettes
        // can point slightly away from the viewer although the front side is visible.
        Vector3 normal =
            Vector3.Dot(hit.GeometricNormal, toViewer) >= 0f
                ? hit.ShadingNormal
                : -hit.ShadingNormal;

        // The ambient term uses the diffuse color, not Ka: Blender writes "Ka 1 1 1" for every
        // material, which would give all surfaces the same gray base and wash out dark colors.
        Vector3 color = material.Diffuse * lighting.AmbientIntensity;

        foreach (ILightSource light in lighting.Lights)
        {
            LightSample sample = light.Illuminate(hit.Point);

            // Lambert's cosine law: a light behind the surface (N·L <= 0) adds nothing. This
            // cheap test comes first, so no shadow ray is traced for such lights.
            float normalDotLight = Vector3.Dot(normal, sample.Direction);
            if (normalDotLight <= 0f || VectorMath.IsNearlyZero(sample.Intensity))
            {
                continue;
            }

            if (!ShadowRays.IsLightVisible(occluders, hit, sample))
            {
                continue;
            }

            Vector3 diffuse = material.Diffuse * normalDotLight;

            // Mirror L at N: R = 2 (N·L) N - L. The highlight is strongest where R points to the viewer.
            Vector3 reflected = (2f * normalDotLight * normal) - sample.Direction;
            float reflectedDotViewer = Vector3.Dot(reflected, toViewer);
            Vector3 specular =
                reflectedDotViewer > 0f
                    ? material.Specular * MathF.Pow(reflectedDotViewer, material.SpecularExponent)
                    : Vector3.Zero;

            color += sample.Intensity * (diffuse + specular);
        }

        return Vector3.Clamp(color, Vector3.Zero, Vector3.One);
    }
}
