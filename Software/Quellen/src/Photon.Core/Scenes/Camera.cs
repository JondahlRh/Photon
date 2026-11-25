// -----------------------------------------------------------------------------
// Author:        Luca Lengl
// AI Assistance: Claude for Information, Bug and Warning; ChatGPT Codex for automatic model framing
// -----------------------------------------------------------------------------

using System.Numerics;
using Photon.Core.Mathematics;

namespace Photon.Core.Scenes;

/// <summary>
/// The view definition from the project requirements: a view coordinate system (u, v, w),
/// the image size in pixels and the projection center.
/// </summary>
/// <remarks>
/// <para>
/// The origin of the view coordinate system is the image center; u + 1 is one pixel to the
/// right, v + 1 one pixel up, and w points towards the viewer (right-handed). The image plane
/// is w = 0, the projection center lies in front of it (w &gt; 0).
/// </para>
/// <para>
/// <see cref="ViewToWorld"/> maps view coordinates to world coordinates. <c>System.Numerics</c>
/// uses row vectors (p · M), so the translation is in the fourth row (M41, M42, M43).
/// </para>
/// </remarks>
public sealed class Camera
{
    /// <summary>
    /// The projection center in world coordinates, computed once.
    /// </summary>
    private readonly Vector3 _projectionCenterWorld;

    /// <summary>
    /// Initializes a new view definition.
    /// </summary>
    /// <param name="viewToWorld">
    /// The homogeneous 4×4 matrix that maps view coordinates to world coordinates
    /// (rotation, translation and scale; no projection).
    /// </param>
    /// <param name="width">The image width in pixels; greater than zero.</param>
    /// <param name="height">The image height in pixels; greater than zero.</param>
    /// <param name="projectionCenter">The projection center in view coordinates; its w (Z) must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the image size is zero or negative.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if the matrix is not invertible or contains a projection, or the projection center
    /// is not in front of the image plane.
    /// </exception>
    public Camera(Matrix4x4 viewToWorld, int width, int height, Vector3 projectionCenter)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        if (!Matrix4x4.Invert(viewToWorld, out _))
        {
            throw new ArgumentException("The matrix must be invertible.", nameof(viewToWorld));
        }

        // An affine matrix keeps the homogeneous coordinate at 1 (fourth column 0, 0, 0, 1).
        if (
            !FloatComparison.NearlyEqual(viewToWorld.M14, 0f)
            || !FloatComparison.NearlyEqual(viewToWorld.M24, 0f)
            || !FloatComparison.NearlyEqual(viewToWorld.M34, 0f)
            || !FloatComparison.NearlyEqual(viewToWorld.M44, 1f)
        )
        {
            throw new ArgumentException(
                "The matrix must not contain a projection.",
                nameof(viewToWorld)
            );
        }

        if (!float.IsFinite(projectionCenter.Z) || projectionCenter.Z <= FloatComparison.Epsilon)
        {
            throw new ArgumentException(
                "The projection center must lie in front of the image plane (w > 0).",
                nameof(projectionCenter)
            );
        }

        ViewToWorld = viewToWorld;
        Width = width;
        Height = height;
        ProjectionCenter = projectionCenter;
        _projectionCenterWorld = Vector3.Transform(projectionCenter, viewToWorld);
    }

    /// <summary>
    /// Gets the matrix that maps view coordinates to world coordinates.
    /// </summary>
    public Matrix4x4 ViewToWorld { get; }

    /// <summary>
    /// Gets the image width in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the image height in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets the projection center in view coordinates.
    /// </summary>
    public Vector3 ProjectionCenter { get; }

    /// <summary>
    /// Gets the projection center ("eye") in world coordinates.
    /// </summary>
    public Vector3 Position => _projectionCenterWorld;

    /// <summary>
    /// Gets the camera's upward direction in world coordinates.
    /// </summary>
    public Vector3 Up => Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitY, ViewToWorld));

    /// <summary>
    /// Creates a perspective camera that shows the complete bounding box with a small margin.
    /// </summary>
    /// <param name="bounds">The nonempty bounding box of the model in world coordinates.</param>
    /// <param name="width">The image width in pixels; greater than zero.</param>
    /// <param name="height">The image height in pixels; greater than zero.</param>
    /// <param name="yawDegrees">The horizontal rotation in degrees.</param>
    /// <param name="pitchDegrees">The vertical inclination in degrees.</param>
    /// <returns>A camera looking at the model center with a vertical field of view of 60 degrees.</returns>
    /// <exception cref="ArgumentException">Thrown if the bounds are empty or not finite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if an image dimension is not positive.</exception>
    public static Camera FrameBounds(
        Aabb bounds,
        int width,
        int height,
        float yawDegrees = 135f,
        float pitchDegrees = 35.26439f
    )
    {
        float yaw = yawDegrees * MathF.PI / 180f;
        float pitch = pitchDegrees * MathF.PI / 180f;

        Matrix4x4 rotation = Matrix4x4.CreateRotationX(-pitch) * Matrix4x4.CreateRotationY(yaw);

        return FrameBounds(bounds, width, height, Quaternion.CreateFromRotationMatrix(rotation));
    }

    /// <summary>
    /// Creates a camera fitted to the model with a freely chosen orientation.
    /// </summary>
    /// <param name="bounds">The finite, nonempty model bounds.</param>
    /// <param name="width">The positive image width.</param>
    /// <param name="height">The positive image height.</param>
    /// <param name="orientation">The finite, nonzero camera rotation.</param>
    /// <param name="zoom">The positive zoom factor; one uses the initial framing.</param>
    /// <returns>A camera looking towards the model center.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the bounds or orientation are invalid.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if an image dimension is not positive.
    /// </exception>
    public static Camera FrameBounds(
        Aabb bounds,
        int width,
        int height,
        Quaternion orientation,
        float zoom = 1f
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        if (
            bounds.IsEmpty
            || !VectorMath.IsFinite(bounds.Center)
            || !VectorMath.IsFinite(bounds.Size)
        )
        {
            throw new ArgumentException(
                "Model bounds must be finite and nonempty.",
                nameof(bounds)
            );
        }

        float lengthSquared = orientation.LengthSquared();

        if (!float.IsFinite(lengthSquared) || lengthSquared <= 0f)
        {
            throw new ArgumentException(
                "Camera orientation must be finite and nonzero.",
                nameof(orientation)
            );
        }

        if (!float.IsFinite(zoom) || zoom <= 0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(zoom),
                "Zoom must be finite and greater than zero."
            );
        }

        float verticalHalfAngle = MathF.PI / 6f;
        float horizontalHalfAngle = MathF.Atan(MathF.Tan(verticalHalfAngle) * width / height);

        float radius = MathF.Max(0.01f, bounds.Size.Length() * 0.5f);
        float distance =
            1.1f * radius / MathF.Sin(MathF.Min(verticalHalfAngle, horizontalHalfAngle));
        distance /= zoom;
        float pixelSize = 2f * distance * MathF.Tan(verticalHalfAngle) / height;

        Matrix4x4 rotation = Matrix4x4.CreateFromQuaternion(Quaternion.Normalize(orientation));

        Matrix4x4 viewToWorld =
            Matrix4x4.CreateScale(pixelSize)
            * rotation
            * Matrix4x4.CreateTranslation(bounds.Center);

        return new Camera(viewToWorld, width, height, new Vector3(0f, 0f, distance / pixelSize));
    }

    /// <summary>
    /// Creates the primary ray through the center of a pixel.
    /// </summary>
    /// <param name="x">The column, from 0 (left) to <see cref="Width"/> - 1.</param>
    /// <param name="y">The row, from 0 (top) to <see cref="Height"/> - 1.</param>
    /// <returns>The ray from the projection center through the pixel center, in world coordinates.</returns>
    public Ray CreatePrimaryRay(int x, int y)
    {
        return CreatePrimaryRay(x + 0.5f, y + 0.5f);
    }

    /// <summary>
    /// Creates the primary ray through any point of the image, e.g. for supersampling.
    /// </summary>
    /// <param name="imageX">The horizontal image position in pixels, from 0 (left edge) to <see cref="Width"/> (right edge).</param>
    /// <param name="imageY">The vertical image position in pixels, from 0 (top edge) to <see cref="Height"/> (bottom edge).</param>
    /// <returns>The ray from the projection center through the image point, in world coordinates.</returns>
    public Ray CreatePrimaryRay(float imageX, float imageY)
    {
        // Image coordinates count from the top left, view coordinates from the center upwards.
        var pointOnImagePlane = new Vector3(imageX - (Width * 0.5f), (Height * 0.5f) - imageY, 0f);
        Vector3 pointWorld = Vector3.Transform(pointOnImagePlane, ViewToWorld);

        return new Ray(_projectionCenterWorld, pointWorld - _projectionCenterWorld);
    }
}
