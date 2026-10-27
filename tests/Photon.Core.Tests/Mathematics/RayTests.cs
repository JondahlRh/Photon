// -----------------------------------------------------------------------------
// Author:        XISander
// AI Assistance: Claude for project skeleton
// -----------------------------------------------------------------------------

using System.Numerics;
using Photon.Core.Mathematics;

namespace Photon.Core.Tests.Mathematics;

public sealed class RayTests
{
    [Fact]
    public void ConstructorNormalizesDirection()
    {
        var ray = new Ray(Vector3.Zero, new Vector3(0f, 0f, -5f));

        Assert.True(FloatComparison.NearlyEqual(1f, ray.Direction.Length()));
        Assert.True(FloatComparison.NearlyEqual(-1f, ray.Direction.Z));
    }

    [Fact]
    public void ConstructorRejectsZeroDirection()
    {
        Assert.Throws<ArgumentException>(() => new Ray(Vector3.Zero, Vector3.Zero));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(1f)]
    [InlineData(2.5f)]
    public void PointAtMovesAlongDirection(float distance)
    {
        var ray = new Ray(new Vector3(1f, 2f, 3f), Vector3.UnitX);

        var point = ray.PointAt(distance);

        Assert.True(FloatComparison.NearlyEqual(1f + distance, point.X));
        Assert.True(FloatComparison.NearlyEqual(2f, point.Y));
        Assert.True(FloatComparison.NearlyEqual(3f, point.Z));
    }

    [Fact]
    public void CreateOffsetStartsAboveSurface()
    {
        var hitPoint = Vector3.Zero;
        var normal = Vector3.UnitY;

        var ray = Ray.CreateOffset(hitPoint, normal, Vector3.UnitY);

        Assert.True(ray.Origin.Y > hitPoint.Y);
        Assert.True(FloatComparison.NearlyEqual(FloatComparison.Epsilon, ray.Origin.Y, 1e-7f));
    }
}
