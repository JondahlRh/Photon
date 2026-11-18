// -----------------------------------------------------------------------------
// Author:        Florian
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------
using System;
using System.IO;
using System.Numerics;
using Photon.Core.Imaging;
using SkiaSharp;

namespace Photon.App.Platform.Skia;

/// <summary>
/// Exports a frame buffer as PNG using SkiaSharp
/// </summary>
public class SkiaImageExporter : IImageExporter
{
    /// <inheritdoc/>
    public Task ExportPngAsync(FrameBuffer buffer, string path)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // runs in background so the UI does not freeze
        return Task.Run(() => Export(buffer, path));
    }

    /// Writes the frame buffer as PNG file/>
    private static void Export(FrameBuffer buffer, string path)
    {
        using SKBitmap bitmap = new SKBitmap(
            buffer.Width,
            buffer.Height,
            SKColorType.Rgba8888,
            SKAlphaType.Opaque
        );

        // copy every pixel from the frame buffer into the bitmap
        for (int y = 0; y < buffer.Height; y++)
        {
            for (int x = 0; x < buffer.Width; x++)
            {
                Vector3 color = buffer.GetPixel(x, y);
                bitmap.SetPixel(
                    x,
                    y,
                    new SKColor(ToByte(color.X), ToByte(color.Y), ToByte(color.Z))
                );
            }
        }

        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        using FileStream stream = File.Create(path);
        data.SaveTo(stream);
    }

    /// <summary>
    /// Converts a linear color value to a gamma corrected byte
    /// </summary>
    private static byte ToByte(float linear)
    {
        float clamped = Math.Clamp(linear, 0f, 1f);
        float gamma = MathF.Pow(clamped, 1f / 2.2f);
        return (byte)((gamma * 255f) + 0.5f);
    }
}
