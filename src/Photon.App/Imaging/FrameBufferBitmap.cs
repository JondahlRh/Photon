// -----------------------------------------------------------------------------
// Author:        BrackyDayn, inspiration from https://github.com/AvaloniaUI/Avalonia/blob/main/samples/RenderDemo/Pages/WriteableBitmapPage.cs
// AI Assistance: ChatGPT Codex for bitmap conversion and tests
// -----------------------------------------------------------------------------
using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Photon.Core.Imaging;

namespace Photon.App.Imaging;

/// <summary>
/// Converts completed render buffers into displayable images.
/// </summary>
internal static class FrameBufferBitmap
{
    /// <summary>
    /// Creates a bitmap from a completed render buffer.
    /// </summary>
    /// <param name="buffer">The completed render buffer.</param>
    /// <returns>A bitmap that the caller must dispose when no longer needed.</returns>
    public static WriteableBitmap Create(FrameBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (!buffer.IsComplete)
        {
            throw new ArgumentException("The render buffer must be complete.", nameof(buffer));
        }

        byte[] row = new byte[checked(buffer.Width * 4)];

        var bitmap = new WriteableBitmap(
            new PixelSize(buffer.Width, buffer.Height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque
        );

        try
        {
            using var destination = bitmap.Lock();

            for (int y = 0; y < buffer.Height; y++)
            {
                var source = buffer.GetRow(y);
                for (int x = 0; x < buffer.Width; x++)
                {
                    var color = source[x];
                    int offset = x * 4;

                    row[offset] = ToSrgbByte(color.Z);
                    row[offset + 1] = ToSrgbByte(color.Y);
                    row[offset + 2] = ToSrgbByte(color.X);
                    row[offset + 3] = 255;
                }

                Marshal.Copy(
                    row,
                    0,
                    IntPtr.Add(destination.Address, checked(y * destination.RowBytes)),
                    row.Length
                );
            }
            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Converts a linear colour channel into an sRGB byte.
    /// </summary>
    /// <param name="linear">The linear colour channel.</param>
    /// <returns>The display channel from 0 to 255.</returns>
    private static byte ToSrgbByte(float linear)
    {
        float value = Math.Clamp(linear, 0f, 1f);

        float srgb =
            value <= 0.0031308f ? value * 12.92f : (1.055f * MathF.Pow(value, 1f / 2.4f)) - 0.055f;

        return (byte)MathF.Round(srgb * 255f);
    }
}
