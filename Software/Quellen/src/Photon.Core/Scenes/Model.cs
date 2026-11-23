// -----------------------------------------------------------------------------
// Author:        Luca Lengl
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------

using Photon.Core.Mathematics;

namespace Photon.Core.Scenes;

/// <summary>
/// The geometry of a scene: all meshes and, as a flat list, every primitive a ray can hit.
/// </summary>
/// <remarks>
/// Usually created by <see cref="ModelBuilder"/>. The flat <see cref="Primitives"/> list is the
/// input for the acceleration structure; it holds the triangles of all meshes followed by
/// standalone primitives such as spheres.
/// </remarks>
public sealed class Model
{
    /// <summary>
    /// The meshes of the model.
    /// </summary>
    private readonly Mesh[] _meshes;

    /// <summary>
    /// All primitives of the model.
    /// </summary>
    private readonly IPrimitive[] _primitives;

    /// <summary>
    /// Initializes a new model.
    /// </summary>
    /// <param name="meshes">The meshes of the model.</param>
    /// <param name="additionalPrimitives">Standalone primitives that are not part of a mesh, or <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="meshes"/> or one of the elements is null.</exception>
    public Model(IEnumerable<Mesh> meshes, IEnumerable<IPrimitive>? additionalPrimitives = null)
    {
        ArgumentNullException.ThrowIfNull(meshes);

        _meshes = meshes.ToArray();
        IPrimitive[] extra = additionalPrimitives?.ToArray() ?? [];
        var primitives = new List<IPrimitive>(extra.Length);
        Aabb bounds = Aabb.Empty;

        foreach (Mesh mesh in _meshes)
        {
            ArgumentNullException.ThrowIfNull(mesh, nameof(meshes));
            primitives.AddRange(mesh.Triangles);
            bounds = bounds.Include(mesh.Bounds);
            TriangleCount += mesh.Triangles.Count;
        }

        foreach (IPrimitive primitive in extra)
        {
            ArgumentNullException.ThrowIfNull(primitive, nameof(additionalPrimitives));
            primitives.Add(primitive);
            bounds = bounds.Include(primitive.Bounds);
        }

        _primitives = primitives.ToArray();
        Bounds = bounds;
    }

    /// <summary>
    /// Gets the meshes of the model.
    /// </summary>
    public IReadOnlyList<Mesh> Meshes => _meshes;

    /// <summary>
    /// Gets every primitive of the model: the triangles of all meshes and standalone primitives.
    /// </summary>
    public IReadOnlyList<IPrimitive> Primitives => _primitives;

    /// <summary>
    /// Gets the number of triangles in all meshes.
    /// </summary>
    public int TriangleCount { get; }

    /// <summary>
    /// Gets the box that contains the whole model; <see cref="Aabb.IsEmpty"/> for an empty model.
    /// </summary>
    public Aabb Bounds { get; }
}
