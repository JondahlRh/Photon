// -----------------------------------------------------------------------------
// Author:        BrackyDayn, MJZgh
// AI Assistance: Chatgpt codex for Summary, Expections, and help with String operations; Claude for code documentation and feedback
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using Photon.Core.Mathematics;
using Photon.Core.Scenes;

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
    /// <returns>The imported model and the warnings collected during the import.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the path is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown when the file cannot be read.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when an OBJ or MTL file contains invalid data, or no faces are found.
    /// </exception>
    public static ImportResult Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        List<Vector3> positions = new List<Vector3>();
        List<FaceCorner[]> faces = new List<FaceCorner[]>();
        List<int> materialIndices = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        var definedMaterials = new HashSet<string>(StringComparer.Ordinal);
        var warnings = new List<string>();
        var warnedKeywords = new HashSet<string>(StringComparer.Ordinal);

        List<Material> materials = new List<Material> { Material.Default };
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

                    var corners = new FaceCorner[parts.Length - 1];

                    for (int i = 1; i < parts.Length; i++)
                    {
                        corners[i - 1] = ParseCorner(
                            parts[i],
                            positions.Count,
                            normals.Count,
                            location
                        );
                    }

                    faces.Add(corners);

                    materialIndices.Add(currentMaterialIndex);
                    break;
                }

                case "vn":
                {
                    if (parts.Length != 4)
                    {
                        throw new FormatException(
                            $"{location}: Expected a normal in the form 'vn x y z'."
                        );
                    }

                    var normal = new Vector3(
                        ParseCoordinate(parts[1], location),
                        ParseCoordinate(parts[2], location),
                        ParseCoordinate(parts[3], location)
                    );

                    if (VectorMath.IsNearlyZero(normal))
                    {
                        throw new FormatException($"{location}: Normal has zero length.");
                    }

                    normals.Add(normal);
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

                        if (!File.Exists(materialPath))
                        {
                            warnings.Add(
                                $"{location}: Material library '{parts[i]}' was not found; "
                                    + "affected faces use a gray fallback."
                            );
                            continue;
                        }

                        LoadMaterials(
                            materialPath,
                            materials,
                            materialLookup,
                            definedMaterials,
                            warnings
                        );
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
                        materials.Add(CreateDefaultMaterial(name));
                    }

                    break;
                }
                case "o":
                case "g":
                case "s":
                case "vt":
                    // The preview uses one mesh and calculated face normals.
                    break;

                default:
                    if (warnedKeywords.Add(parts[0]))
                    {
                        warnings.Add(
                            $"{location}: OBJ statement '{parts[0]}' is not supported and was skipped."
                        );
                    }
                    break;
            }
        }

        if (faces.Count == 0)
        {
            throw new FormatException($"{path}: No triangular faces were found.");
        }

        for (int i = 1; i < materials.Count; i++)
        {
            if (!definedMaterials.Contains(materials[i].Name))
            {
                warnings.Add(
                    $"{path}: Material '{materials[i].Name}' is not defined in any material "
                        + "library; a gray fallback is used."
                );
            }
        }

        var builder = new ModelBuilder();

        foreach (Vector3 pos in positions)
        {
            builder.AddPosition(pos);
        }

        foreach (Vector3 normal in normals)
        {
            builder.AddNormal(normal);
        }

        for (int i = 1; i < materials.Count; i++)
        {
            builder.AddMaterial(materials[i]);
        }

        int activeMaterialIndex = 0;

        for (int i = 0; i < faces.Count; i++)
        {
            int materialIndex = materialIndices[i];
            if (materialIndex != activeMaterialIndex)
            {
                builder.UseMaterial(materials[materialIndex].Name);
                activeMaterialIndex = materialIndex;
            }

            builder.AddFace(faces[i]);
        }
        return new ImportResult(builder.Build(), warnings);
    }

    /// <summary>
    /// Loads named materials and their properties. A material that is defined again replaces the earlier one.
    /// </summary>
    /// <param name="path">The material library path.</param>
    /// <param name="materials">The model's material collection.</param>
    /// <param name="materialLookup">Material names mapped to collection indices.</param>
    /// <param name="definedMaterials">The names of all materials defined so far.</param>
    /// <param name="warnings">The collected warnings for unsupported statements.</param>
    /// <exception cref="IOException">
    /// Thrown when an existing material library cannot be read.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when a material name or diffuse RGB color is invalid.
    /// </exception>
    private static void LoadMaterials(
        string path,
        List<Material> materials,
        Dictionary<string, int> materialLookup,
        HashSet<string> definedMaterials,
        List<String> warnings
    )
    {
        if (!File.Exists(path))
        {
            return;
        }

        int materialIndex = -1;
        int lineNumber = 0;
        var warnedKeywords = new HashSet<string>(StringComparer.Ordinal);

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
                definedMaterials.Add(name);

                if (!materialLookup.TryGetValue(name, out materialIndex))
                {
                    materialIndex = materials.Count;
                    materialLookup.Add(name, materialIndex);
                    materials.Add(CreateDefaultMaterial(name));
                }
                else
                {
                    materials[materialIndex] = CreateDefaultMaterial(name);
                }
            }
            else if (IsMaterialProperty(parts[0]))
            {
                if (materialIndex < 0)
                {
                    throw new FormatException($"{location}: Expected 'Kd r g b' after 'newmtl'.");
                }

                materials[materialIndex] = ApplyProperty(materials[materialIndex], parts, location);
            }
            else if (warnedKeywords.Add(parts[0]))
            {
                warnings.Add(
                    $"{location}: Material '{parts[0]}' is not supported and was skipped."
                );
            }
        }
    }

    /// <summary>
    /// Determines whether a keyword sets a property of the current material.
    /// </summary>
    /// <param name="keyword">The first word of the statement.</param>
    /// <returns>True for the supported property keywords.</returns>
    private static bool IsMaterialProperty(string keyword)
    {
        return keyword is "Ka" or "Kd" or "Ks" or "Ns" or "d" or "Ni" or "illum";
    }

    /// <summary>
    /// Returns a copy of a material with one property changed.
    /// </summary>
    /// <param name="current">The material to copy.</param>
    /// <param name="parts">The split statement.</param>
    /// <param name="location">The file path and line number for error messages.</param>
    /// <returns>The changed material.</returns>
    /// <exception cref="FormatException">Thrown when the value is invalid or out of range.</exception>
    private static Material ApplyProperty(Material current, string[] parts, string location)
    {
        string keyword = parts[0];

        try
        {
            return keyword switch
            {
                "Ka" => Copy(current, ambient: ParseColor(parts, location)),
                "Kd" => Copy(current, diffuse: ParseColor(parts, location)),
                "Ks" => Copy(current, specular: ParseColor(parts, location)),
                "Ns" => Copy(current, specularExponent: ParseSingle(parts, location)),
                "d" => Copy(current, opacity: ParseSingle(parts, location)),
                "Ni" => Copy(current, refractiveIndex: ParseSingle(parts, location)),
                _ => Copy(current, illuminationModel: ParseIlluminationModel(parts, location)),
            };
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new FormatException(
                $"{location}: Invalid value for '{keyword}': {exception.Message}"
            );
        }
    }

    /// <summary>
    /// Parses an RGB color statement such as <c>Kd r g b</c>.
    /// </summary>
    /// <param name="parts">The split statement.</param>
    /// <param name="location">The file path and line number for error messages.</param>
    /// <returns>The parsed color.</returns>
    /// <exception cref="FormatException">Thrown when the statement has the wrong shape or an invalid number.</exception>
    private static Vector3 ParseColor(string[] parts, string location)
    {
        if (parts.Length != 4)
        {
            throw new FormatException($"{location}: Expected '{parts[0]} r g b'.");
        }

        return new Vector3(
            ParseCoordinate(parts[1], location),
            ParseCoordinate(parts[2], location),
            ParseCoordinate(parts[3], location)
        );
    }

    /// <summary>
    /// Parses a statement with one number, such as <c>Ns 50</c>.
    /// </summary>
    /// <param name="parts">The split statement.</param>
    /// <param name="location">The file path and line number for error messages.</param>
    /// <returns>The parsed number.</returns>
    /// <exception cref="FormatException">Thrown when the statement has the wrong shape or an invalid number.</exception>
    private static float ParseSingle(string[] parts, string location)
    {
        if (parts.Length != 2)
        {
            throw new FormatException($"{location}: Expected '{parts[0]} value'.");
        }

        return ParseCoordinate(parts[1], location);
    }

    /// <summary>
    /// Parses the illumination model of an <c>illum</c> statement.
    /// </summary>
    /// <param name="parts">The split statement.</param>
    /// <param name="location">The file path and line number for error messages.</param>
    /// <returns>The illumination model number.</returns>
    /// <exception cref="FormatException">Thrown when the value is not an integer.</exception>
    private static int ParseIlluminationModel(string[] parts, string location)
    {
        if (
            parts.Length != 2
            || !int.TryParse(
                parts[1],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int model
            )
        )
        {
            throw new FormatException($"{location}: Expected 'illum' followed by an integer.");
        }

        return model;
    }

    /// <summary>
    /// Creates a copy of a material with the given properties replaced.
    /// </summary>
    /// <param name="source">The material to copy.</param>
    /// <param name="ambient">The ambient reflection coefficient (<c>Ka</c>).</param>
    /// <param name="diffuse">The diffuse reflection coefficient (<c>Kd</c>).</param>
    /// <param name="specular">The specular reflection coefficient (<c>Ks</c>).</param>
    /// <param name="specularExponent">The Phong exponent (<c>Ns</c>); zero or greater.</param>
    /// <param name="opacity">The opacity (<c>d</c>), from 0 (fully transparent) to 1 (opaque).</param>
    /// <param name="refractiveIndex">The index of refraction (<c>Ni</c>); greater than zero.</param>
    /// <param name="illuminationModel">The MTL illumination model (<c>illum</c>), from 0 to 10.</param>
    /// <returns>The new material.</returns>
    private static Material Copy(
        Material source,
        Vector3? ambient = null,
        Vector3? diffuse = null,
        Vector3? specular = null,
        float? specularExponent = null,
        float? opacity = null,
        float? refractiveIndex = null,
        int? illuminationModel = null
    )
    {
        return new Material(
            name: source.Name,
            ambient: ambient ?? source.Ambient,
            diffuse: diffuse ?? source.Diffuse,
            specular: specular ?? source.Specular,
            specularExponent: specularExponent ?? source.SpecularExponent,
            opacity: opacity ?? source.Opacity,
            refractiveIndex: refractiveIndex ?? source.RefractiveIndex,
            reflectivity: source.Reflectivity,
            illuminationModel: illuminationModel ?? source.IlluminationModel
        );
    }

    /// <summary>
    /// Creates a material with the same values as <see cref="Material.Default"/> under the given name.
    /// </summary>
    /// <param name="name">The material name.</param>
    /// <returns>The default material with that name.</returns>
    private static Material CreateDefaultMaterial(string name)
    {
        Material defaults = Material.Default;
        return new Material(
            name,
            defaults.Ambient,
            defaults.Diffuse,
            defaults.Specular,
            defaults.SpecularExponent,
            defaults.Opacity,
            defaults.RefractiveIndex,
            defaults.Reflectivity,
            defaults.IlluminationModel
        );
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
    /// Parses one face corner of the form <c>v</c>, <c>v/vt</c>, <c>v//vn</c> or <c>v/vt/vn</c>.
    /// </summary>
    /// <param name="text">The corner text.</param>
    /// <param name="positionCount">The number of positions read so far.</param>
    /// <param name="normalCount">The number of normals read so far.</param>
    /// <param name="location">The file path and line number for error messages.</param>
    /// <returns>The corner with zero-based position and optional normal indices.</returns>
    /// <exception cref="FormatException">Thrown when an index is invalid or out of range.</exception>
    private static FaceCorner ParseCorner(
        string text,
        int positionCount,
        int normalCount,
        string location
    )
    {
        string[] fields = text.Split('/');

        if (fields.Length > 3)
        {
            throw new FormatException($"{location}: Invalid face vertex '{text}'.");
        }

        int positionIndex = ResolveIndex(fields[0], positionCount, "position", location);
        int? normalIndex = null;

        // The texture index in fields[1] is ignored.
        if (fields.Length == 3 && fields[2].Length > 0)
        {
            normalIndex = ResolveIndex(fields[2], normalCount, "normal", location);
        }

        return new FaceCorner(positionIndex, normalIndex);
    }

    /// <summary>
    /// Converts a one-based or negative OBJ index into a zero-based index.
    /// </summary>
    /// <param name="text">The index text.</param>
    /// <param name="count">The number of elements read so far.</param>
    /// <param name="kind">The element kind for error messages, such as "position".</param>
    /// <param name="location">The file path and line number for error messages.</param>
    /// <returns>The zero-based index.</returns>
    /// <exception cref="FormatException">Thrown when the index is zero, invalid, or out of range.</exception>
    private static int ResolveIndex(string text, int count, string kind, string location)
    {
        bool success = int.TryParse(
            text,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int objIndex
        );

        if (!success || objIndex == 0)
        {
            throw new FormatException($"{location}: Invalid {kind} index '{text}'.");
        }

        // Positive indices start at 1; negative indices count back from the end read so far.
        int index = objIndex > 0 ? objIndex - 1 : count + objIndex;

        if (index < 0 || index >= count)
        {
            throw new FormatException($"{location}: {kind} index '{text}' is out of range.");
        }

        return index;
    }
}
