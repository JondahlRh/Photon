// -----------------------------------------------------------------------------
// Author:        Luca Lengl
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------

using System.Numerics;
using Photon.Core.Mathematics;
using Photon.Core.Scenes;

namespace Photon.Core.Tests.Scenes;

public sealed class CameraTests
{
    [Fact]
    public void IdentityViewLooksAlongNegativeW()
    {
        var camera = new Camera(Matrix4x4.Identity, 100, 50, new Vector3(0f, 0f, 100f));

        // Exact image center between the two middle pixels.
        Ray ray = camera.CreatePrimaryRay(50f, 25f);

        Assert.Equal(new Vector3(0f, 0f, 100f), ray.Origin);
        Assert.True(FloatComparison.NearlyEqual(-1f, ray.Direction.Z));
    }

    [Fact]
    public void TopLeftPixelPointsLeftAndUp()
    {
        var camera = new Camera(Matrix4x4.Identity, 100, 50, new Vector3(0f, 0f, 100f));

        Ray ray = camera.CreatePrimaryRay(0, 0);

        Assert.True(ray.Direction.X < 0f);
        Assert.True(ray.Direction.Y > 0f);
    }

    [Fact]
    public void ImageEdgeMatchesProjectionCenterDistance()
    {
        // Projection center 50 pixels in front of a 100 pixel high image: the top edge
        // is 50 pixels up, so the ray through it rises at exactly 45 degrees.
        var camera = new Camera(Matrix4x4.Identity, 200, 100, new Vector3(0f, 0f, 50f));

        Ray ray = camera.CreatePrimaryRay(100f, 0f);

        Assert.True(FloatComparison.NearlyEqual(ray.Direction.Y, -ray.Direction.Z, 1e-4f));
        Assert.True(FloatComparison.NearlyEqual(0f, ray.Direction.X, 1e-4f));
    }

    [Fact]
    public void ViewToWorldMatrixMovesAndScalesView()
    {
        // One pixel is 0.01 world units, the image center is at (1, 2, 3).
        Matrix4x4 viewToWorld =
            Matrix4x4.CreateScale(0.01f) * Matrix4x4.CreateTranslation(1f, 2f, 3f);
        var camera = new Camera(viewToWorld, 100, 100, new Vector3(0f, 0f, 100f));

        Ray ray = camera.CreatePrimaryRay(50f, 50f);

        Assert.True(Vector3.Distance(new Vector3(1f, 2f, 4f), camera.Position) < 1e-4f);
        Assert.True(Vector3.Distance(-Vector3.UnitZ, ray.Direction) < 1e-4f);
    }

    [Fact]
    public void ProjectionCenterBehindImagePlaneThrows()
    {
        Assert.Throws<ArgumentException>(() =>
            new Camera(Matrix4x4.Identity, 100, 100, new Vector3(0f, 0f, -1f))
        );
    }

    [Fact]
    public void NonInvertibleMatrixThrows()
    {
        Assert.Throws<ArgumentException>(() =>
            new Camera(new Matrix4x4(), 100, 100, new Vector3(0f, 0f, 1f))
        );
    }

    [Fact]
    public void ProjectionMatrixThrows()
    {
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(1f, 1f, 0.1f, 10f);

        Assert.Throws<ArgumentException>(() =>
            new Camera(projection, 100, 100, new Vector3(0f, 0f, 1f))
        );
    }
}
