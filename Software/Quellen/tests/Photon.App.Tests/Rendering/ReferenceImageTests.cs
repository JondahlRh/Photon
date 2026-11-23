// -----------------------------------------------------------------------------
// Author:        XISander
// AI Assistance: Claude Code for test cases
// -----------------------------------------------------------------------------

using System.Numerics;
using Photon.App.Platform.Skia;
using Photon.Core.Imaging;
using Photon.Core.InputOutput;
using Photon.Core.Mathematics;
using Photon.Core.Rendering;
using Photon.Core.Scenes;

namespace Photon.App.Tests.Rendering;

/// <summary>
/// Renders the example cube on a ground plane with two point lights and saves it as PNG,
/// so the lighting can be checked by eye. The file lands next to the test assembly in
/// <c>ReferenceImages/</c>.
/// </summary>
public sealed class ReferenceImageTests
{
    private const int Width = 640;

    private const int Height = 480;

    private static readonly Vector3 _background = new(0.05f, 0.07f, 0.1f);

    // The main light sits to the right, above and in front of the cube (+x, +y, +z).
    private static readonly PointLight _mainLight = new(
        new Vector3(4f, 6f, 3f),
        new Vector3(0.75f, 0.75f, 0.7f)
    );

    // A weaker, bluish fill light from the left.
    private static readonly PointLight _fillLight = new(
        new Vector3(-4f, 5f, 4f),
        new Vector3(0.3f, 0.3f, 0.4f)
    );

    // On the ground, behind the cube as seen from the main light, but reached by the fill light.
    private static readonly Vector3 _shadowedGroundPoint = new(-2f, -1f, -1.5f);

    // On the ground, reached by both lights.
    private static readonly Vector3 _litGroundPoint = new(2.5f, -1f, 2.5f);

    [Fact]
    public async Task CubeOnPlaneWithTwoLightsIsRenderedAndSaved()
    {
        Model model = ObjMtlImporter
            .Load(Path.Combine(FindRepositoryRoot(), "examples", "cube-on-plane.obj"))
            .Model;
        var camera = Camera.FrameBounds(model.Bounds, Width, Height);
        var lighting = new Lighting(new Vector3(0.15f), [_mainLight, _fillLight]);
        var scene = new Scene(model, lighting, camera);
        var target = new FrameBuffer(Width, Height);

        new RayTracer().Render(
            scene,
            new RenderSettings(_background),
            target,
            TestContext.Current.CancellationToken
        );

        string directory = Path.Combine(AppContext.BaseDirectory, "ReferenceImages");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "phong-cube-on-plane.png");
        await new SkiaImageExporter().ExportPngAsync(target, path);
        TestContext.Current.TestOutputHelper?.WriteLine($"Reference image: {path}");

        Assert.True(target.IsComplete);
        Assert.True(new FileInfo(path).Length > 0);

        Vector3 shadowed = PixelAt(target, camera, scene, _shadowedGroundPoint);
        Vector3 lit = PixelAt(target, camera, scene, _litGroundPoint);
        Assert.True(
            Brightness(shadowed) < Brightness(lit) * 0.6f,
            $"Shadow {shadowed} is not clearly darker than lit ground {lit}."
        );

        // The fill light still reaches the shadow, so it is brighter than ambient light alone.
        Vector3 ambientOnly = new Vector3(0.7f) * lighting.AmbientIntensity;
        Assert.True(Brightness(shadowed) > Brightness(ambientOnly) + 0.05f);
    }

    /// <summary>
    /// Reads the pixel that shows the given world point and checks that the point is not hidden.
    /// </summary>
    private static Vector3 PixelAt(FrameBuffer target, Camera camera, Scene scene, Vector3 point)
    {
        Assert.True(Matrix4x4.Invert(camera.ViewToWorld, out Matrix4x4 worldToView));

        // Central projection onto the image plane z = 0 through the projection center.
        Vector3 view = Vector3.Transform(point, worldToView);
        Vector3 center = camera.ProjectionCenter;
        Vector3 onImagePlane = center + ((view - center) * (center.Z / (center.Z - view.Z)));
        int x = (int)MathF.Floor(onImagePlane.X + (Width * 0.5f));
        int y = (int)MathF.Floor((Height * 0.5f) - onImagePlane.Y);

        var bvh = new Bvh(scene);
        Assert.True(
            bvh.Intersect(
                camera.CreatePrimaryRay(x, y),
                FloatComparison.Epsilon,
                float.MaxValue,
                out HitInfo hit
            )
        );
        Assert.True(
            Vector3.Distance(point, hit.Point) < 0.1f,
            $"{point} is hidden at {hit.Point}."
        );

        return target.GetPixel(x, y);
    }

    private static float Brightness(Vector3 color)
    {
        return color.X + color.Y + color.Z;
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
