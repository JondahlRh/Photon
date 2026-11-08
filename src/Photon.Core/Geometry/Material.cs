// -----------------------------------------------------------------------------
// Author:        BrackyDayn
// AI Assistance: ChatGpt Codex for help with integration and summary
// -----------------------------------------------------------------------------
using System.Numerics;

namespace Photon.Core.Geometry;

/// <summary>
/// Describes the surface appearance of geometry.
/// </summary>
public readonly struct Material
{
    /// <summary>
    /// The material name, or null for a default material.
    /// </summary>
    private readonly string? _name;

    /// <summary>
    /// Initializes a material with a name and diffuse color.
    /// </summary>
    /// <param name="name">The material name.</param>
    /// <param name="diffuseColor">The diffuse RGB color.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is empty or whitespace.
    /// </exception>
    public Material(string name, Vector3 diffuseColor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _name = name;
        DiffuseColor = diffuseColor;
    }

    /// <summary>
    /// Gets the material name, or an empty string for a default material.
    /// </summary>
    public string Name
    {
        get
        {
            if (_name == null)
            {
                return string.Empty;
            }

            return _name;
        }
    }

    /// <summary>
    /// Gets the diffuse RGB color.
    /// </summary>
    public Vector3 DiffuseColor { get; }
}
