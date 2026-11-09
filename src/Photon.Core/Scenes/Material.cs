// -----------------------------------------------------------------------------
// Author:        Luca Lengl
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------

using System.Numerics;
using Photon.Core.Mathematics;

namespace Photon.Core.Scenes;

/// <summary>
/// Describes how a surface reflects and transmits light, based on the coefficients of an MTL file.
/// </summary>
/// <remarks>
/// All colors are linear RGB values. How the reflection share is derived from MTL values
/// is still an open point in the architecture notebook (chapter 9).
/// </remarks>
public sealed class Material
{
    /// <summary>
    /// Initializes a new material and validates all coefficients.
    /// </summary>
    /// <param name="name">The material name as used by <c>newmtl</c> and <c>usemtl</c>.</param>
    /// <param name="ambient">The ambient reflection coefficient (<c>Ka</c>).</param>
    /// <param name="diffuse">The diffuse reflection coefficient (<c>Kd</c>).</param>
    /// <param name="specular">The specular reflection coefficient (<c>Ks</c>).</param>
    /// <param name="specularExponent">The Phong exponent (<c>Ns</c>); zero or greater.</param>
    /// <param name="opacity">The opacity (<c>d</c>), from 0 (fully transparent) to 1 (opaque).</param>
    /// <param name="refractiveIndex">The index of refraction (<c>Ni</c>); greater than zero.</param>
    /// <param name="reflectivity">The share of light reflected recursively (kr), from 0 to 1.</param>
    /// <param name="illuminationModel">The MTL illumination model (<c>illum</c>), from 0 to 10.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="name"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if a coefficient is outside its valid range.</exception>
    public Material(
        string name,
        Vector3 ambient,
        Vector3 diffuse,
        Vector3 specular,
        float specularExponent = 0f,
        float opacity = 1f,
        float refractiveIndex = 1f,
        float reflectivity = 0f,
        int illuminationModel = 2
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ValidateColor(ambient, nameof(ambient));
        ValidateColor(diffuse, nameof(diffuse));
        ValidateColor(specular, nameof(specular));
        ValidateRange(specularExponent, 0f, float.MaxValue, nameof(specularExponent));
        ValidateRange(opacity, 0f, 1f, nameof(opacity));
        ValidateRange(reflectivity, 0f, 1f, nameof(reflectivity));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(refractiveIndex, 0f);
        ArgumentOutOfRangeException.ThrowIfLessThan(illuminationModel, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(illuminationModel, 10);

        Name = name;
        Ambient = ambient;
        Diffuse = diffuse;
        Specular = specular;
        SpecularExponent = specularExponent;
        Opacity = opacity;
        RefractiveIndex = refractiveIndex;
        Reflectivity = reflectivity;
        IlluminationModel = illuminationModel;
    }

    /// <summary>
    /// Gets the light gray material used for faces without a known material.
    /// </summary>
    public static Material Default { get; } =
        new("Default", new Vector3(0.1f), new Vector3(0.8f), new Vector3(0.2f), 32f);

    /// <summary>
    /// Gets the material name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the ambient reflection coefficient (<c>Ka</c>).
    /// </summary>
    public Vector3 Ambient { get; }

    /// <summary>
    /// Gets the diffuse reflection coefficient (<c>Kd</c>), used for Lambert reflection.
    /// </summary>
    public Vector3 Diffuse { get; }

    /// <summary>
    /// Gets the specular reflection coefficient (<c>Ks</c>), used for Phong reflection.
    /// </summary>
    public Vector3 Specular { get; }

    /// <summary>
    /// Gets the Phong exponent (<c>Ns</c>); larger values give smaller, sharper highlights.
    /// </summary>
    public float SpecularExponent { get; }

    /// <summary>
    /// Gets the opacity (<c>d</c>), from 0 (fully transparent) to 1 (opaque).
    /// </summary>
    public float Opacity { get; }

    /// <summary>
    /// Gets the share of light transmitted through the surface (kt = 1 - d).
    /// </summary>
    public float Transparency => 1f - Opacity;

    /// <summary>
    /// Gets the index of refraction (<c>Ni</c>) used for Snell's law.
    /// </summary>
    public float RefractiveIndex { get; }

    /// <summary>
    /// Gets the share of light reflected recursively (kr), from 0 to 1.
    /// </summary>
    public float Reflectivity { get; }

    /// <summary>
    /// Gets the MTL illumination model (<c>illum</c>).
    /// </summary>
    public int IlluminationModel { get; }

    /// <summary>
    /// Throws if a color has a negative or non-finite component.
    /// </summary>
    /// <param name="color">The color to check.</param>
    /// <param name="parameterName">The parameter name for the exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the color is invalid.</exception>
    private static void ValidateColor(Vector3 color, string parameterName)
    {
        if (!VectorMath.IsFiniteAndNotNegative(color))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                color,
                "Color components must be finite and not negative."
            );
        }
    }

    /// <summary>
    /// Throws if a value is not finite or outside the given range.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="min">The smallest allowed value.</param>
    /// <param name="max">The largest allowed value.</param>
    /// <param name="parameterName">The parameter name for the exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is invalid.</exception>
    private static void ValidateRange(float value, float min, float max, string parameterName)
    {
        if (!float.IsFinite(value) || value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"Value must be between {min} and {max}."
            );
        }
    }
}
