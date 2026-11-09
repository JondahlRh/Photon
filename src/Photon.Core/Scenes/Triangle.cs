// -----------------------------------------------------------------------------
// Author:        Luca Lengl
// AI Assistance: Claude for Information, Bug and Warning and the Möller-Trumbore intersection
// -----------------------------------------------------------------------------

using System.Numerics;
using Photon.Core.Mathematics;

namespace Photon.Core.Scenes;

/// <summary>
/// A triangle of a <see cref="Mesh"/>. It references its corners by index into the mesh arrays.
/// </summary>
public sealed class Triangle : IPrimitive
{
    /// <summary>
    /// The mesh that owns the vertex and normal arrays.
    /// </summary>
    private readonly Mesh _mesh;

    /// <summary>
    /// Initializes a new triangle. Only <see cref="Mesh"/> creates triangles.
    /// </summary>
    /// <param name="mesh">The mesh that owns the vertex and normal arrays.</param>
    /// <param name="index0">The index of the first corner.</param>
    /// <param name="index1">The index of the second corner.</param>
    /// <param name="index2">The index of the third corner.</param>
    internal Triangle(Mesh mesh, int index0, int index1, int index2)
    {
        _mesh = mesh;
        Index0 = index0;
        Index1 = index1;
        Index2 = index2;
        Bounds = Aabb.FromPoints(Vertex0, Vertex1, Vertex2);
    }

    /// <summary>
    /// Gets the index of the first corner in <see cref="Mesh.Vertices"/> and <see cref="Mesh.Normals"/>.
    /// </summary>
    public int Index0 { get; }

    /// <summary>
    /// Gets the index of the second corner in <see cref="Mesh.Vertices"/> and <see cref="Mesh.Normals"/>.
    /// </summary>
    public int Index1 { get; }

    /// <summary>
    /// Gets the index of the third corner in <see cref="Mesh.Vertices"/> and <see cref="Mesh.Normals"/>.
    /// </summary>
    public int Index2 { get; }

    /// <summary>
    /// Gets the position of the first corner in world coordinates.
    /// </summary>
    public Vector3 Vertex0 => _mesh.Vertices[Index0];

    /// <summary>
    /// Gets the position of the second corner in world coordinates.
    /// </summary>
    public Vector3 Vertex1 => _mesh.Vertices[Index1];

    /// <summary>
    /// Gets the position of the third corner in world coordinates.
    /// </summary>
    public Vector3 Vertex2 => _mesh.Vertices[Index2];

    /// <summary>
    /// Gets the material of the owning mesh.
    /// </summary>
    public Material Material => _mesh.Material;

    /// <inheritdoc/>
    public Aabb Bounds { get; }

    /// <inheritdoc/>
    public bool Intersect(in Ray ray, float minDistance, float maxDistance, out HitInfo hit)
    {
        hit = default;

        Vector3 v0 = Vertex0;
        Vector3 edge1 = Vertex1 - v0;
        Vector3 edge2 = Vertex2 - v0;

        // Möller-Trumbore: solve O + t*D = v0 + u*edge1 + v*edge2 with Cramer's rule.
        Vector3 p = Vector3.Cross(ray.Direction, edge2);
        float determinant = Vector3.Dot(edge1, p);

        // Ray parallel to the triangle plane or degenerate triangle.
        if (MathF.Abs(determinant) < FloatComparison.Epsilon * FloatComparison.Epsilon)
        {
            return false;
        }

        float inverseDeterminant = 1f / determinant;
        Vector3 s = ray.Origin - v0;
        float u = Vector3.Dot(s, p) * inverseDeterminant;
        if (u < 0f || u > 1f)
        {
            return false;
        }

        Vector3 q = Vector3.Cross(s, edge1);
        float v = Vector3.Dot(ray.Direction, q) * inverseDeterminant;
        if (v < 0f || u + v > 1f)
        {
            return false;
        }

        float distance = Vector3.Dot(edge2, q) * inverseDeterminant;
        if (distance < minDistance || distance > maxDistance)
        {
            return false;
        }

        Vector3 geometricNormal = Vector3.Normalize(Vector3.Cross(edge1, edge2));

        // Phong shading: interpolate the corner normals with barycentric weights (1-u-v, u, v)
        // and normalize again, since the interpolated vector is generally shorter than 1.
        Vector3 interpolated =
            ((1f - u - v) * _mesh.Normals[Index0])
            + (u * _mesh.Normals[Index1])
            + (v * _mesh.Normals[Index2]);
        Vector3 shadingNormal = VectorMath.IsNearlyZero(interpolated)
            ? geometricNormal
            : Vector3.Normalize(interpolated);

        hit = new HitInfo(
            distance,
            ray.PointAt(distance),
            geometricNormal,
            shadingNormal,
            _mesh.Material
        );
        return true;
    }
}
