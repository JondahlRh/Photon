// -----------------------------------------------------------------------------
// Author:        MJZgh
// AI Assistance: Claude (test generation) and Code feedback
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
    /// Checks that a zero-length normal produces a warning with its line, keeps the indices of
    /// later normals, and is replaced by the calculated normal.
    /// </summary>
    [Fact]
    public void LoadReplacesZeroLengthNormalWithCalculatedNormal()
    {
        string path = WriteFile(
            "zero-normal.obj",
            """
            v 0 0 0
            v 1 0 0
            v 0 1 0
            v 0 0 1
            vn -0.0000 -0.0000 -0.0000
            vn 0 1 0
            f 1//1 2//1 3//1
            f 1//2 4//2 2//2
            """
        );

        ImportResult result = ObjMtlImporter.Load(path);

        string warning = Assert.Single(result.Warnings);
        Assert.Contains($"{path}:5:", warning, StringComparison.Ordinal);
        Assert.Contains("zero length", warning, StringComparison.Ordinal);

        Mesh mesh = Assert.Single(result.Model.Meshes);
        MeshTriangle first = mesh.Triangles[0];
        MeshTriangle second = mesh.Triangles[1];

        // Positions 1 and 2 are shared with the second face (normal +Y), position 3 is not.
        Vector3 shared = Vector3.Normalize(new Vector3(0f, 1f, 1f));
        AssertNearlyEqual(shared, mesh.Normals[first.Index0]);
        AssertNearlyEqual(shared, mesh.Normals[first.Index1]);
        AssertNearlyEqual(Vector3.UnitZ, mesh.Normals[first.Index2]);

        // The second face still uses the normal from the file.
        AssertNearlyEqual(Vector3.UnitY, mesh.Normals[second.Index0]);
        AssertNearlyEqual(Vector3.UnitY, mesh.Normals[second.Index1]);
        AssertNearlyEqual(Vector3.UnitY, mesh.Normals[second.Index2]);
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

    /// <summary>
    /// Checks that all supported MTL properties are copied to the mesh material.
    /// </summary>
    [Fact]
    public void LoadReadsMaterialProperties()
    {
        WriteFile(
            "properties.mtl",
            """
            newmtl Red
            Ka 0.1 0.2 0.3
            Kd 0.4 0.5 0.6
            Ks 0.7 0.8 0.9
            Ns 50
            d 0.5
            Ni 1.45
            illum 2
            """
        );
        string path = WriteFile(
            "properties.obj",
            """
            mtllib properties.mtl
            v 0 0 0
            v 1 0 0
            v 0 1 0
            usemtl Red
            f 1 2 3
            """
        );

        Material material = Assert.Single(ObjMtlImporter.Load(path).Model.Meshes).Material;

        Assert.Equal("Red", material.Name);
        Assert.Equal(new Vector3(0.1f, 0.2f, 0.3f), material.Ambient);
        Assert.Equal(new Vector3(0.4f, 0.5f, 0.6f), material.Diffuse);
        Assert.Equal(new Vector3(0.7f, 0.8f, 0.9f), material.Specular);
        Assert.Equal(50f, material.SpecularExponent);
        Assert.Equal(0.5f, material.Opacity);
        Assert.Equal(1.45f, material.RefractiveIndex);
        Assert.Equal(2, material.IlluminationModel);
    }

    /// <summary>
    /// Checks that an undefined material name uses the default material values and warns once.
    /// </summary>
    [Fact]
    public void LoadUsesDefaultMaterialForUndefinedName()
    {
        string path = WriteFile(
            "undefined.obj",
            """
            v 0 0 0
            v 1 0 0
            v 0 1 0
            usemtl Unknown
            f 1 2 3
            """
        );

        ImportResult result = ObjMtlImporter.Load(path);
        Material material = Assert.Single(result.Model.Meshes).Material;

        Assert.Equal("Unknown", material.Name);
        Assert.Equal(Material.Default.Diffuse, material.Diffuse);
        Assert.Single(result.Warnings);
    }

    /// <summary>
    /// Checks that an invalid MTL value throws an exception that names the library and line.
    /// </summary>
    [Fact]
    public void LoadReportsInvalidMaterialValueWithLineNumber()
    {
        WriteFile("invalid.mtl", "newmtl Red\nillum 11\n");
        string path = WriteFile(
            "invalid.obj",
            """
            mtllib invalid.mtl
            v 0 0 0
            v 1 0 0
            v 0 1 0
            usemtl Red
            f 1 2 3
            """
        );

        FormatException exception = Assert.Throws<FormatException>(() => ObjMtlImporter.Load(path));

        Assert.Contains("invalid.mtl:2:", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Checks that an unsupported MTL keyword is reported once per library.
    /// </summary>
    [Fact]
    public void LoadWarnsOncePerUnknownMtlKeyword()
    {
        WriteFile("warn.mtl", "newmtl Red\nmap_Kd a.png\nmap_Kd b.png\nKd 1 0 0\n");
        string path = WriteFile(
            "warn.obj",
            """
            mtllib warn.mtl
            v 0 0 0
            v 1 0 0
            v 0 1 0
            usemtl Red
            f 1 2 3
            """
        );

        ImportResult result = ObjMtlImporter.Load(path);

        string warning = Assert.Single(result.Warnings);
        Assert.Contains("map_Kd", warning, StringComparison.Ordinal);
    }

    /// <summary>
    /// Checks that gray color shorthand is accepted and the <c>-halo</c> option is reported.
    /// </summary>
    [Fact]
    public void LoadAcceptsGrayShorthandAndWarnsAboutHalo()
    {
        WriteFile("short.mtl", "newmtl Red\nKa 0.2\nKs 0.3\nd -halo 0.5\n");
        string path = WriteFile(
            "short.obj",
            """
            mtllib short.mtl
            v 0 0 0
            v 1 0 0
            v 0 1 0
            usemtl Red
            f 1 2 3
            """
        );

        ImportResult result = ObjMtlImporter.Load(path);
        Material material = Assert.Single(result.Model.Meshes).Material;

        Assert.Equal(new Vector3(0.2f), material.Ambient);
        Assert.Equal(new Vector3(0.3f), material.Specular);
        Assert.Equal(0.5f, material.Opacity);
        string warning = Assert.Single(result.Warnings);
        Assert.Contains("-halo", warning, StringComparison.Ordinal);
    }

    /// <summary>
    /// Checks that a U-shaped polygon is triangulated without filling its notch.
    /// </summary>
    [Fact]
    public void AddFaceTriangulatesConcavePolygonWithoutFillingNotch()
    {
        var builder = new ModelBuilder();
        Vector3[] outline =
        [
            new(0, 0, 0),
            new(3, 0, 0),
            new(3, 3, 0),
            new(2, 3, 0),
            new(2, 1, 0),
            new(1, 1, 0),
            new(1, 3, 0),
            new(0, 3, 0),
        ];

        foreach (Vector3 point in outline)
        {
            builder.AddPosition(point);
        }

        builder.AddFace([new(0), new(1), new(2), new(3), new(4), new(5), new(6), new(7)]);

        Mesh mesh = Assert.Single(builder.Build().Meshes);
        float area = 0f;

        foreach (MeshTriangle triangle in mesh.Triangles)
        {
            Vector3 a = mesh.Vertices[triangle.Index0];
            Vector3 b = mesh.Vertices[triangle.Index1];
            Vector3 c = mesh.Vertices[triangle.Index2];
            area += MathF.Abs(Vector3.Cross(b - a, c - a).Z) / 2f;
        }

        Assert.Equal(6, mesh.Triangles.Count);
        Assert.Equal(7f, area, 4);
    }

    /// <summary>
    /// Checks that the homogeneous coordinate of a vertex is applied by dividing the coordinates by it.
    /// </summary>
    [Fact]
    public void LoadAppliesHomogeneousCoordinateOfVertex()
    {
        string path = WriteFile(
            "homogeneous.obj",
            """
            v 2 4 6 2
            v 1 0 0
            v 0 1 0
            f 1 2 3
            """
        );

        Mesh mesh = Assert.Single(ObjMtlImporter.Load(path).Model.Meshes);

        Assert.Equal(new Vector3(1f, 2f, 3f), mesh.Vertices[0]);
    }

    /// <summary>
    /// Checks that a zero homogeneous coordinate is rejected with the line number.
    /// </summary>
    [Fact]
    public void LoadReportsZeroHomogeneousCoordinateWithLineNumber()
    {
        string path = WriteFile(
            "zero-w.obj",
            """
            v 1 2 3 0
            v 1 0 0
            v 0 1 0
            f 1 2 3
            """
        );

        FormatException exception = Assert.Throws<FormatException>(() => ObjMtlImporter.Load(path));

        Assert.Contains(path + ":1:", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Checks that vertex colors are skipped with a single warning and the geometry is kept.
    /// </summary>
    [Fact]
    public void LoadSkipsVertexColorsWithWarning()
    {
        string path = WriteFile(
            "colors.obj",
            """
            v 0 0 0 1 0 0
            v 1 0 0 0 1 0
            v 0 1 0 0 0 1
            f 1 2 3
            """
        );

        ImportResult result = ObjMtlImporter.Load(path);

        Assert.Equal(1, result.Model.TriangleCount);
        string warning = Assert.Single(result.Warnings);
        Assert.Contains("Vertex colors", warning, StringComparison.Ordinal);
    }

    /// <summary>
    /// Checks that a self-intersecting quad is rejected instead of being triangulated.
    /// </summary>
    [Fact]
    public void LoadRejectsSelfIntersectingQuad()
    {
        string path = WriteFile(
            "bowtie.obj",
            """
            v 3 2 0
            v 5 0 0
            v 3 5 0
            v 4 0 0
            f 1 2 3 4
            """
        );

        FormatException exception = Assert.Throws<FormatException>(() => ObjMtlImporter.Load(path));

        Assert.Contains("self-intersecting", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Checks that a triangle whose corners lie on one line is rejected.
    /// </summary>
    [Fact]
    public void LoadRejectsDegenerateTriangle()
    {
        string path = WriteFile(
            "line.obj",
            """
            v 0 0 0
            v 1 0 0
            v 2 0 0
            f 1 2 3
            """
        );

        FormatException exception = Assert.Throws<FormatException>(() => ObjMtlImporter.Load(path));

        Assert.Contains("degenerate", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Checks that a valid quad with a small size is accepted.
    /// </summary>
    [Fact]
    public void LoadAcceptsSmallQuad()
    {
        string path = WriteFile(
            "small.obj",
            """
            v 0 0 0
            v 0.001 0 0
            v 0.001 0.001 0
            v 0 0.001 0
            f 1 2 3 4
            """
        );

        Model model = ObjMtlImporter.Load(path).Model;

        Assert.Equal(2, model.TriangleCount);
    }

    /// <summary>
    /// Checks that a self-intersecting face is reported with its file and line.
    /// </summary>
    [Fact]
    public void LoadReportsSelfIntersectionWithFileAndLine()
    {
        string path = WriteFile(
            "bowtie-line.obj",
            """
            v 3 2 0
            v 5 0 0
            v 3 5 0
            v 4 0 0
            f 1 2 3 4
            """
        );

        FormatException exception = Assert.Throws<FormatException>(() => ObjMtlImporter.Load(path));

        Assert.Contains(path + ":5:", exception.Message, StringComparison.Ordinal);
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

    /// <summary>
    /// Asserts that two vectors are equal within a small tolerance.
    /// </summary>
    /// <param name="expected">The expected vector.</param>
    /// <param name="actual">The actual vector.</param>
    private static void AssertNearlyEqual(Vector3 expected, Vector3 actual)
    {
        Assert.True(
            Vector3.Distance(expected, actual) < 1e-5f,
            $"Expected {expected}, but got {actual}."
        );
    }
}
