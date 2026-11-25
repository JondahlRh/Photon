// -----------------------------------------------------------------------------
// Author:        Florian
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------
using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
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

        if (!buffer.IsComplete)
        {
            throw new ArgumentException("The render buffer must be complete.", nameof(buffer));
        }

        // runs in background so the UI does not freeze
        return Task.Run(() => Export(buffer, path));
    }

    /// <summary>Writes the frame buffer as PNG file.</summary>
    /// <param name="buffer">The frame buffer to export.</param>
    /// <param name="path">The target file path.</param>
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
                    new SKColor(
                        SrgbConverter.ToByte(color.X),
                        SrgbConverter.ToByte(color.Y),
                        SrgbConverter.ToByte(color.Z)
                    )
                );
            }
        }

        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        using FileStream stream = File.Create(path);
        data.SaveTo(stream);
    }
}
