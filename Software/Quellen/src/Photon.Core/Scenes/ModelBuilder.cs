// -----------------------------------------------------------------------------
// Author:        Luca Lengl, MJZgh
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
    /// Relative tolerance for geometric tests, as a fraction of the squared size of the polygon.
    /// </summary>
    private const float RelativeTolerance = 1e-6f;

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
    /// Adds a face with three or more corners. Faces with more corners are validated as simple
    /// polygons and split by ear clipping, so concave faces are triangulated correctly.
    /// </summary>
    /// <param name="corners">The corners in order around the face.</param>
    /// <exception cref="ArgumentException">Thrown if fewer than three corners are given.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if an index refers to no position or normal.</exception>
    /// <exception cref="FormatException">Thrown if the polygon is degenerate or self-intersecting.</exception>
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

        AddTriangulated(corners);
    }

    /// <summary>
    /// Splits a simple polygon into triangles by ear clipping, so concave polygons are triangulated correctly.
    /// </summary>
    /// <param name="corners">The polygon corners in order.</param>
    /// <exception cref="FormatException">Thrown when the polygon is self-intersecting and cannot be triangulated.</exception>
    private void AddTriangulated(ReadOnlySpan<FaceCorner> corners)
    {
        var points = new Vector3[corners.Length];

        for (int i = 0; i < corners.Length; i++)
        {
            points[i] = _positions[corners[i].PositionIndex];
        }

        float size = BoundingBoxDiagonalSquared(points);
        float tolerance = RelativeTolerance * size;
        Vector3 normal = NewellNormal(points);

        if (normal.Length() <= tolerance)
        {
            throw new FormatException("The polygon is degenerate: its corners lie on one line.");
        }

        normal = Vector3.Normalize(normal);

        // Ear clipping only guarantees a correct result for simple polygons, so the
        // polygon is checked first. Otherwise invalid geometry would pass unnoticed.
        if (HasSelfIntersection(points, normal, tolerance))
        {
            throw new FormatException(
                "The polygon is self-intersecting and cannot be triangulated."
            );
        }
        var remaining = new List<int>(corners.Length);

        for (int i = 0; i < corners.Length; i++)
        {
            remaining.Add(i);
        }

        while (remaining.Count > 3)
        {
            bool clipped = false;

            for (int k = 0; k < remaining.Count && !clipped; k++)
            {
                int prev = remaining[(k + remaining.Count - 1) % remaining.Count];
                int curr = remaining[k];
                int next = remaining[(k + 1) % remaining.Count];

                if (!IsEar(points, remaining, prev, curr, next, normal))
                {
                    continue;
                }

                EmitTriangle(corners[prev], corners[curr], corners[next]);
                remaining.RemoveAt(k);
                clipped = true;
            }

            if (!clipped)
            {
                throw new FormatException(
                    "The polygon is self-intersecting and cannot be triangulated."
                );
            }
        }

        EmitTriangle(corners[remaining[0]], corners[remaining[1]], corners[remaining[2]]);
    }

    /// <summary>
    /// Gets the squared length of the diagonal of the bounding box of the points.
    /// </summary>
    /// <param name="points">The points to measure.</param>
    private static float BoundingBoxDiagonalSquared(Vector3[] points)
    {
        Vector3 min = points[0];
        Vector3 max = points[0];

        foreach (Vector3 point in points)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }

        return (max - min).LengthSquared();
    }

    /// <summary>
    /// Determines whether two edges that are not neighbours in the polygon touch or cross each other.
    /// </summary>
    /// <param name="points">The polygon corners in order.</param>
    /// <param name="normal">The polygon normal, which defines the orientation of the plane.</param>
    /// <param name="tolerance">The distance below which a point counts as lying on a line.</param>
    /// /// <returns><c>true</c> if the polygon intersects itself.</returns>
    private static bool HasSelfIntersection(Vector3[] points, Vector3 normal, float tolerance)
    {
        int count = points.Length;

        for (int i = 0; i < count; i++)
        {
            int iNext = (i + 1) % count;

            // Edges next to edge i share a corner with it and are skipped. Edge count - 1
            // is also next to edge 0, so it is skipped when i is 0.
            for (int j = i + 2; j < count; j++)
            {
                if (i == 0 && j == count - 1)
                {
                    continue;
                }

                int jNext = (j + 1) % count;

                if (
                    SegmentsTouch(
                        points[i],
                        points[iNext],
                        points[j],
                        points[jNext],
                        normal,
                        tolerance
                    )
                )
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether two line segments in the polygon's plane cross or touch.
    /// </summary>
    /// <param name="p1">The first endpoint of the first segment.</param>
    /// <param name="p2">The second endpoint of the first segment.</param>
    /// <param name="q1">The first endpoint of the second segment.</param>
    /// <param name="q2">The second endpoint of the second segment.</param>
    /// <param name="normal">The unit normal of the polygon plane.</param>
    /// <param name="tolerance">The distance below which a point counts as lying on a line.</param>
    private static bool SegmentsTouch(
        Vector3 p1,
        Vector3 p2,
        Vector3 q1,
        Vector3 q2,
        Vector3 normal,
        float tolerance
    )
    {
        float d1 = Side(q1, q2, p1, normal, tolerance);
        float d2 = Side(q1, q2, p2, normal, tolerance);
        float d3 = Side(p1, p2, q1, normal, tolerance);
        float d4 = Side(p1, p2, q2, normal, tolerance);

        // Proper crossing: each segment has its endpoints on opposite sides of the other.
        if (d1 * d2 < 0 && d3 * d4 < 0)
        {
            return true;
        }

        // Touching: an endpoint lies exactly on the other segment.
        return (d1 == 0f && IsOnSegment(q1, q2, p1))
            || (d2 == 0f && IsOnSegment(q1, q2, p2))
            || (d3 == 0f && IsOnSegment(p1, p2, q1))
            || (d4 == 0f && IsOnSegment(p1, p2, q2));
    }

    /// <summary>
    /// Gets the side of point <paramref name="p"/> relative to the line a-b: 1, -1, or 0 if it lies on the line.
    /// </summary>
    /// <param name="a">The first point of the line.</param>
    /// <param name="b">The second point of the line.</param>
    /// <param name="p">The point to test.</param>
    /// <param name="normal">The unit normal of the polygon plane.</param>
    /// <param name="tolerance">The distance below which a point counts as lying on a line.</param>
    private static int Side(Vector3 a, Vector3 b, Vector3 p, Vector3 normal, float tolerance)
    {
        float value = Vector3.Dot(Vector3.Cross(b - a, p - a), normal);

        if (MathF.Abs(value) <= tolerance)
        {
            return 0;
        }

        return value > 0f ? 1 : -1;
    }

    /// <summary>
    /// Determines whether a point that lies on the line through a and b is within the segment a-b.
    /// </summary>
    /// <param name="a">The first endpoint of the segment.</param>
    /// <param name="b">The second endpoint of the segment.</param>
    /// <param name="p">The point to test.</param>
    private static bool IsOnSegment(Vector3 a, Vector3 b, Vector3 p)
    {
        return MathF.Min(a.X, b.X) <= p.X
            && p.X <= MathF.Max(a.X, b.X)
            && MathF.Min(a.Y, b.Y) <= p.Y
            && p.Y <= MathF.Max(a.Y, b.Y)
            && MathF.Min(a.Z, b.Z) <= p.Z
            && p.Z <= MathF.Max(a.Z, b.Z);
    }

    /// <summary>
    /// Adds one triangle to the current group.
    /// </summary>
    private void EmitTriangle(FaceCorner a, FaceCorner b, FaceCorner c)
    {
        _currentGroup.Corners.Add(a);
        _currentGroup.Corners.Add(b);
        _currentGroup.Corners.Add(c);
    }

    /// <summary>
    /// Determines whether the corner at <paramref name="curr"/> can be clipped as an ear.
    /// </summary>
    private static bool IsEar(
        Vector3[] points,
        List<int> remaining,
        int prev,
        int curr,
        int next,
        Vector3 normal
    )
    {
        Vector3 a = points[prev];
        Vector3 b = points[curr];
        Vector3 c = points[next];

        // A reflex corner cannot be an ear.
        if (Vector3.Dot(Vector3.Cross(b - a, c - b), normal) < 0f)
        {
            return false;
        }

        foreach (int other in remaining)
        {
            if (other == prev || other == curr || other == next)
            {
                continue;
            }

            if (IsInsideTriangle(points[other], a, b, c, normal))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether a point lies inside or on the edge of a triangle in the polygon's plane.
    /// </summary>
    private static bool IsInsideTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
    {
        return Vector3.Dot(Vector3.Cross(b - a, p - a), normal) >= 0f
            && Vector3.Dot(Vector3.Cross(c - b, p - b), normal) >= 0f
            && Vector3.Dot(Vector3.Cross(a - c, p - c), normal) >= 0f;
    }

    /// <summary>
    /// Calculates the polygon normal with Newell's method. Its length is twice the polygon area.
    /// </summary>
    private static Vector3 NewellNormal(Vector3[] points)
    {
        var normal = Vector3.Zero;

        for (int i = 0; i < points.Length; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[(i + 1) % points.Length];

            normal.X += (a.Y - b.Y) * (a.Z + b.Z);
            normal.Y += (a.Z - b.Z) * (a.X + b.X);
            normal.Z += (a.X - b.X) * (a.Y + b.Y);
        }

        return normal;
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
