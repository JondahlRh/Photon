// -----------------------------------------------------------------------------
// Author:        Luca Lengl
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------

using System.Numerics;
using System.Runtime.InteropServices;
using Photon.Core.Mathematics;

namespace Photon.Core.Scenes;

/// <summary>
/// Collects positions, normals, materials and faces while a file is read and creates
/// the immutable <see cref="Model"/> with <see cref="Build"/> (builder pattern).
/// </summary>
/// <remarks>
/// <para>
/// All indices are zero-based; converting the one-based and negative indices of OBJ files
/// is the importer's job. Every call to <see cref="UseMaterial"/> starts a new group, and
/// every group with at least one face becomes one <see cref="Mesh"/>.
/// </para>
/// <para>
/// Corners without a normal get a calculated vertex normal: the area-weighted average of the
/// normals of all faces that share the position, across all groups, so no seam appears where
/// the material changes.
/// </para>
/// <para>This class is not thread-safe.</para>
/// </remarks>
public sealed class ModelBuilder
{
    /// <summary>
    /// The positions added so far.
    /// </summary>
    private readonly List<Vector3> _positions = [];

    /// <summary>
    /// The normals added so far.
    /// </summary>
    private readonly List<Vector3> _normals = [];

    /// <summary>
    /// The registered materials by name.
    /// </summary>
    private readonly Dictionary<string, Material> _materials = new(StringComparer.Ordinal);

    /// <summary>
    /// The face groups in the order they were started.
    /// </summary>
    private readonly List<FaceGroup> _groups = [];

    /// <summary>
    /// The group that receives new faces.
    /// </summary>
    private FaceGroup _currentGroup;

    /// <summary>
    /// Initializes a new builder whose first group uses <see cref="Material.Default"/>.
    /// </summary>
    public ModelBuilder()
    {
        _currentGroup = new FaceGroup(Material.Default);
        _groups.Add(_currentGroup);
    }

    /// <summary>
    /// Gets the number of positions added so far.
    /// </summary>
    public int PositionCount => _positions.Count;

    /// <summary>
    /// Gets the number of normals added so far.
    /// </summary>
    public int NormalCount => _normals.Count;

    /// <summary>
    /// Adds a vertex position.
    /// </summary>
    /// <param name="position">The position in world coordinates.</param>
    /// <returns>The zero-based index of the new position.</returns>
    /// <exception cref="ArgumentException">Thrown if a component is not finite.</exception>
    public int AddPosition(Vector3 position)
    {
        if (!VectorMath.IsFinite(position))
        {
            throw new ArgumentException("All components must be finite.", nameof(position));
        }

        _positions.Add(position);
        return _positions.Count - 1;
    }

    /// <summary>
    /// Adds a vertex normal. It does not need to be normalized.
    /// </summary>
    /// <param name="normal">The normal vector.</param>
    /// <returns>The zero-based index of the new normal.</returns>
    /// <exception cref="ArgumentException">Thrown if a component is not finite or the vector has zero length.</exception>
    public int AddNormal(Vector3 normal)
    {
        if (!VectorMath.IsFinite(normal) || VectorMath.IsNearlyZero(normal))
        {
            throw new ArgumentException(
                "Normal must be finite and not have zero length.",
                nameof(normal)
            );
        }

        _normals.Add(normal);
        return _normals.Count - 1;
    }

    /// <summary>
    /// Registers a material so that <see cref="UseMaterial"/> can select it by name.
    /// A material with the same name replaces the earlier one.
    /// </summary>
    /// <param name="material">The material to register.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="material"/> is null.</exception>
    public void AddMaterial(Material material)
    {
        ArgumentNullException.ThrowIfNull(material);
        _materials[material.Name] = material;

        // A usemtl line may come before its mtllib was read; update groups that wait for it.
        foreach (FaceGroup group in _groups)
        {
            if (group.PendingMaterialName == material.Name)
            {
                group.Material = material;
            }
        }
    }

    /// <summary>
    /// Starts a new group whose faces use the material with the given name.
    /// </summary>
    /// <param name="name">The material name.</param>
    /// <returns>
    /// <c>true</c> if the material is registered; <c>false</c> if it is unknown. Unknown materials
    /// use <see cref="Material.Default"/> unless a material with this name is added later.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="name"/> is null, empty or whitespace.</exception>
    public bool UseMaterial(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        bool known = _materials.TryGetValue(name, out Material? material);
        _currentGroup = new FaceGroup(material ?? Material.Default)
        {
            PendingMaterialName = known ? null : name,
        };
        _groups.Add(_currentGroup);
        return known;
    }

    /// <summary>
    /// Adds a face with three or more corners. Faces with more corners are split into a
    /// triangle fan around the first corner, which is correct for convex polygons.
    /// </summary>
    /// <param name="corners">The corners in counter-clockwise order seen from the front.</param>
    /// <exception cref="ArgumentException">Thrown if fewer than three corners are given.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if an index refers to no position or normal.</exception>
    public void AddFace(ReadOnlySpan<FaceCorner> corners)
    {
        if (corners.Length < 3)
        {
            throw new ArgumentException("A face needs at least three corners.", nameof(corners));
        }

        foreach (FaceCorner corner in corners)
        {
            if ((uint)corner.PositionIndex >= (uint)_positions.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(corners),
                    corner.PositionIndex,
                    "Position index does not refer to a position."
                );
            }

            if (corner.NormalIndex is int normalIndex && (uint)normalIndex >= (uint)_normals.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(corners),
                    normalIndex,
                    "Normal index does not refer to a normal."
                );
            }
        }

        for (var i = 1; i < corners.Length - 1; i++)
        {
            _currentGroup.Corners.Add(corners[0]);
            _currentGroup.Corners.Add(corners[i]);
            _currentGroup.Corners.Add(corners[i + 1]);
        }
    }

    /// <summary>
    /// Adds a triangle whose corners have no normals.
    /// </summary>
    /// <param name="positionIndex0">The index of the first position.</param>
    /// <param name="positionIndex1">The index of the second position.</param>
    /// <param name="positionIndex2">The index of the third position.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if an index refers to no position.</exception>
    public void AddTriangle(int positionIndex0, int positionIndex1, int positionIndex2)
    {
        AddFace([new(positionIndex0), new(positionIndex1), new(positionIndex2)]);
    }

    /// <summary>
    /// Creates the immutable model from everything added so far. The builder can be used further.
    /// </summary>
    /// <returns>The new model; one mesh per group that contains at least one face.</returns>
    public Model Build()
    {
        Vector3[] calculatedNormals = CalculateVertexNormals();
        var meshes = new List<Mesh>();

        foreach (FaceGroup group in _groups)
        {
            if (group.Corners.Count > 0)
            {
                meshes.Add(BuildMesh(group, calculatedNormals));
            }
        }

        return new Model(meshes);
    }

    /// <summary>
    /// Calculates one normal per position from all faces that share it.
    /// </summary>
    /// <returns>The normalized normal per position; <see cref="Vector3.UnitY"/> for unused or degenerate positions.</returns>
    private Vector3[] CalculateVertexNormals()
    {
        var sums = new Vector3[_positions.Count];

        foreach (FaceGroup group in _groups)
        {
            for (var i = 0; i < group.Corners.Count; i += 3)
            {
                int a = group.Corners[i].PositionIndex;
                int b = group.Corners[i + 1].PositionIndex;
                int c = group.Corners[i + 2].PositionIndex;

                // The cross product is twice the triangle area long, so larger
                // faces contribute more (area weighting).
                Vector3 faceNormal = Vector3.Cross(
                    _positions[b] - _positions[a],
                    _positions[c] - _positions[a]
                );
                sums[a] += faceNormal;
                sums[b] += faceNormal;
                sums[c] += faceNormal;
            }
        }

        for (var i = 0; i < sums.Length; i++)
        {
            // No epsilon check here: the sums scale with the model size, so a fixed
            // threshold would reject valid normals of small models. Only a zero sum
            // (unused position or degenerate faces) gives a non-finite result.
            Vector3 normal = Vector3.Normalize(sums[i]);
            sums[i] = VectorMath.IsFinite(normal) ? normal : Vector3.UnitY;
        }

        return sums;
    }

    /// <summary>
    /// Creates a mesh with its own vertex array for one group. Each distinct combination of
    /// position and normal becomes one vertex, so triangles share vertices where possible.
    /// </summary>
    /// <param name="group">The group to convert.</param>
    /// <param name="calculatedNormals">The calculated normal per position.</param>
    /// <returns>The mesh of the group.</returns>
    private Mesh BuildMesh(FaceGroup group, Vector3[] calculatedNormals)
    {
        var vertexLookup = new Dictionary<FaceCorner, int>();
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var indices = new int[group.Corners.Count];

        for (var i = 0; i < indices.Length; i++)
        {
            FaceCorner corner = group.Corners[i];
            if (!vertexLookup.TryGetValue(corner, out int vertexIndex))
            {
                vertexIndex = vertices.Count;
                vertexLookup.Add(corner, vertexIndex);
                vertices.Add(_positions[corner.PositionIndex]);
                normals.Add(
                    corner.NormalIndex is int normalIndex
                        ? _normals[normalIndex]
                        : calculatedNormals[corner.PositionIndex]
                );
            }

            indices[i] = vertexIndex;
        }

        return new Mesh(
            CollectionsMarshal.AsSpan(vertices),
            CollectionsMarshal.AsSpan(normals),
            indices,
            group.Material
        );
    }

    /// <summary>
    /// The faces added after one <see cref="UseMaterial"/> call, stored as three corners per triangle.
    /// </summary>
    private sealed class FaceGroup
    {
        /// <summary>
        /// Initializes a new empty group.
        /// </summary>
        /// <param name="material">The material of the group.</param>
        public FaceGroup(Material material)
        {
            Material = material;
        }

        /// <summary>
        /// Gets or sets the material of the group.
        /// </summary>
        public Material Material { get; set; }

        /// <summary>
        /// Gets the name of a material that was not registered yet when the group started, or <c>null</c>.
        /// </summary>
        public string? PendingMaterialName { get; init; }

        /// <summary>
        /// Gets the triangle corners, three per triangle.
        /// </summary>
        public List<FaceCorner> Corners { get; } = [];
    }
}
