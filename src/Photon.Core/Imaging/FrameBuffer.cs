// -----------------------------------------------------------------------------
// Author:        Luca Lengl
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------

using System.Numerics;

namespace Photon.Core.Imaging;

/// <summary>
/// Holds the rendered image as linear RGB color values and tracks which rows are finished.
/// </summary>
public sealed class FrameBuffer
{
    /// <summary>
    /// The pixel colors in row-major order (index = y * width + x).
    /// </summary>
    private readonly Vector3[] _pixels;

    /// <summary>
    /// One flag per row: 0 while the row is being rendered, 1 once it is completed.
    /// </summary>
    private readonly int[] _completedRows;

    /// <summary>
    /// The number of rows marked as completed.
    /// </summary>
    private int _completedRowCount;

    /// <summary>
    /// Initializes a new frame buffer filled with black.
    /// </summary>
    /// <param name="width">The image width in pixels; must be greater than zero.</param>
    /// <param name="height">The image height in pixels; must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="width"/> or <paramref name="height"/> is zero or negative.
    /// </exception>
    /// <exception cref="OverflowException">Thrown if the total number of pixels is too large.</exception>
    public FrameBuffer(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        Width = width;
        Height = height;
        _pixels = new Vector3[checked(width * height)];
        _completedRows = new int[height];
    }

    /// <summary>
    /// Gets the image width in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the image height in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets the number of rows that are marked as completed.
    /// </summary>
    public int CompletedRowCount => Volatile.Read(ref _completedRowCount);

    /// <summary>
    /// Gets the share of completed rows, from 0 (nothing rendered) to 1 (image complete).
    /// </summary>
    public float Progress => (float)CompletedRowCount / Height;

    /// <summary>
    /// Gets a value indicating whether all rows are marked as completed.
    /// </summary>
    public bool IsComplete => CompletedRowCount == Height;

    /// <summary>
    /// Sets the color of a single pixel.
    /// </summary>
    /// <param name="x">The column, from 0 (left) to <see cref="Width"/> - 1.</param>
    /// <param name="y">The row, from 0 (top) to <see cref="Height"/> - 1.</param>
    /// <param name="color">The linear RGB color.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the coordinates are outside the image.</exception>
    public void SetPixel(int x, int y, Vector3 color)
    {
        _pixels[GetIndex(x, y)] = color;
    }

    /// <summary>
    /// Gets the color of a single pixel.
    /// </summary>
    /// <param name="x">The column, from 0 (left) to <see cref="Width"/> - 1.</param>
    /// <param name="y">The row, from 0 (top) to <see cref="Height"/> - 1.</param>
    /// <returns>The linear RGB color of the pixel.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the coordinates are outside the image.</exception>
    public Vector3 GetPixel(int x, int y)
    {
        return _pixels[GetIndex(x, y)];
    }

    /// <summary>
    /// Gets all pixels of one row without copying them.
    /// </summary>
    /// <param name="y">The row, from 0 (top) to <see cref="Height"/> - 1.</param>
    /// <returns>The colors of the row from left to right.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="y"/> is outside the image.</exception>
    /// <remarks>Only read rows for which <see cref="IsRowCompleted"/> returns <c>true</c>.</remarks>
    public ReadOnlySpan<Vector3> GetRow(int y)
    {
        ValidateRow(y);
        return _pixels.AsSpan(y * Width, Width);
    }

    /// <summary>
    /// Marks a row as completed so readers may use it. Marking a row twice has no further effect.
    /// </summary>
    /// <param name="y">The row, from 0 (top) to <see cref="Height"/> - 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="y"/> is outside the image.</exception>
    public void MarkRowCompleted(int y)
    {
        ValidateRow(y);

        // Exchange publishes the pixel writes of this row before the flag becomes visible
        // and tells us whether the row was already marked, so the counter stays correct.
        if (Interlocked.Exchange(ref _completedRows[y], 1) == 0)
        {
            Interlocked.Increment(ref _completedRowCount);
        }
    }

    /// <summary>
    /// Determines whether a row is marked as completed.
    /// </summary>
    /// <param name="y">The row, from 0 (top) to <see cref="Height"/> - 1.</param>
    /// <returns><c>true</c> if the row is completed; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="y"/> is outside the image.</exception>
    public bool IsRowCompleted(int y)
    {
        ValidateRow(y);
        return Volatile.Read(ref _completedRows[y]) == 1;
    }

    /// <summary>
    /// Converts pixel coordinates into the index of <see cref="_pixels"/>.
    /// </summary>
    /// <param name="x">The column.</param>
    /// <param name="y">The row.</param>
    /// <returns>The index <c>y * Width + x</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the coordinates are outside the image.</exception>
    private int GetIndex(int x, int y)
    {
        // Unsigned comparison checks both "< 0" and ">= size" in one step.
        if ((uint)x >= (uint)Width)
        {
            throw new ArgumentOutOfRangeException(nameof(x), x, "Column is outside the image.");
        }

        ValidateRow(y);
        return (y * Width) + x;
    }

    /// <summary>
    /// Ensures that a row index lies inside the image.
    /// </summary>
    /// <param name="y">The row to check.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="y"/> is outside the image.</exception>
    private void ValidateRow(int y)
    {
        if ((uint)y >= (uint)Height)
        {
            throw new ArgumentOutOfRangeException(nameof(y), y, "Row is outside the image.");
        }
    }
}
