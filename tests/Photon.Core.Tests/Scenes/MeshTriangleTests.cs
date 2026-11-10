// -----------------------------------------------------------------------------
// Author:        Luca Lengl
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------

using System.Numerics;
using Photon.Core.Mathematics;
using Photon.Core.Scenes;

namespace Photon.Core.Tests.Scenes;

public sealed class MeshTriangleTests
{
    [Fact]
    public void RayHitsTriangleAtExpectedDistance()
    {
        MeshTriangle triangle = CreateFlatTriangle();
        var ray = new Ray(new Vector3(0.2f, 0.2f, 5f), -Vector3.UnitZ);

        bool hit = triangle.Intersect(
            ray,
            FloatComparison.Epsilon,
            float.MaxValue,
            out HitInfo info
        );

        Assert.True(hit);
        Assert.True(FloatComparison.NearlyEqual(5f, info.Distance));
        Assert.True(FloatComparison.NearlyEqual(0f, info.Point.Z));
        Assert.Equal(Vector3.UnitZ, info.GeometricNormal);
        Assert.Same(Material.Default, info.Material);
    }

    [Fact]
    public void RayOutsideTriangleMisses()
    {
        MeshTriangle triangle = CreateFlatTriangle();
        var ray = new Ray(new Vector3(0.8f, 0.8f, 5f), -Vector3.UnitZ);

        Assert.False(triangle.Intersect(ray, FloatComparison.Epsilon, float.MaxValue, out _));
    }

    [Fact]
    public void TriangleBehindRayMisses()
    {
        MeshTriangle triangle = CreateFlatTriangle();
        var ray = new Ray(new Vector3(0.2f, 0.2f, 5f), Vector3.UnitZ);

        Assert.False(triangle.Intersect(ray, FloatComparison.Epsilon, float.MaxValue, out _));
    }

    [Fact]
    public void HitBeyondMaxDistanceIsIgnored()
    {
        MeshTriangle triangle = CreateFlatTriangle();
        var ray = new Ray(new Vector3(0.2f, 0.2f, 5f), -Vector3.UnitZ);

        Assert.False(triangle.Intersect(ray, FloatComparison.Epsilon, 4f, out _));
    }

    [Fact]
    public void ShadingNormalIsInterpolatedFromCornerNormals()
    {
        // Corner 0 points along +Z, corners 1 and 2 along +X; at corner 0 only its normal counts.
        var mesh = new Mesh(
            [Vector3.Zero, Vector3.UnitX, Vector3.UnitY],
            [Vector3.UnitZ, Vector3.UnitX, Vector3.UnitX],
            [0, 1, 2],
            Material.Default
        );
        var ray = new Ray(new Vector3(0.001f, 0.001f, 1f), -Vector3.UnitZ);

        bool hit = mesh.Triangles[0]
            .Intersect(ray, FloatComparison.Epsilon, float.MaxValue, out HitInfo info);

        Assert.True(hit);
        Assert.True(info.ShadingNormal.Z > 0.99f);
        Assert.True(FloatComparison.NearlyEqual(1f, info.ShadingNormal.Length()));
    }

    [Fact]
    public void BoundsContainAllCorners()
    {
        MeshTriangle triangle = CreateFlatTriangle();

        Assert.Equal(Vector3.Zero, triangle.Bounds.Min);
        Assert.Equal(new Vector3(1f, 1f, 0f), triangle.Bounds.Max);
    }

    private static MeshTriangle CreateFlatTriangle()
    {
        var mesh = new Mesh(
            [Vector3.Zero, Vector3.UnitX, Vector3.UnitY],
            [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
            [0, 1, 2],
            Material.Default
        );
        return mesh.Triangles[0];
    }
}
