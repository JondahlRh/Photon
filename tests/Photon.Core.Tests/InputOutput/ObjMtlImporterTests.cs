// -----------------------------------------------------------------------------
// Author:        BrackyDayn
// AI Assistance: Generated with AI assistance
// -----------------------------------------------------------------------------
using System.Globalization;
using System.Numerics;
using Photon.Core.Geometry;
using Photon.Core.InputOutput;

namespace Photon.Core.Tests.InputOutput;

/// <summary>
/// Verifies material loading and face assignments using temporary OBJ and MTL files.
/// </summary>
public sealed class ObjMtlImporterTests : IDisposable
{
    /// <summary>
    /// Directory containing this test's input files.
    /// </summary>
    private readonly string _directory;

    /// <summary>
    /// Initializes an isolated directory for each test.
    /// </summary>
    public ObjMtlImporterTests()
    {
        _directory = Path.Combine(
            Path.GetTempPath(),
            "PhotonImporterTests",
            Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(_directory);
    }

    /// <summary>
    /// Checks relative library paths, material changes, and culture-independent RGB values.
    /// </summary>
    [Fact]
    public void LoadAssignsRelativeLibraryColorsToFaces()
    {
        WriteFile(
            "models/materials/palette.mtl",
            """
            # Two diffuse materials
            newmtl Red
            Ka 0 0 0
            Kd 1.0 0.2 0.2 # inline comment

            newmtl Green
            Kd 0.2 1.0 0.2
            """
        );
        string path = WriteFile(
            "models/model.obj",
            """
            mtllib materials/palette.mtl
            v 0 0 0
            v 1 0 0
            v 0 1 0
            f 1 2 3
            usemtl Red
            f 1 2 3
            usemtl Green
            f 1 2 3
            """
        );

        CultureInfo originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            Model model = ObjMtlImporter.Load(path);

            Assert.Equal(3, model.Mesh.Triangles.Count);
            Assert.Equal(3, model.Materials.Count);
            Assert.Collection(
                model.MaterialIndices,
                index => Assert.Equal(0, index),
                index => Assert.Equal(1, index),
                index => Assert.Equal(2, index)
            );
            Assert.Equal(new Vector3(0.8f), model.Materials[0].DiffuseColor);
            Assert.Equal("Red", model.Materials[1].Name);
            Assert.Equal(new Vector3(1f, 0.2f, 0.2f), model.Materials[1].DiffuseColor);
            Assert.Equal("Green", model.Materials[2].Name);
            Assert.Equal(new Vector3(0.2f, 1f, 0.2f), model.Materials[2].DiffuseColor);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    /// <summary>
    /// Checks that a later material definition updates an already assigned placeholder.
    /// </summary>
    [Fact]
    public void LoadResolvesMaterialsDeclaredAfterFaces()
    {
        WriteFile("palette.mtl", "newmtl Red\nKd 1 0 0\n");
        string path = WriteFile(
            "model.obj",
            """
            v 0 0 0
            v 1 0 0
            v 0 1 0
            usemtl Red
            f 1 2 3
            mtllib palette.mtl
            """
        );

        Model model = ObjMtlImporter.Load(path);

        Assert.Equal(2, model.Materials.Count);
        int index = Assert.Single(model.MaterialIndices);
        Assert.Equal(new Vector3(1f, 0f, 0f), model.Materials[index].DiffuseColor);
    }

    /// <summary>
    /// Checks fallback colors for missing libraries and undefined material names.
    /// </summary>
    [Fact]
    public void LoadKeepsMissingMaterialsGray()
    {
        string path = WriteFile(
            "model.obj",
            """
            mtllib missing.mtl
            v 0 0 0
            v 1 0 0
            v 0 1 0
            f 1 2 3
            usemtl Missing
            f 1 2 3
            """
        );

        Model model = ObjMtlImporter.Load(path);

        Assert.Equal(2, model.Mesh.Triangles.Count);
        Assert.All(
            model.MaterialIndices,
            index => Assert.Equal(new Vector3(0.8f), model.Materials[index].DiffuseColor)
        );
    }

    /// <summary>
    /// Checks multiple libraries and redefinitions without changing existing face indices.
    /// </summary>
    [Fact]
    public void LoadUsesTheLatestMaterialDefinition()
    {
        WriteFile("first.mtl", "newmtl Red\nKd 1 0 0\n");
        WriteFile("second.mtl", "newmtl Red\nKd 0 1 0\n");
        string path = WriteFile(
            "model.obj",
            """
            mtllib first.mtl
            v 0 0 0
            v 1 0 0
            v 0 1 0
            usemtl Red
            f 1 2 3
            mtllib first.mtl second.mtl
            f 1 2 3
            """
        );

        Model model = ObjMtlImporter.Load(path);

        Assert.Equal(2, model.Materials.Count);
        Assert.Collection(
            model.MaterialIndices,
            index => Assert.Equal(1, index),
            index => Assert.Equal(1, index)
        );
        Assert.Equal(new Vector3(0f, 1f, 0f), model.Materials[1].DiffuseColor);
    }

    /// <summary>
    /// Checks that malformed RGB values identify their MTL file and line.
    /// </summary>
    /// <param name="colorLine">An invalid diffuse color statement.</param>
    [Theory]
    [InlineData("Kd 1 0")]
    [InlineData("Kd invalid 0 0")]
    [InlineData("Kd NaN 0 0")]
    [InlineData("Kd 1e999 0 0")]
    public void LoadReportsInvalidMaterialColors(string colorLine)
    {
        string materialPath = WriteFile("palette.mtl", "newmtl Red\n" + colorLine);
        string path = WriteFile("model.obj", "mtllib palette.mtl\n");

        FormatException exception = Assert.Throws<FormatException>(() => ObjMtlImporter.Load(path));

        Assert.Contains(materialPath + ":2:", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Checks that diffuse colors require a preceding material declaration.
    /// </summary>
    [Fact]
    public void LoadRejectsColorsBeforeMaterialNames()
    {
        string materialPath = WriteFile("palette.mtl", "Kd 1 0 0\n");
        string path = WriteFile("model.obj", "mtllib palette.mtl\n");

        FormatException exception = Assert.Throws<FormatException>(() => ObjMtlImporter.Load(path));

        Assert.Contains(materialPath + ":1:", exception.Message, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Creates a file within the test directory, including any parent directories.
    /// </summary>
    /// <param name="relativePath">The path relative to the test directory.</param>
    /// <param name="contents">The file contents.</param>
    /// <returns>The absolute file path.</returns>
    private string WriteFile(string relativePath, string contents)
    {
        string path = Path.Combine(_directory, relativePath);
        string? directory = Path.GetDirectoryName(path);

        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, contents);
        return path;
    }
}
