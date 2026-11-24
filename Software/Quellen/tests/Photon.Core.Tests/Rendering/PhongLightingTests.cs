// -----------------------------------------------------------------------------
// Author:        XISander
// AI Assistance: Claude Code for test cases
// -----------------------------------------------------------------------------

using System.Numerics;
using Photon.Core.Mathematics;
using Photon.Core.Rendering;
using Photon.Core.Scenes;

namespace Photon.Core.Tests.Rendering;

public sealed class PhongLightingTests
{
    private const float ColorTolerance = 1e-4f;

    private static readonly PhongLighting _lighting = new();

    private static readonly IAccelerationStructure _noOccluders =
        new BruteForceAccelerationStructure([]);

    // The viewer looks straight down at a floor in the plane y = 0, so V = (0, 1, 0).
    private static readonly Ray _viewRay = new(new Vector3(0f, 5f, 0f), -Vector3.UnitY);

    [Fact]
    public void AmbientIsDiffuseColorTimesSceneAmbient()
    {
        var material = CreateMaterial(diffuse: new Vector3(0.4f, 0.5f, 1f));
        var lighting = new Lighting(new Vector3(0.5f, 0.4f, 0.2f), []);

        Vector3 color = Shade(CreateFloorHit(material), lighting);

        AssertColor(new Vector3(0.2f, 0.2f, 0.2f), color);
    }

    [Fact]
    public void MaterialAmbientIsIgnored()
    {
        // Blender writes "Ka 1 1 1" for every material, also for black ones.
        var material = CreateMaterial(ambient: Vector3.One, diffuse: new Vector3(0.005f));
        var lighting = new Lighting(new Vector3(0.5f), []);

        Vector3 color = Shade(CreateFloorHit(material), lighting);

        AssertColor(new Vector3(0.0025f), color);
    }

    [Fact]
    public void LambertFollowsCosineOfLightAngle()
    {
        var material = CreateMaterial(diffuse: new Vector3(0.8f, 0.6f, 0.4f));

        // The light is 60 degrees away from the normal, so N·L = cos(60°) = 0.5.
        var light = new PointLight(
            new Vector3(MathF.Sin(MathF.PI / 3f), MathF.Cos(MathF.PI / 3f), 0f) * 4f,
            new Vector3(1f, 0.5f, 1f)
        );

        Vector3 color = Shade(CreateFloorHit(material), new Lighting(Vector3.Zero, [light]));

        AssertColor(new Vector3(0.4f, 0.15f, 0.2f), color);
    }

    [Fact]
    public void LambertIsZeroForLightBehindSurface()
    {
        var material = CreateMaterial(
            diffuse: Vector3.One,
            specular: Vector3.One,
            specularExponent: 1f
        );
        var light = new PointLight(new Vector3(1f, -4f, 0f), Vector3.One);

        Vector3 color = Shade(CreateFloorHit(material), new Lighting(Vector3.Zero, [light]));

        AssertColor(Vector3.Zero, color);
    }

    [Fact]
    public void PhongHighlightIsFullWhenReflectionPointsToViewer()
    {
        var material = CreateMaterial(
            specular: new Vector3(0.3f, 0.6f, 0.9f),
            specularExponent: 50f
        );

        // A light straight above is mirrored straight back to the viewer: R·V = 1.
        var light = new PointLight(new Vector3(0f, 4f, 0f), Vector3.One);

        Vector3 color = Shade(CreateFloorHit(material), new Lighting(Vector3.Zero, [light]));

        AssertColor(new Vector3(0.3f, 0.6f, 0.9f), color);
    }

    [Fact]
    public void PhongHighlightFallsOffWithSpecularExponent()
    {
        var material = CreateMaterial(specular: Vector3.One, specularExponent: 4f);

        // L at 45° gives R = (-sin 45°, cos 45°, 0), so R·V = cos 45° and (R·V)^4 = 0.25.
        var light = new PointLight(new Vector3(3f, 3f, 0f), Vector3.One);

        Vector3 color = Shade(CreateFloorHit(material), new Lighting(Vector3.Zero, [light]));

        AssertColor(new Vector3(0.25f), color);
    }

    [Fact]
    public void PhongHighlightIsZeroWhenReflectionPointsAwayFromViewer()
    {
        var material = CreateMaterial(specular: Vector3.One, specularExponent: 1f);
        var light = new PointLight(new Vector3(3f, 3f, 0f), Vector3.One);

        // The viewer sits beyond the light; R = (-1, 1, 0)/√2 points away from V = (1, 0.1, 0).
        var viewRay = new Ray(new Vector3(10f, 1f, 0f), new Vector3(-10f, -1f, 0f));
        HitInfo hit = CreateFloorHit(material);

        Vector3 color = _lighting.Shade(
            viewRay,
            hit,
            new Lighting(Vector3.Zero, [light]),
            _noOccluders
        );

        AssertColor(Vector3.Zero, color);
    }

    [Fact]
    public void BlockedLightContributesNothing()
    {
        var material = CreateMaterial(
            diffuse: new Vector3(0.5f),
            specular: Vector3.One,
            specularExponent: 10f
        );
        var light = new PointLight(new Vector3(0f, 4f, 0f), Vector3.One);
        var occluders = new BruteForceAccelerationStructure(
            new Model([CreateHorizontalTriangle(2f)]).Primitives
        );

        Vector3 color = _lighting.Shade(
            _viewRay,
            CreateFloorHit(material),
            new Lighting(new Vector3(0.2f), [light]),
            occluders
        );

        // Only the ambient term remains in the shadow.
        AssertColor(new Vector3(0.1f), color);
    }

    [Fact]
    public void OnlyTheBlockedLightIsRemoved()
    {
        var material = CreateMaterial(diffuse: new Vector3(0.5f));
        var blocked = new PointLight(new Vector3(0f, 4f, 0f), Vector3.One);

        // Far to the side, the second light passes beside the small blocker above the point.
        var visible = new PointLight(new Vector3(20f, 20f, 0f), Vector3.One);
        var occluders = new BruteForceAccelerationStructure(
            new Model([CreateHorizontalTriangle(2f)]).Primitives
        );

        Vector3 color = _lighting.Shade(
            _viewRay,
            CreateFloorHit(material),
            new Lighting(Vector3.Zero, [blocked, visible]),
            occluders
        );

        // Only the visible light at 45° adds 0.5 * cos(45°).
        AssertColor(new Vector3(0.5f * MathF.Sqrt(0.5f)), color);
    }

    [Fact]
    public void ContributionsOfAllLightSourcesAreAdded()
    {
        var material = CreateMaterial(diffuse: new Vector3(0.5f));
        var red = new PointLight(new Vector3(0f, 4f, 0f), new Vector3(0.4f, 0f, 0f));
        var blue = new PointLight(new Vector3(0f, 9f, 0f), new Vector3(0f, 0f, 0.6f));

        Vector3 color = Shade(CreateFloorHit(material), new Lighting(Vector3.Zero, [red, blue]));

        AssertColor(new Vector3(0.2f, 0f, 0.3f), color);
    }

    [Fact]
    public void AnyLightSourceIsSupported()
    {
        var material = CreateMaterial(diffuse: Vector3.One);

        // A directional light (e.g. the sun) is not a point light, but an ILightSource.
        var sun = new DirectionalLight(
            Vector3.Normalize(new Vector3(0f, 1f, 1f)),
            new Vector3(0.8f)
        );

        Vector3 color = Shade(CreateFloorHit(material), new Lighting(Vector3.Zero, [sun]));

        AssertColor(new Vector3(0.8f * MathF.Sqrt(0.5f)), color);
    }

    [Fact]
    public void ShadingNormalIsUsedInsteadOfGeometricNormal()
    {
        var material = CreateMaterial(diffuse: Vector3.One);

        // The interpolated normal leans 60° towards +x, the light comes from +x.
        Vector3 shadingNormal = new(MathF.Sin(MathF.PI / 3f), MathF.Cos(MathF.PI / 3f), 0f);
        var hit = new HitInfo(5f, Vector3.Zero, Vector3.UnitY, shadingNormal, material);
        var light = new PointLight(new Vector3(10f, 0.001f, 0f), Vector3.One);

        Vector3 color = Shade(hit, new Lighting(Vector3.Zero, [light]));

        // N·L with the shading normal is about sin(60°); the geometric normal would give about 0.
        AssertColor(new Vector3(MathF.Sin(MathF.PI / 3f)), color, tolerance: 1e-3f);
    }

    [Fact]
    public void BackSideIsLitFromBehind()
    {
        var material = CreateMaterial(diffuse: Vector3.One);
        var light = new PointLight(new Vector3(0f, -4f, 0f), Vector3.One);

        // Viewer and light are both below the floor whose normals point up.
        var viewRay = new Ray(new Vector3(0f, -5f, 0f), Vector3.UnitY);

        Vector3 color = _lighting.Shade(
            viewRay,
            CreateFloorHit(material),
            new Lighting(Vector3.Zero, [light]),
            _noOccluders
        );

        AssertColor(Vector3.One, color);
    }

    [Fact]
    public void ColorIsClampedToOne()
    {
        var material = CreateMaterial(
            diffuse: Vector3.One,
            specular: Vector3.One,
            specularExponent: 1f
        );
        var light = new PointLight(new Vector3(0f, 4f, 0f), new Vector3(5f, 0.1f, 0f));

        Vector3 color = Shade(CreateFloorHit(material), new Lighting(new Vector3(0.2f), [light]));

        // Red: 0.2 + 5 * (1 + 1) -> 1; green: 0.2 + 0.1 * 2 = 0.4; blue: ambient only.
        AssertColor(new Vector3(1f, 0.4f, 0.2f), color);
    }

    [Fact]
    public void NullArgumentsThrow()
    {
        HitInfo hit = CreateFloorHit(CreateMaterial());
        var lighting = new Lighting(Vector3.Zero, []);

        Assert.Throws<ArgumentNullException>(() =>
            _lighting.Shade(_viewRay, hit, null!, _noOccluders)
        );
        Assert.Throws<ArgumentNullException>(() => _lighting.Shade(_viewRay, hit, lighting, null!));
    }

    private static Vector3 Shade(HitInfo hit, Lighting lighting)
    {
        return _lighting.Shade(_viewRay, hit, lighting, _noOccluders);
    }

    private static HitInfo CreateFloorHit(Material material)
    {
        return new HitInfo(5f, Vector3.Zero, Vector3.UnitY, Vector3.UnitY, material);
    }

    private static Material CreateMaterial(
        Vector3? ambient = null,
        Vector3? diffuse = null,
        Vector3? specular = null,
        float specularExponent = 0f
    )
    {
        return new Material(
            "Test",
            ambient ?? Vector3.Zero,
            diffuse ?? Vector3.Zero,
            specular ?? Vector3.Zero,
            specularExponent
        );
    }

    private static Mesh CreateHorizontalTriangle(float height)
    {
        // Small enough that a light far to the side shines past it onto the origin.
        return new Mesh(
            [
                new Vector3(-1f, height, -1f),
                new Vector3(0f, height, 1f),
                new Vector3(1f, height, -1f),
            ],
            [Vector3.UnitY, Vector3.UnitY, Vector3.UnitY],
            [0, 1, 2],
            Material.Default
        );
    }

    private static void AssertColor(
        Vector3 expected,
        Vector3 actual,
        float tolerance = ColorTolerance
    )
    {
        Assert.True(
            Vector3.Distance(expected, actual) < tolerance,
            $"Expected {expected}, got {actual}."
        );
    }

    /// <summary>
    /// A light infinitely far away whose rays all arrive in the same direction.
    /// </summary>
    private sealed class DirectionalLight(Vector3 towardsLight, Vector3 intensity) : ILightSource
    {
        public LightSample Illuminate(Vector3 point)
        {
            return new LightSample(towardsLight, float.PositiveInfinity, intensity);
        }
    }
}
