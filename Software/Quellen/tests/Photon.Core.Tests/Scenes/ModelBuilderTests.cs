// -----------------------------------------------------------------------------
// Author:        Luca Lengl
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------

using System.Numerics;
using Photon.Core.Mathematics;
using Photon.Core.Scenes;

namespace Photon.Core.Tests.Scenes;

public sealed class ModelBuilderTests
{
    [Fact]
    public void QuadIsSplitIntoTwoTriangles()
    {
        ModelBuilder builder = CreateBuilderWithSquare();

        builder.AddFace([new(0), new(1), new(2), new(3)]);
        Model model = builder.Build();

        Assert.Single(model.Meshes);
        Assert.Equal(2, model.TriangleCount);
        Assert.Equal(2, model.Primitives.Count);
        Assert.Equal(4, model.Meshes[0].Vertices.Length);
    }

    [Fact]
    public void EachMaterialGroupBecomesOneMesh()
    {
        ModelBuilder builder = CreateBuilderWithSquare();
        var red = new Material("Red", Vector3.Zero, new Vector3(1f, 0f, 0f), Vector3.Zero);
        builder.AddMaterial(red);

        builder.AddTriangle(0, 1, 2);
        builder.UseMaterial("Red");
        builder.AddTriangle(0, 2, 3);
        Model model = builder.Build();

        Assert.Equal(2, model.Meshes.Count);
        Assert.Same(Material.Default, model.Meshes[0].Material);
        Assert.Same(red, model.Meshes[1].Material);
    }

    [Fact]
    public void GroupsWithoutFacesAreSkipped()
    {
        ModelBuilder builder = CreateBuilderWithSquare();

        builder.UseMaterial("Unused");
        builder.UseMaterial("Used");
        builder.AddTriangle(0, 1, 2);
        Model model = builder.Build();

        Assert.Single(model.Meshes);
    }

    [Fact]
    public void UnknownMaterialIsResolvedWhenAddedLater()
    {
        ModelBuilder builder = CreateBuilderWithSquare();

        bool known = builder.UseMaterial("Late");
        builder.AddTriangle(0, 1, 2);
        var late = new Material("Late", Vector3.Zero, Vector3.One, Vector3.Zero);
        builder.AddMaterial(late);
        Model model = builder.Build();

        Assert.False(known);
        Assert.Same(late, model.Meshes[0].Material);
    }

    [Fact]
    public void MissingNormalsAreCalculatedFromFaces()
    {
        ModelBuilder builder = CreateBuilderWithSquare();

        builder.AddTriangle(0, 1, 2);
        Model model = builder.Build();

        foreach (Vector3 normal in model.Meshes[0].Normals)
        {
            Assert.True(FloatComparison.NearlyEqual(1f, normal.Z));
        }
    }

    [Fact]
    public void SharedPositionGetsAveragedNormal()
    {
        // Two faces meet at a right angle along the edge from position 0 to 1.
        var builder = new ModelBuilder();
        builder.AddPosition(Vector3.Zero);
        builder.AddPosition(Vector3.UnitX);
        builder.AddPosition(Vector3.UnitY);
        builder.AddPosition(Vector3.UnitZ);
        builder.AddTriangle(0, 1, 2); // normal +Z
        builder.AddTriangle(0, 3, 1); // normal +Y

        Model model = builder.Build();
        Vector3 normalAtOrigin = model.Meshes[0].Normals[0];

        Vector3 expected = Vector3.Normalize(new Vector3(0f, 1f, 1f));
        Assert.True(Vector3.Distance(expected, normalAtOrigin) < 1e-4f);
    }

    [Fact]
    public void NormalsOfVerySmallModelsAreCalculated()
    {
        var builder = new ModelBuilder();
        builder.AddPosition(Vector3.Zero);
        builder.AddPosition(new Vector3(1e-4f, 0f, 0f));
        builder.AddPosition(new Vector3(0f, 1e-4f, 0f));
        builder.AddTriangle(0, 1, 2);

        Model model = builder.Build();

        Assert.True(FloatComparison.NearlyEqual(1f, model.Meshes[0].Normals[0].Z));
    }

    [Fact]
    public void ExplicitNormalsAreKept()
    {
        ModelBuilder builder = CreateBuilderWithSquare();
        int normal = builder.AddNormal(new Vector3(0f, 0f, 2f));

        builder.AddFace([new(0, normal), new(1, normal), new(2, normal)]);
        Model model = builder.Build();

        Assert.Equal(Vector3.UnitZ, model.Meshes[0].Normals[0]);
    }

    [Fact]
    public void InvalidIndexThrows()
    {
        ModelBuilder builder = CreateBuilderWithSquare();

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddTriangle(0, 1, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            builder.AddFace([new(0, 0), new(1), new(2)])
        );
    }

    [Fact]
    public void FaceWithTwoCornersThrows()
    {
        ModelBuilder builder = CreateBuilderWithSquare();

        Assert.Throws<ArgumentException>(() => builder.AddFace([new(0), new(1)]));
    }

    [Fact]
    public void ModelBoundsContainAllVertices()
    {
        ModelBuilder builder = CreateBuilderWithSquare();

        builder.AddFace([new(0), new(1), new(2), new(3)]);
        Model model = builder.Build();

        Assert.Equal(Vector3.Zero, model.Bounds.Min);
        Assert.Equal(new Vector3(1f, 1f, 0f), model.Bounds.Max);
    }

    private static ModelBuilder CreateBuilderWithSquare()
    {
        var builder = new ModelBuilder();
        builder.AddPosition(new Vector3(0f, 0f, 0f));
        builder.AddPosition(new Vector3(1f, 0f, 0f));
        builder.AddPosition(new Vector3(1f, 1f, 0f));
        builder.AddPosition(new Vector3(0f, 1f, 0f));
        return builder;
    }
}
