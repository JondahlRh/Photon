// -----------------------------------------------------------------------------
// Author:        Luca Lengl, Jondahl Rhenius
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------

using System.Numerics;
using Photon.Core.Imaging;
using Photon.Core.InputOutput;
using Photon.Core.Mathematics;
using Photon.Core.Rendering;
using Photon.Core.Scenes;

namespace Photon.Core.Tests.Rendering;

public sealed class RayTracerTests
{
    private const int Size = 10;

    private const float ColorTolerance = 1e-4f;

    private static readonly Vector3 _background = new(0.1f, 0.2f, 0.3f);

    private static readonly RenderSettings _settings = new(_background);

    [Fact]
    public void CenterPixelShowsTriangleNormalAsColor()
    {
        FrameBuffer target = Render(CreateScene(CreateTriangle(-10f, Vector3.UnitZ)));

        // Normal (0, 0, 1) remapped from [-1, 1] to [0, 1].
        AssertColor(new Vector3(0.5f, 0.5f, 1f), target.GetPixel(Size / 2, Size / 2));
    }

    [Fact]
    public void CornerPixelShowsBackground()
    {
        FrameBuffer target = Render(CreateScene(CreateTriangle(-10f, Vector3.UnitZ)));

        AssertColor(_background, target.GetPixel(0, 0));
    }

    [Fact]
    public void AllRowsAreCompleted()
    {
        FrameBuffer target = Render(CreateScene(CreateTriangle(-10f, Vector3.UnitZ)));

        Assert.True(target.IsComplete);
    }

    [Fact]
    public void NearestTriangleWins()
    {
        // The far triangle comes first, so the renderer must not stop at the first hit.
        Scene scene = CreateScene(
            CreateTriangle(-10f, Vector3.UnitZ),
            CreateTriangle(-5f, Vector3.UnitX)
        );

        FrameBuffer target = Render(scene);

        AssertColor(new Vector3(1f, 0.5f, 0.5f), target.GetPixel(Size / 2, Size / 2));
    }

    /// <summary>
    /// Checks that BVH rendering matches brute-force rendering for every pixel.
    /// </summary>
    [Fact]
    public void BvhMatchesBruteForcePixels()
    {
        Scene scene = CreateScene(
            CreateTriangle(-10f, Vector3.UnitZ),
            CreateTriangle(-7f, Vector3.UnitX),
            CreateTriangle(-4f, -Vector3.UnitZ),
            CreateTriangle(-12f, Vector3.UnitY),
            CreateTriangle(-2f, -Vector3.UnitX)
        );

        FrameBuffer bvhBuffer = Render(scene);
        FrameBuffer bruteForceBuffer = RenderBruteForce(scene);

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                AssertColor(
                    bruteForceBuffer.GetPixel(x, y),
                    bvhBuffer.GetPixel(x, y)
                );
            }
        }
    }

    [Fact]
    public void BufferSizeMismatchThrows()
    {
        Scene scene = CreateScene(CreateTriangle(-10f, Vector3.UnitZ));
        var target = new FrameBuffer(Size + 1, Size);

        Assert.Throws<ArgumentException>(() =>
            new RayTracer().Render(scene, _settings, target, TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public void CancelledRenderThrowsAndCompletesNoRow()
    {
        Scene scene = CreateScene(CreateTriangle(-10f, Vector3.UnitZ));
        var target = new FrameBuffer(Size, Size);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            new RayTracer().Render(scene, _settings, target, cancellation.Token)
        );
        Assert.Equal(0, target.CompletedRowCount);
    }

    [Fact]
    public void ExampleCubeRendersAndIsExported()
    {
        const int imageSize = 64;
        Model model = ObjMtlImporter
            .Load(Path.Combine(FindRepositoryRoot(), "examples", "cube.obj"))
            .Model;

        // Image plane at z = 5 spans 4 units; the projection center lies 4 units in front of it.
        Matrix4x4 viewToWorld =
            Matrix4x4.CreateScale(4f / imageSize) * Matrix4x4.CreateTranslation(0f, 0f, 5f);
        var camera = new Camera(viewToWorld, imageSize, imageSize, new Vector3(0f, 0f, imageSize));
        var scene = new Scene(model, new Lighting(Vector3.Zero, []), camera);
        var target = new FrameBuffer(imageSize, imageSize);

        new RayTracer().Render(scene, _settings, target, TestContext.Current.CancellationToken);

        Assert.True(target.IsComplete);
        Assert.True(
            Vector3.Distance(_background, target.GetPixel(imageSize / 2, imageSize / 2))
                > ColorTolerance
        );
        AssertColor(_background, target.GetPixel(0, 0));

        string path = Path.Combine(AppContext.BaseDirectory, "photon-cube.ppm");
        PpmWriter.Write(target, path);
        Assert.True(File.Exists(path));
    }

    private static FrameBuffer Render(Scene scene)
    {
        var target = new FrameBuffer(Size, Size);
        new RayTracer().Render(scene, _settings, target, TestContext.Current.CancellationToken);
        return target;
    }

    /// <summary>
    /// Renders a scene by testing every primitive for every pixel.
    /// </summary>
    /// <param name="scene">The scene to render.</param>
    /// <returns>The brute-force frame buffer.</returns>
    private static FrameBuffer RenderBruteForce(Scene scene)
    {
        var target = new FrameBuffer(Size, Size);

        for (int y = 0; y < target.Height; y++)
        {
            for (int x = 0; x < target.Width; x++)
            {
                var ray = scene.Camera.CreatePrimaryRay(x, y);
                var closestDistance = float.MaxValue;
                HitInfo? closestHit = null;

                foreach (IPrimitive primitive in scene.Model.Primitives)
                {
                    bool intersects = primitive.Intersect(
                        ray,
                        FloatComparison.Epsilon,
                        closestDistance,
                        out HitInfo hit
                    );

                    if (!intersects)
                    {
                        continue;
                    }

                    closestDistance = hit.Distance;
                    closestHit = hit;
                }

                if (!closestHit.HasValue)
                {
                    target.SetPixel(x, y, _settings.BackgroundColor);
                    continue;
                }

                var color = (closestHit.Value.ShadingNormal + Vector3.One) * 0.5f;
                target.SetPixel(x, y, color);
            }

            target.MarkRowCompleted(y);
        }

        return target;
    }

    private static Scene CreateScene(params Mesh[] meshes)
    {
        var camera = new Camera(Matrix4x4.Identity, Size, Size, new Vector3(0f, 0f, 10f));
        return new Scene(new Model(meshes), new Lighting(Vector3.Zero, []), camera);
    }

    private static Mesh CreateTriangle(float z, Vector3 normal)
    {
        return new Mesh(
            [new Vector3(-3f, -3f, z), new Vector3(3f, -3f, z), new Vector3(0f, 3f, z)],
            [normal, normal, normal],
            [0, 1, 2],
            Material.Default
        );
    }

    private static void AssertColor(Vector3 expected, Vector3 actual)
    {
        Assert.True(
            Vector3.Distance(expected, actual) < ColorTolerance,
            $"Expected {expected}, got {actual}."
        );
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (
            directory is not null && !File.Exists(Path.Combine(directory.FullName, "Photon.slnx"))
        )
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Photon.slnx was not found.");
    }
}
