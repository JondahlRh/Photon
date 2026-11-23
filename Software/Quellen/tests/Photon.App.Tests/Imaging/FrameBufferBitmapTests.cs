// -----------------------------------------------------------------------------
// Author:        ChatGPT Codex
// AI Assistance: ChatGPT Codex for bitmap conversion regression tests
// -----------------------------------------------------------------------------

using System.Numerics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Photon.App.Imaging;
using Photon.Core.Imaging;

namespace Photon.App.Tests.Imaging;

public sealed class FrameBufferBitmapTests : IClassFixture<AvaloniaFixture>
{
    [Fact]
    public void NullBufferIsRejected()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => FrameBufferBitmap.Create(null!));

        Assert.Equal("buffer", exception.ParamName);
    }

    [Fact]
    public void PartlyRenderedBufferIsRejected()
    {
        var buffer = new FrameBuffer(3, 2);
        buffer.MarkRowCompleted(0);

        var exception = Assert.Throws<ArgumentException>(() => FrameBufferBitmap.Create(buffer));

        Assert.Equal("buffer", exception.ParamName);
    }

    [Fact]
    public void BitmapPreservesPixelOrderAcrossNonSquareRows()
    {
        var buffer = new FrameBuffer(3, 2);
        buffer.SetPixel(0, 0, Vector3.UnitX);
        buffer.SetPixel(1, 0, Vector3.UnitY);
        buffer.SetPixel(2, 0, Vector3.UnitZ);
        buffer.SetPixel(0, 1, Vector3.One);
        buffer.SetPixel(1, 1, Vector3.Zero);
        buffer.SetPixel(2, 1, new Vector3(1f, 1f, 0f));
        buffer.MarkRowCompleted(0);
        buffer.MarkRowCompleted(1);

        using var bitmap = FrameBufferBitmap.Create(buffer);

        Assert.Equal(new PixelSize(3, 2), bitmap.PixelSize);
        Assert.Equal(PixelFormat.Bgra8888, bitmap.Format);
        Assert.Equal(AlphaFormat.Opaque, bitmap.AlphaFormat);
        Assert.Equal(
            new byte[] { 0, 0, 255, 255, 0, 255, 0, 255, 255, 0, 0, 255 },
            ReadRow(bitmap, 0)
        );
        Assert.Equal(
            new byte[] { 255, 255, 255, 255, 0, 0, 0, 255, 0, 255, 255, 255 },
            ReadRow(bitmap, 1)
        );
    }

    [Theory]
    [InlineData(-1f, 0)]
    [InlineData(0f, 0)]
    [InlineData(0.001f, 3)]
    [InlineData(0.0031308f, 10)]
    [InlineData(0.18f, 118)]
    [InlineData(0.5f, 188)]
    [InlineData(1f, 255)]
    [InlineData(2f, 255)]
    public void LinearColorsAreEncodedAsSrgbAndClamped(float linear, int expected)
    {
        var buffer = new FrameBuffer(1, 1);
        buffer.SetPixel(0, 0, new Vector3(linear));
        buffer.MarkRowCompleted(0);

        using var bitmap = FrameBufferBitmap.Create(buffer);

        byte channel = (byte)expected;
        Assert.Equal(new byte[] { channel, channel, channel, 255 }, ReadRow(bitmap, 0));
    }

    private static byte[] ReadRow(WriteableBitmap bitmap, int y)
    {
        using var frame = bitmap.Lock();
        byte[] row = new byte[bitmap.PixelSize.Width * 4];
        Marshal.Copy(IntPtr.Add(frame.Address, y * frame.RowBytes), row, 0, row.Length);
        return row;
    }
}
