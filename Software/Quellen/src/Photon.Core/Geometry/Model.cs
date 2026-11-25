// -----------------------------------------------------------------------------
// Author:        BrackyDayn
// AI Assistance: ChatGpt Codex for help with integration and summary, inspiration form https://pbr-book.org/4ed/Shapes/Triangle_Meshes
// -----------------------------------------------------------------------------
using System.Reflection.Metadata.Ecma335;

namespace Photon.Core.Geometry;

/// <summary>
/// Combines triangle geometry with materials and their assignments.
/// </summary>
public readonly struct Model
{
    /// <summary>
    /// The materials, or null for a default model.
    /// </summary>
    private readonly IReadOnlyList<Material>? _materials;

    /// <summary>
    /// The material indices, or null for a default model.
    /// </summary>
    private readonly IReadOnlyList<int>? _materialIndices;

    /// <summary>
    /// Initializes a model with geometry and material assignments.
    /// </summary>
    /// <param name="mesh">The triangle geometry.</param>
    /// <param name="materials">The available materials.</param>
    /// <param name="materialIndices">
    /// One material index for each triangle.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when either collection is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the index count differs from the triangle count,
    /// or an index does not refer to an existing material.
    /// </exception>
    public Model(Mesh mesh, IEnumerable<Material> materials, IEnumerable<int> materialIndices)
    {
        ArgumentNullException.ThrowIfNull(materials);
        ArgumentNullException.ThrowIfNull(materialIndices);

        Material[] materialArray = materials.ToArray();
        int[] indexArray = materialIndices.ToArray();

        if (indexArray.Length != mesh.Triangles.Count)
        {
            throw new ArgumentException("Triangle msut have one index", nameof(materialIndices));
        }
        foreach (int index in indexArray)
        {
            if (index < 0 || index >= materialArray.Length)
            {
                throw new ArgumentException(
                    "index does not refer material",
                    nameof(materialIndices)
                );
            }
        }
        Mesh = mesh;
        _materials = Array.AsReadOnly(materialArray);
        _materialIndices = Array.AsReadOnly(indexArray);
    }

    /// <summary>
    /// Gets the triangle geometry.
    /// </summary>
    public Mesh Mesh { get; }

    /// <summary>
    /// Gets the available materials.
    /// </summary>
    public IReadOnlyList<Material> Materials
    {
        get
        {
            if (_materials == null)
            {
                return Array.Empty<Material>();
            }

            return _materials;
        }
    }

    /// <summary>
    /// Gets one material index for each triangle
    /// </summary>
    public IReadOnlyList<int> MaterialIndices
    {
        get
        {
            if (_materialIndices == null)
            {
                return Array.Empty<int>();
            }
            return _materialIndices;
        }
    }
}
