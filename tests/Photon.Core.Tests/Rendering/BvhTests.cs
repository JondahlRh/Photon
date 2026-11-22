// -----------------------------------------------------------------------------
// Author:        Jondahl Rhenius
// AI Assistance: Copilot for Information, Bug and Warning
// -----------------------------------------------------------------------------

using System.Numerics;
using Photon.Core.Mathematics;
using Photon.Core.Rendering;
using Photon.Core.Scenes;

namespace Photon.Core.Tests.Rendering;

/// <summary>
/// Verifies BVH construction, traversal and distance range handling.
/// </summary>
public sealed class BvhTests
{
    /// <summary>
    /// Checks that an empty scene has no intersection.
    /// </summary>
    [Fact]
    public void EmptySceneMisses()
    {
        var bvh = new Bvh(CreateScene());
        var ray = new Ray(new Vector3(0f, 0f, 10f), -Vector3.UnitZ);

        bool foundHit = bvh.Intersect(
            ray,
            FloatComparison.Epsilon,
            float.MaxValue,
            out HitInfo hit
        );

        Assert.False(foundHit);
        Assert.Equal(0f, hit.Distance);
    }

    /// <summary>
    /// Checks that a ray miss does not return a hit.
    /// </summary>
    [Fact]
    public void RayMissingPrimitivesMisses()
    {
        var bvh = new Bvh(CreateScene(CreateTriangle(-5f, Vector3.UnitZ)));
        var ray = new Ray(new Vector3(5f, 5f, 10f), -Vector3.UnitZ);

        bool foundHit = bvh.Intersect(
            ray,
            FloatComparison.Epsilon,
            float.MaxValue,
            out HitInfo hit
        );

        Assert.False(foundHit);
        Assert.Equal(0f, hit.Distance);
    }

    /// <summary>
    /// Checks that the nearest primitive is returned across multiple leaves.
    /// </summary>
    [Fact]
    public void ReturnsNearestPrimitiveAcrossLeaves()
    {
        var bvh = new Bvh(
            CreateScene(
                CreateTriangle(-2f, Vector3.UnitZ),
                CreateTriangle(-4f, Vector3.UnitZ),
                CreateTriangle(-6f, Vector3.UnitZ),
                CreateTriangle(-8f, Vector3.UnitZ),
                CreateTriangle(-10f, Vector3.UnitZ)
            )
        );
        var ray = new Ray(new Vector3(0f, 0f, 10f), -Vector3.UnitZ);

        bool foundHit = bvh.Intersect(
            ray,
            FloatComparison.Epsilon,
            float.MaxValue,
            out HitInfo hit
        );

        Assert.True(foundHit);
        Assert.True(FloatComparison.NearlyEqual(12f, hit.Distance));
    }

    /// <summary>
    /// Checks that an intersection outside the requested distance range is ignored.
    /// </summary>
    [Fact]
    public void HitOutsideDistanceRangeMisses()
    {
        var bvh = new Bvh(CreateScene(CreateTriangle(-5f, Vector3.UnitZ)));
        var ray = new Ray(new Vector3(0f, 0f, 10f), -Vector3.UnitZ);

        bool foundHit = bvh.Intersect(ray, FloatComparison.Epsilon, 10f, out _);

        Assert.False(foundHit);
    }

    /// <summary>
    /// Checks that invalid or reversed distance ranges are rejected.
    /// </summary>
    [Theory]
    [InlineData(-1f, 10f)]
    [InlineData(10f, 5f)]
    public void InvalidDistanceRangeMisses(float minDistance, float maxDistance)
    {
        var bvh = new Bvh(CreateScene(CreateTriangle(-5f, Vector3.UnitZ)));
        var ray = new Ray(new Vector3(0f, 0f, 10f), -Vector3.UnitZ);

        Assert.False(bvh.Intersect(ray, minDistance, maxDistance, out _));
    }

    /// <summary>
    /// Checks that non-finite distance ranges are rejected.
    /// </summary>
    [Fact]
    public void NonFiniteDistanceRangeMisses()
    {
        var bvh = new Bvh(CreateScene(CreateTriangle(-5f, Vector3.UnitZ)));
        var ray = new Ray(new Vector3(0f, 0f, 10f), -Vector3.UnitZ);

        Assert.False(bvh.Intersect(ray, float.NaN, 10f, out _));
        Assert.False(bvh.Intersect(ray, 0f, float.PositiveInfinity, out _));
    }

    /// <summary>
    /// Creates a scene with the specified triangle meshes.
    /// </summary>
    /// <param name="meshes">The meshes contained by the scene.</param>
    /// <returns>The scene used by a BVH test.</returns>
    private static Scene CreateScene(params Mesh[] meshes)
    {
        var camera = new Camera(Matrix4x4.Identity, 10, 10, new Vector3(0f, 0f, 10f));
        return new Scene(new Model(meshes), new Lighting(Vector3.Zero, []), camera);
    }

    /// <summary>
    /// Creates a triangle at the specified Z coordinate.
    /// </summary>
    /// <param name="z">The Z coordinate of the triangle plane.</param>
    /// <param name="normal">The triangle normal.</param>
    /// <returns>The triangle mesh.</returns>
    private static Mesh CreateTriangle(float z, Vector3 normal)
    {
        return new Mesh(
            [new Vector3(-3f, -3f, z), new Vector3(3f, -3f, z), new Vector3(0f, 3f, z)],
            [normal, normal, normal],
            [0, 1, 2],
            Material.Default
        );
    }
}
