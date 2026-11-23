// -----------------------------------------------------------------------------
// Author:        Luca Lengl
// AI Assistance: Claude for information, bugs and warnings
// -----------------------------------------------------------------------------

using System.Numerics;
using Photon.Core.Mathematics;
using Photon.Core.Rendering;
using Photon.Core.Scenes;

namespace Photon.Core.Tests.Rendering;

public sealed class ShadowRaysTests
{
    private static readonly Vector3 _surfacePoint = Vector3.Zero;

    [Fact]
    public void SurfaceDoesNotShadowItself()
    {
        var structure = CreateStructure(CreateFloor(0f));

        Assert.True(IsLit(structure, lightHeight: 5f));
    }

    [Fact]
    public void LightBelowSurfaceIsNotBlockedBySurface()
    {
        var structure = CreateStructure(CreateFloor(0f));

        Assert.True(IsLit(structure, lightHeight: -5f));
    }

    [Fact]
    public void BlockerBetweenPointAndLightCastsShadow()
    {
        var structure = CreateStructure(CreateFloor(0f), CreateFloor(2f));

        Assert.False(IsLit(structure, lightHeight: 5f));
    }

    [Fact]
    public void BlockerBehindLightCastsNoShadow()
    {
        var structure = CreateStructure(CreateFloor(0f), CreateFloor(8f));

        Assert.True(IsLit(structure, lightHeight: 5f));
    }

    [Fact]
    public void ClosestHitIsNearestPrimitive()
    {
        var structure = CreateStructure(CreateFloor(0f), CreateFloor(2f));
        var ray = new Ray(new Vector3(0f, 5f, 0f), -Vector3.UnitY);

        bool found = structure.Intersect(
            ray,
            FloatComparison.Epsilon,
            float.MaxValue,
            out HitInfo hit
        );

        Assert.True(found);
        Assert.True(FloatComparison.NearlyEqual(3f, hit.Distance));
    }

    [Fact]
    public void EmptyStructureIsNeverOccluded()
    {
        var structure = new BruteForceAccelerationStructure([]);
        var ray = new Ray(Vector3.Zero, Vector3.UnitY);

        Assert.False(structure.IsOccluded(ray, FloatComparison.Epsilon, float.MaxValue));
    }

    [Fact]
    public void BvhBlockerBetweenPointAndLightCastsShadow()
    {
        var bvh = CreateBvh(
            CreateFloor(0f),
            CreateFloor(2f),
            CreateFloor(-2f),
            CreateFloor(-4f),
            CreateFloor(-6f),
            CreateFloor(-8f)
        );

        Assert.False(IsLit(bvh, lightHeight: 5f));
    }

    [Fact]
    public void BvhSurfaceDoesNotShadowItself()
    {
        var bvh = CreateBvh(
            CreateFloor(0f),
            CreateFloor(-2f),
            CreateFloor(-4f),
            CreateFloor(-6f),
            CreateFloor(-8f)
        );

        Assert.True(IsLit(bvh, lightHeight: 5f));
    }

    private static bool IsLit(IAccelerationStructure structure, float lightHeight)
    {
        // Hit the floor from the side of the light, like a primary ray from the camera would.
        float direction = MathF.Sign(lightHeight);
        var primaryRay = new Ray(new Vector3(0f, direction, 0f), new Vector3(0f, -direction, 0f));
        Assert.True(
            structure.Intersect(
                primaryRay,
                FloatComparison.Epsilon,
                float.MaxValue,
                out HitInfo hit
            )
        );
        Assert.True(VectorMath.IsNearlyZero(hit.Point - _surfacePoint));

        var light = new PointLight(new Vector3(0f, lightHeight, 0f), Vector3.One);
        return ShadowRays.IsLightVisible(structure, hit, light.Illuminate(hit.Point));
    }

    private static BruteForceAccelerationStructure CreateStructure(params Mesh[] meshes)
    {
        return new BruteForceAccelerationStructure(new Model(meshes).Primitives);
    }

    private static Bvh CreateBvh(params Mesh[] meshes)
    {
        // More than four primitives, so the hierarchy has inner nodes and not only one leaf.
        var camera = new Camera(Matrix4x4.Identity, 10, 10, new Vector3(0f, 0f, 10f));
        return new Bvh(new Scene(new Model(meshes), new Lighting(Vector3.Zero, []), camera));
    }

    private static Mesh CreateFloor(float height)
    {
        // The winding order gives a geometric normal of (0, -1, 0), i.e. away from a light above.
        return new Mesh(
            [
                new Vector3(-10f, height, -10f),
                new Vector3(10f, height, -10f),
                new Vector3(0f, height, 10f),
            ],
            [Vector3.UnitY, Vector3.UnitY, Vector3.UnitY],
            [0, 1, 2],
            Material.Default
        );
    }
}
