// -----------------------------------------------------------------------------
// Author:        Florian Hipp
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------

using System;

namespace Photon.Core.Imaging;

/// <summary>
/// Converts linear colour values into 8-bit sRGB values.
/// </summary>
public static class SrgbConverter
{
    /// <summary>
    /// Converts a linear colour channel into an sRGB byte.
    /// </summary>
    /// <param name="linear">The linear colour channel.</param>
    /// <returns>The display channel from 0 to 255.</returns>
    public static byte ToByte(float linear)
    {
        float value = Math.Clamp(linear, 0f, 1f);

        float srgb =
            value <= 0.0031308f ? value * 12.92f : (1.055f * MathF.Pow(value, 1f / 2.4f)) - 0.055f;

        return (byte)MathF.Round(srgb * 255f);
    }
}
