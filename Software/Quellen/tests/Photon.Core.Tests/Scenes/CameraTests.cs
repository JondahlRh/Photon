// -----------------------------------------------------------------------------
// Author:        Luca Lengl
// AI Assistance: Claude for Information, Bug and Warning; ChatGPT Codex for automatic framing regression tests
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

    [Theory]
    [InlineData(1f, 0f, 0f, 0f, 1280, 720)]
    [InlineData(100f, 200f, 150f, 60f, 1280, 720)]
    [InlineData(0.01f, 0f, 0f, 0f, 1280, 720)]
    [InlineData(100f, -200f, -150f, -60f, 720, 1280)]
    [InlineData(1f, 0f, 0f, 0f, 64, 1024)]
    public void FrameBoundsKeepsAllCornersWithinImage(
        float scale,
        float x,
        float y,
        float z,
        int width,
        int height
    )
    {
        var center = new Vector3(x, y, z);
        Vector3 halfSize = new Vector3(2f, 3f, 0.5f) * scale;
        var bounds = new Aabb(center - halfSize, center + halfSize);

        var camera = Camera.FrameBounds(bounds, width, height);

        Ray centerRay = camera.CreatePrimaryRay(width * 0.5f, height * 0.5f);
        Vector3 expectedDirection = Vector3.Normalize(bounds.Center - camera.Position);
        Assert.True(Vector3.Distance(expectedDirection, centerRay.Direction) < 1e-4f);
        Assert.True(Matrix4x4.Invert(camera.ViewToWorld, out Matrix4x4 worldToView));

        // Project every corner onto the camera's image plane; no model edge may be cropped.
        foreach (float cornerX in new[] { bounds.Min.X, bounds.Max.X })
        {
            foreach (float cornerY in new[] { bounds.Min.Y, bounds.Max.Y })
            {
                foreach (float cornerZ in new[] { bounds.Min.Z, bounds.Max.Z })
                {
                    Vector3 point = Vector3.Transform(
                        new Vector3(cornerX, cornerY, cornerZ),
                        worldToView
                    );
                    float depth = camera.ProjectionCenter.Z - point.Z;
                    Assert.True(depth > 0f);
                    float perspectiveScale = camera.ProjectionCenter.Z / depth;
                    Assert.InRange(point.X * perspectiveScale, -width * 0.5f, width * 0.5f);
                    Assert.InRange(point.Y * perspectiveScale, -height * 0.5f, height * 0.5f);
                }
            }
        }
    }

    [Fact]
    public void FrameBoundsRejectsEmptyModel()
    {
        Assert.Throws<ArgumentException>(() => Camera.FrameBounds(Aabb.Empty, 100, 100));
    }

    [Theory]
    [InlineData(0f, 0f, 1280, 720)]
    [InlineData(90f, 0f, 1280, 720)]
    [InlineData(-90f, 0f, 1280, 720)]
    [InlineData(180f, 0f, 720, 1280)]
    [InlineData(90f, 90f, 720, 1280)]
    public void QuaternionCameraMatchesPreviewRaysAtPolesAndWhenRolled(
        float pitchDegrees,
        float rollDegrees,
        int width,
        int height
    )
    {
        var bounds = new Aabb(new Vector3(60f, -40f, 25f), new Vector3(310f, 320f, 100f));
        Quaternion orientation = Quaternion.CreateFromRotationMatrix(
            Matrix4x4.CreateRotationX(pitchDegrees * MathF.PI / 180f)
                * Matrix4x4.CreateRotationZ(rollDegrees * MathF.PI / 180f)
        );
        var camera = Camera.FrameBounds(bounds, width, height, orientation);
        Matrix4x4 view = Matrix4x4.CreateLookAt(camera.Position, bounds.Center, camera.Up);

        Assert.True(Matrix4x4.Invert(view, out Matrix4x4 cameraToWorld));

        foreach (
            (int x, int y) in new[] { (0, 0), (width / 2, height / 2), (width - 1, height - 1) }
        )
        {
            float tangent = MathF.Tan(MathF.PI / 6f);
            var localDirection = new Vector3(
                (2f * (x + 0.5f) / width - 1f) * width / height * tangent,
                (1f - 2f * (y + 0.5f) / height) * tangent,
                -1f
            );
            Vector3 previewDirection = Vector3.Normalize(
                Vector3.TransformNormal(localDirection, cameraToWorld)
            );

            Assert.True(VectorMath.IsFinite(previewDirection));
            Assert.True(
                Vector3.Distance(previewDirection, camera.CreatePrimaryRay(x, y).Direction) < 1e-4f
            );
        }
    }

    [Theory]
    [InlineData(0f, 0f, 0f, 0f)]
    [InlineData(float.NaN, 0f, 0f, 1f)]
    [InlineData(0f, float.PositiveInfinity, 0f, 1f)]
    public void FrameBoundsRejectsInvalidQuaternion(float x, float y, float z, float w)
    {
        var bounds = new Aabb(-Vector3.One, Vector3.One);

        Assert.Throws<ArgumentException>(() =>
            Camera.FrameBounds(bounds, 100, 100, new Quaternion(x, y, z, w))
        );
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
