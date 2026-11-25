// -----------------------------------------------------------------------------
// Author:        Luca Lengl
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------

namespace Photon.Core.Scenes;

/// <summary>
/// An immutable state of model, lighting and view that a render job works with.
/// A change creates a new instance, so a running render job is never affected.
/// </summary>
public sealed class Scene
{
    /// <summary>
    /// Initializes a new scene.
    /// </summary>
    /// <param name="model">The geometry.</param>
    /// <param name="lighting">The ambient light and light sources.</param>
    /// <param name="camera">The view definition.</param>
    /// <exception cref="ArgumentNullException">Thrown if an argument is null.</exception>
    public Scene(Model model, Lighting lighting, Camera camera)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(lighting);
        ArgumentNullException.ThrowIfNull(camera);

        Model = model;
        Lighting = lighting;
        Camera = camera;
    }

    /// <summary>
    /// Gets the geometry.
    /// </summary>
    public Model Model { get; }

    /// <summary>
    /// Gets the ambient light and light sources.
    /// </summary>
    public Lighting Lighting { get; }

    /// <summary>
    /// Gets the view definition.
    /// </summary>
    public Camera Camera { get; }
}
