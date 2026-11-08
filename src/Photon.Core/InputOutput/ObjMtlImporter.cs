// -----------------------------------------------------------------------------
// Author:        BrackyDayn
// AI Assistance: Generated with AI assistance
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using Photon.Core.Geometry;
using Photon.Core.Mathematics;

namespace Photon.Core.InputOutput;

/// <summary>
/// Imports triangular OBJ geometry and diffuse RGB colors from MTL files.
/// Grouping, smoothing, texture coordinates, and imported normals are ignored.
/// </summary>
public readonly struct ObjMtlImporter
{
    /// <summary>
    /// Loads vertex positions, triangular faces, and their material assignments.
    /// </summary>
    /// <param name="path">The path to the OBJ file.</param>
    /// <returns>The imported geometry and materials, including a gray fallback.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the path is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown when the file cannot be read.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when an OBJ or MTL file contains invalid data, or no faces are found.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when a face is not triangular or an unsupported statement occurs.
    /// </exception>
    public static Model Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        List<Vector3> positions = new List<Vector3>();
        List<Triangle> triangles = new List<Triangle>();
        List<int> materialIndices = new List<int>();

        List<Material> materials = new List<Material>
        {
            new Material("Default", new Vector3(0.8f, 0.8f, 0.8f)),
        };
        Dictionary<string, int> materialLookup = new Dictionary<string, int>(
            StringComparer.Ordinal
        );
        int currentMaterialIndex = 0;
        string directory = Path.GetDirectoryName(Path.GetFullPath(path)) ?? string.Empty;

        int lineNumber = 0;

        foreach (string rawLine in File.ReadLines(path))
        {
            lineNumber++;

            string line = rawLine;
            int commentIndex = line.IndexOf('#');

            if (commentIndex >= 0)
            {
                line = line[..commentIndex];
            }

            line = line.Trim();

            if (line.Length == 0)
            {
                continue;
            }
            string[] parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

            string location = $"{path}:{lineNumber}";

            switch (parts[0])
            {
                case "v":
                {
                    if (parts.Length != 4)
                    {
                        throw new FormatException(
                            $"{location}: Expected a vertex in the form 'v x y z'."
                        );
                    }

                    float x = ParseCoordinate(parts[1], location);
                    float y = ParseCoordinate(parts[2], location);
                    float z = ParseCoordinate(parts[3], location);

                    positions.Add(new Vector3(x, y, z));
                    break;
                }
                case "f":
                {
                    if (parts.Length < 4)
                    {
                        throw new FormatException(
                            $"{location}: A face requires at least three vertices."
                        );
                    }

                    if (parts.Length != 4)
                    {
                        throw new NotSupportedException(
                            $"{location}: Only triangular faces are supported."
                        );
                    }

                    int a = ParseVertexIndex(parts[1], positions.Count, location);
                    int b = ParseVertexIndex(parts[2], positions.Count, location);
                    int c = ParseVertexIndex(parts[3], positions.Count, location);

                    triangles.Add(new Triangle(positions[a], positions[b], positions[c]));

                    materialIndices.Add(currentMaterialIndex);
                    break;
                }
                case "mtllib":
                {
                    if (parts.Length < 2)
                    {
                        throw new FormatException(
                            $"{location}: Expected a material library filename."
                        );
                    }

                    for (int i = 1; i < parts.Length; i++)
                    {
                        string materialPath = Path.Combine(directory, parts[i]);
                        LoadMaterials(materialPath, materials, materialLookup);
                    }

                    break;
                }
                case "usemtl":
                {
                    if (parts.Length < 2)
                    {
                        throw new FormatException($"{location}: Expected a material name.");
                    }

                    string name = line["usemtl".Length..].Trim();

                    if (!materialLookup.TryGetValue(name, out currentMaterialIndex))
                    {
                        // A later library can fill this material without changing face indices.
                        currentMaterialIndex = materials.Count;
                        materialLookup.Add(name, currentMaterialIndex);
                        materials.Add(new Material(name, new Vector3(0.8f, 0.8f, 0.8f)));
                    }

                    break;
                }
                case "o":
                case "g":
                case "s":
                case "vt":
                case "vn":
                    // The preview uses one mesh and calculated face normals.
                    break;

                default:
                    throw new NotSupportedException(
                        $"{location}: OBJ statement '{parts[0]}' is not supported yet."
                    );
            }
        }

        if (triangles.Count == 0)
        {
            throw new FormatException($"{path}: No triangular faces were found.");
        }

        Mesh mesh = new Mesh(triangles);

        return new Model(mesh, materials, materialIndices);
    }

    /// <summary>
    /// Loads named materials and their diffuse RGB colors.
    /// Missing libraries leave existing fallback colors unchanged.
    /// </summary>
    /// <param name="path">The material library path.</param>
    /// <param name="materials">The model's material collection.</param>
    /// <param name="materialLookup">Material names mapped to collection indices.</param>
    /// <exception cref="IOException">
    /// Thrown when an existing material library cannot be read.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when a material name or diffuse RGB color is invalid.
    /// </exception>
    private static void LoadMaterials(
        string path,
        List<Material> materials,
        Dictionary<string, int> materialLookup
    )
    {
        if (!File.Exists(path))
        {
            return;
        }

        int materialIndex = -1;
        int lineNumber = 0;

        foreach (string rawLine in File.ReadLines(path))
        {
            lineNumber++;

            string line = rawLine;
            int commentIndex = line.IndexOf('#');

            if (commentIndex >= 0)
            {
                line = line[..commentIndex];
            }

            line = line.Trim();

            if (line.Length == 0)
            {
                continue;
            }

            string[] parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            string location = $"{path}:{lineNumber}";

            if (parts[0] == "newmtl")
            {
                if (parts.Length < 2)
                {
                    throw new FormatException($"{location}: Expected a material name.");
                }

                string name = line["newmtl".Length..].Trim();

                if (!materialLookup.TryGetValue(name, out materialIndex))
                {
                    materialIndex = materials.Count;
                    materialLookup.Add(name, materialIndex);
                    materials.Add(new Material(name, new Vector3(0.8f, 0.8f, 0.8f)));
                }
                else
                {
                    materials[materialIndex] = new Material(name, new Vector3(0.8f, 0.8f, 0.8f));
                }
            }
            else if (parts[0] == "Kd")
            {
                if (materialIndex < 0 || parts.Length != 4)
                {
                    throw new FormatException($"{location}: Expected 'Kd r g b' after 'newmtl'.");
                }

                float red = ParseCoordinate(parts[1], location);
                float green = ParseCoordinate(parts[2], location);
                float blue = ParseCoordinate(parts[3], location);
                string name = materials[materialIndex].Name;

                materials[materialIndex] = new Material(name, new Vector3(red, green, blue));
            }
        }
    }

    /// <summary>
    /// Parses a finite coordinate using culture-independent number formatting.
    /// </summary>
    /// <param name="text">The coordinate text.</param>
    /// <param name="location">The file path and line number for error messages.</param>
    /// <returns>The parsed coordinate.</returns>
    /// <exception cref="FormatException">
    /// Thrown when the coordinate is invalid or not finite.
    /// </exception>
    private static float ParseCoordinate(string text, string location)
    {
        bool success = float.TryParse(
            text,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out float value
        );

        if (!success || !float.IsFinite(value))
        {
            throw new FormatException($"{location}: Invalid coordinate '{text}'.");
        }
        return value;
    }

    /// <summary>
    /// Reads an OBJ position index and converts it into a zero-based index.
    /// Texture and normal references after a slash are ignored.
    /// </summary>
    /// <param name="text">The complete face reference, such as "1/2/3".</param>
    /// <param name="vertexCount">The number of previously read positions.</param>
    /// <param name="location">The file path and line number for error messages.</param>
    /// <returns>The zero-based position index.</returns>
    /// <exception cref="FormatException">
    /// Thrown when the position index is invalid or out of range.
    /// </exception>
    private static int ParseVertexIndex(string text, int vertexCount, string location)
    {
        int slashIndex = text.IndexOf('/');

        if (slashIndex >= 0)
        {
            text = text[..slashIndex];
        }

        bool success = int.TryParse(
            text,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int objIndex
        );

        if (!success || objIndex == 0)
        {
            throw new FormatException($"{location}: Invalid position index '{text}'.");
        }

        int index;

        if (objIndex > 0)
        {
            // Positive OBJ indices start at 1.
            index = objIndex - 1;
        }
        else
        {
            // Negative indices count backward from the positions read so far.
            index = vertexCount + objIndex;
        }

        if (index < 0 || index >= vertexCount)
        {
            throw new FormatException($"{location}: Position index '{text}' is out of range.");
        }

        return index;
    }
}
