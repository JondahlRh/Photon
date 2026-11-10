// -----------------------------------------------------------------------------
// Author:        Luca Lengl
// AI Assistance: Claude for Information, Warning and Bugs.
// -----------------------------------------------------------------------------

using System.Numerics;
using Photon.Core.Mathematics;

namespace Photon.Core.Tests.Mathematics;

public sealed class AabbTests
{
    private static readonly Aabb _unitBox = new(-Vector3.One, Vector3.One);

    [Fact]
    public void EmptyBoxBecomesPointWhenIncludingPoint()
    {
        var point = new Vector3(1f, 2f, 3f);

        Aabb box = Aabb.Empty.Include(point);

        Assert.True(Aabb.Empty.IsEmpty);
        Assert.False(box.IsEmpty);
        Assert.Equal(point, box.Min);
        Assert.Equal(point, box.Max);
    }

    [Fact]
    public void RayThroughBoxIntersects()
    {
        var ray = new Ray(new Vector3(0f, 0f, 5f), -Vector3.UnitZ);

        Assert.True(_unitBox.Intersects(ray, Inverse(ray), 0f, float.MaxValue));
    }

    [Fact]
    public void RayPassingBesideBoxMisses()
    {
        var ray = new Ray(new Vector3(2f, 0f, 5f), -Vector3.UnitZ);

        Assert.False(_unitBox.Intersects(ray, Inverse(ray), 0f, float.MaxValue));
    }

    [Fact]
    public void BoxBeyondMaxDistanceMisses()
    {
        var ray = new Ray(new Vector3(0f, 0f, 5f), -Vector3.UnitZ);

        Assert.False(_unitBox.Intersects(ray, Inverse(ray), 0f, 3f));
    }

    [Fact]
    public void AxisParallelRayInsideSlabIntersects()
    {
        // Direction components of zero give infinite inverse values.
        var ray = new Ray(new Vector3(0.5f, 0.5f, 5f), -Vector3.UnitZ);

        Assert.True(_unitBox.Intersects(ray, Inverse(ray), 0f, float.MaxValue));
    }

    private static Vector3 Inverse(Ray ray) => Vector3.One / ray.Direction;
}
