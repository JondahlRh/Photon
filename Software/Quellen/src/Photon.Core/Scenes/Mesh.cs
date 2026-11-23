// -----------------------------------------------------------------------------
// Author:        Luca Lengl
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------

using System.Numerics;
using Photon.Core.Mathematics;

namespace Photon.Core.Scenes;

/// <summary>
/// A set of triangles with shared vertex and normal arrays and exactly one material.
/// </summary>
/// <remarks>
/// Vertex <c>i</c> has the position <c>Vertices[i]</c> and the normal <c>Normals[i]</c>.
/// The arrays are copied on construction, so the mesh cannot be changed from outside.
/// </remarks>
public sealed class Mesh
{
    /// <summary>
    /// The vertex positions in world coordinates.
    /// </summary>
    private readonly Vector3[] _vertices;

    /// <summary>
    /// The normalized vertex normals, one per vertex.
    /// </summary>
    private readonly Vector3[] _normals;

    /// <summary>
    /// The triangles of the mesh.
    /// </summary>
    private readonly MeshTriangle[] _triangles;

    /// <summary>
    /// Initializes a new mesh and creates its triangles.
    /// </summary>
    /// <param name="vertices">The vertex positions in world coordinates.</param>
    /// <param name="normals">The vertex normals; same length as <paramref name="vertices"/>. They are normalized here.</param>
    /// <param name="indices">Three vertex indices per triangle, counter-clockwise seen from the front.</param>
    /// <param name="material">The material of all triangles.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="material"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if the array lengths do not match, the index count is not a multiple of three,
    /// a normal has zero length or a value is not finite.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if an index is outside the vertex array.</exception>
    public Mesh(
        ReadOnlySpan<Vector3> vertices,
        ReadOnlySpan<Vector3> normals,
        ReadOnlySpan<int> indices,
        Material material
    )
    {
        ArgumentNullException.ThrowIfNull(material);

        if (normals.Length != vertices.Length)
        {
            throw new ArgumentException("Each vertex needs exactly one normal.", nameof(normals));
        }

        if (indices.Length % 3 != 0)
        {
            throw new ArgumentException(
                "The index count must be a multiple of three.",
                nameof(indices)
            );
        }

        _vertices = vertices.ToArray();
        _normals = new Vector3[normals.Length];
        Aabb bounds = Aabb.Empty;

        for (var i = 0; i < _vertices.Length; i++)
        {
            if (!VectorMath.IsFinite(_vertices[i]))
            {
                throw new ArgumentException($"Vertex {i} is not finite.", nameof(vertices));
            }

            if (!VectorMath.IsFinite(normals[i]) || VectorMath.IsNearlyZero(normals[i]))
            {
                throw new ArgumentException(
                    $"Normal {i} is not finite or has zero length.",
                    nameof(normals)
                );
            }

            _normals[i] = Vector3.Normalize(normals[i]);
            bounds = bounds.Include(_vertices[i]);
        }

        Material = material;
        Bounds = bounds;
        _triangles = new MeshTriangle[indices.Length / 3];

        for (var t = 0; t < _triangles.Length; t++)
        {
            int i0 = CheckIndex(indices[3 * t]);
            int i1 = CheckIndex(indices[(3 * t) + 1]);
            int i2 = CheckIndex(indices[(3 * t) + 2]);
            _triangles[t] = new MeshTriangle(this, i0, i1, i2);
        }
    }

    /// <summary>
    /// Gets the vertex positions in world coordinates.
    /// </summary>
    public ReadOnlySpan<Vector3> Vertices => _vertices;

    /// <summary>
    /// Gets the normalized vertex normals, one per vertex.
    /// </summary>
    public ReadOnlySpan<Vector3> Normals => _normals;

    /// <summary>
    /// Gets the triangles of the mesh.
    /// </summary>
    public IReadOnlyList<MeshTriangle> Triangles => _triangles;

    /// <summary>
    /// Gets the material of all triangles.
    /// </summary>
    public Material Material { get; }

    /// <summary>
    /// Gets the box that contains all vertices.
    /// </summary>
    public Aabb Bounds { get; }

    /// <summary>
    /// Throws if an index does not refer to a vertex.
    /// </summary>
    /// <param name="index">The index to check.</param>
    /// <returns>The unchanged index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is outside the vertex array.</exception>
    private int CheckIndex(int index)
    {
        if ((uint)index >= (uint)_vertices.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                "Index does not refer to a vertex."
            );
        }

        return index;
    }
}
