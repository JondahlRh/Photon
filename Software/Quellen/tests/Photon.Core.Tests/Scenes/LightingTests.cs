// -----------------------------------------------------------------------------
// Author:        Luca Lengl
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------

using System.Numerics;
using Photon.Core.Mathematics;
using Photon.Core.Scenes;

namespace Photon.Core.Tests.Scenes;

public sealed class LightingTests
{
    [Fact]
    public void PointLightReturnsDirectionAndDistance()
    {
        var light = new PointLight(new Vector3(0f, 4f, 0f), Vector3.One);

        LightSample sample = light.Illuminate(Vector3.Zero);

        Assert.Equal(Vector3.UnitY, sample.Direction);
        Assert.True(FloatComparison.NearlyEqual(4f, sample.Distance));
        Assert.Equal(Vector3.One, sample.Intensity);
    }

    [Fact]
    public void PointLightWithNegativeIntensityThrows()
    {
        Assert.Throws<ArgumentException>(() => new PointLight(Vector3.Zero, new Vector3(-1f)));
    }

    [Fact]
    public void LightingKeepsItsLightsWhenSourceListChanges()
    {
        var lights = new List<ILightSource> { new PointLight(Vector3.UnitX, Vector3.One) };
        var lighting = new Lighting(Vector3.Zero, lights);

        lights.Add(new PointLight(Vector3.UnitY, Vector3.One));

        Assert.Single(lighting.Lights);
    }

    [Fact]
    public void NegativeAmbientIntensityThrows()
    {
        Assert.Throws<ArgumentException>(() => new Lighting(new Vector3(-1f), []));
    }
}
