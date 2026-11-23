// -----------------------------------------------------------------------------
// Author:        Luca Lengl
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;
using System.Text;
using Photon.Core.Imaging;

namespace Photon.Core.Tests.Rendering;

/// <summary>
/// Writes a frame buffer as plain PPM so renders can be checked by eye until the image export exists.
/// </summary>
internal static class PpmWriter
{
    /// <summary>
    /// Writes all pixels of the buffer to a plain PPM file (P3, 8 bits per channel).
    /// </summary>
    /// <param name="buffer">The buffer with linear RGB colors.</param>
    /// <param name="path">The file to create or overwrite.</param>
    public static void Write(FrameBuffer buffer, string path)
    {
        var builder = new StringBuilder();
        builder.Append(CultureInfo.InvariantCulture, $"P3\n{buffer.Width} {buffer.Height}\n255\n");

        for (var y = 0; y < buffer.Height; y++)
        {
            foreach (Vector3 color in buffer.GetRow(y))
            {
                builder.Append(
                    CultureInfo.InvariantCulture,
                    $"{ToByte(color.X)} {ToByte(color.Y)} {ToByte(color.Z)}\n"
                );
            }
        }

        File.WriteAllText(path, builder.ToString());
    }

    /// <summary>
    /// Clamps a color channel to [0, 1] and scales it to [0, 255].
    /// </summary>
    /// <param name="value">The linear channel value.</param>
    /// <returns>The channel as an integer from 0 to 255.</returns>
    private static int ToByte(float value)
    {
        return (int)MathF.Round(Math.Clamp(value, 0f, 1f) * 255f);
    }
}
