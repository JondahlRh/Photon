// -----------------------------------------------------------------------------
// Author:        BrackyDayn
// AI Assistance: ChatGpt Codex for help with integration and summary, inspiration form https://pbr-book.org/4ed/Shapes/Triangle_Meshes
// -----------------------------------------------------------------------------
using Photon.Core.Mathematics;

namespace Photon.Core.Geometry;

/// <summary>
/// Represents a model composed of triangles.
/// </summary>
public readonly struct Mesh
{
    /// <summary>
    /// The triangle collection, or null for a default mesh.
    /// </summary>
    private readonly IReadOnlyList<Triangle>? _triangles;

    /// <summary>
    /// Initializes a mesh with a copy of the supplied triangles.
    /// </summary>
    /// <param name="triangles">The triangles forming the mesh.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="triangles"/> is null.
    /// </exception>
    public Mesh(IEnumerable<Triangle> triangles)
    {
        ArgumentNullException.ThrowIfNull(triangles);

        _triangles = Array.AsReadOnly(triangles.ToArray());
    }

    /// <summary>
    /// Gets the triangles forming the mesh.
    /// Returns an empty collection for a default mesh.
    /// </summary>
    public IReadOnlyList<Triangle> Triangles
    {
        get
        {
            if (_triangles == null)
            {
                return Array.Empty<Triangle>();
            }

            return _triangles;
        }
    }
}
