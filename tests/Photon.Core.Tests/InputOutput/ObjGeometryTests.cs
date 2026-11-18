// -----------------------------------------------------------------------------
// Author:        MJZgh
// AI Assistance: Claude (test generation)
// -----------------------------------------------------------------------------
using System.Numerics;
using Photon.Core.InputOutput;
using Photon.Core.Scenes;

namespace Photon.Core.Tests.InputOutput;

/// <summary>
/// Verifies faces, normals, warnings and error locations of the OBJ importer.
/// </summary>
public sealed class ObjGeometryTests : IDisposable
{
    /// <summary>
    /// Directory containing this test's input files.
    /// </summary>
    private readonly string _directory;

    /// <summary>
    /// Initializes an isolated directory for each test.
    /// </summary>
    public ObjGeometryTests()
    {
        _directory = Path.Combine(
            Path.GetTempPath(),
            "PhotonGeometryTests",
            Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(_directory);
    }

    /// <summary>
    /// Checks that a quad is split into two triangles.
    /// </summary>
    [Fact]
    public void LoadSplitsQuadsIntoTwoTriangles()
    {
        string path = WriteFile(
            "quad.obj",
            """
            v 0 0 0
            v 1 0 0
            v 1 1 0
            v 0 1 0
            f 1 2 3 4
            """
        );

        Model model = ObjMtlImporter.Load(path).Model;

        Assert.Equal(2, model.TriangleCount);
    }

    /// <summary>
    /// Checks that negative indices count back from the positions read so far.
    /// </summary>
    [Fact]
    public void LoadResolvesNegativeIndices()
    {
        string path = WriteFile(
            "negative.obj",
            """
            v 0 0 0
            v 1 0 0
            v 0 1 0
            f -3 -2 -1
            """
        );

        Mesh mesh = Assert.Single(ObjMtlImporter.Load(path).Model.Meshes);
        MeshTriangle triangle = Assert.Single(mesh.Triangles);

        Assert.Equal(0, triangle.Index0);
        Assert.Equal(1, triangle.Index1);
        Assert.Equal(2, triangle.Index2);
    }

    /// <summary>
    /// Checks that normals given with <c>vn</c> are used instead of calculated ones.
    /// </summary>
    [Fact]
    public void LoadUsesVertexNormalsFromTheFile()
    {
        string path = WriteFile(
            "normals.obj",
            """
            v 0 0 0
            v 1 0 0
            v 0 1 0
            vn 0 0 1
            f 1//1 2//1 3//1
            """
        );

        Mesh mesh = Assert.Single(ObjMtlImporter.Load(path).Model.Meshes);

        Assert.Equal(new Vector3(0f, 0f, 1f), mesh.Normals[0]);
        Assert.Equal(new Vector3(0f, 0f, 1f), mesh.Normals[1]);
        Assert.Equal(new Vector3(0f, 0f, 1f), mesh.Normals[2]);
    }

    /// <summary>
    /// Checks that an unknown keyword is reported once and the import continues.
    /// </summary>
    [Fact]
    public void LoadWarnsOncePerUnknownKeyword()
    {
        string path = WriteFile(
            "unknown.obj",
            """
            v 0 0 0
            v 1 0 0
            v 0 1 0
            foo 1
            foo 2
            f 1 2 3
            """
        );

        ImportResult result = ObjMtlImporter.Load(path);

        Assert.Equal(1, result.Model.TriangleCount);
        string warning = Assert.Single(result.Warnings);
        Assert.Contains("'foo'", warning, StringComparison.Ordinal);
    }

    /// <summary>
    /// Checks that a missing material library produces a warning instead of an error.
    /// </summary>
    [Fact]
    public void LoadWarnsAboutMissingMaterialLibrary()
    {
        string path = WriteFile(
            "missing.obj",
            """
            mtllib missing.mtl
            v 0 0 0
            v 1 0 0
            v 0 1 0
            f 1 2 3
            """
        );

        ImportResult result = ObjMtlImporter.Load(path);

        string warning = Assert.Single(result.Warnings);
        Assert.Contains("missing.mtl", warning, StringComparison.Ordinal);
    }

    /// <summary>
    /// Checks that an out-of-range index throws an exception that names the failing line.
    /// </summary>
    [Fact]
    public void LoadReportsOutOfRangeIndexWithLineNumber()
    {
        string path = WriteFile(
            "range.obj",
            """
            v 0 0 0
            v 1 0 0
            v 0 1 0
            f 1 2 4
            """
        );

        FormatException exception = Assert.Throws<FormatException>(() => ObjMtlImporter.Load(path));

        Assert.Contains(path + ":4:", exception.Message, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Creates a file within the test directory.
    /// </summary>
    /// <param name="relativePath">The path relative to the test directory.</param>
    /// <param name="contents">The file contents.</param>
    /// <returns>The absolute file path.</returns>
    private string WriteFile(string relativePath, string contents)
    {
        string path = Path.Combine(_directory, relativePath);
        File.WriteAllText(path, contents);
        return path;
    }
}
