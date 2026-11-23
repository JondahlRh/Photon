// -----------------------------------------------------------------------------
// Author:        Luca Lengl
// AI Assistance: Claude for Information, Bug and Warning
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
