// -----------------------------------------------------------------------------
// Author:        Claude
// AI Assistance: Claude for test fixture implementation
// -----------------------------------------------------------------------------
using System.Numerics;
using Photon.App.Platform.Skia;
using Photon.Core.Imaging;
using SkiaSharp;

namespace Photon.App.Tests.Platform.Skia;

public sealed class SkiaImageExporterTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

    public void Dispose()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }

    [Fact]
    public async Task NullBufferIsRejected()
    {
        var exporter = new SkiaImageExporter();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            exporter.ExportPngAsync(null!, _path)
        );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InvalidPathIsRejected(string? path)
    {
        var exporter = new SkiaImageExporter();

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            exporter.ExportPngAsync(CreateBuffer(1, 1), path!)
        );
    }

    [Fact]
    public async Task ExportWritesPngWithSizeAndPixelOrder()
    {
        var buffer = CreateBuffer(3, 2);
        buffer.SetPixel(0, 0, Vector3.UnitX);
        buffer.SetPixel(1, 0, Vector3.UnitY);
        buffer.SetPixel(2, 0, Vector3.UnitZ);
        buffer.SetPixel(0, 1, Vector3.One);
        buffer.SetPixel(1, 1, Vector3.Zero);
        buffer.SetPixel(2, 1, new Vector3(1f, 1f, 0f));

        await new SkiaImageExporter().ExportPngAsync(buffer, _path);

        using var bitmap = SKBitmap.Decode(_path);
        Assert.NotNull(bitmap);
        Assert.Equal(3, bitmap.Width);
        Assert.Equal(2, bitmap.Height);
        Assert.Equal(new SKColor(255, 0, 0), bitmap.GetPixel(0, 0));
        Assert.Equal(new SKColor(0, 255, 0), bitmap.GetPixel(1, 0));
        Assert.Equal(new SKColor(0, 0, 255), bitmap.GetPixel(2, 0));
        Assert.Equal(new SKColor(255, 255, 255), bitmap.GetPixel(0, 1));
        Assert.Equal(new SKColor(0, 0, 0), bitmap.GetPixel(1, 1));
        Assert.Equal(new SKColor(255, 255, 0), bitmap.GetPixel(2, 1));
    }

    // Gleiche Werte wie in FrameBufferBitmapTests -> Export == Anzeige
    [Theory]
    [InlineData(-1f, 0)]
    [InlineData(0f, 0)]
    [InlineData(0.001f, 3)]
    [InlineData(0.0031308f, 10)]
    [InlineData(0.18f, 118)]
    [InlineData(0.5f, 188)]
    [InlineData(1f, 255)]
    [InlineData(2f, 255)]
    public async Task LinearColorsAreEncodedAsSrgbLikeTheDisplay(float linear, int expected)
    {
        var buffer = CreateBuffer(1, 1);
        buffer.SetPixel(0, 0, new Vector3(linear));

        await new SkiaImageExporter().ExportPngAsync(buffer, _path);

        using var bitmap = SKBitmap.Decode(_path);
        byte c = (byte)expected;
        Assert.Equal(new SKColor(c, c, c, 255), bitmap.GetPixel(0, 0));
    }

    private static FrameBuffer CreateBuffer(int width, int height)
    {
        var buffer = new FrameBuffer(width, height);
        for (int y = 0; y < height; y++)
        {
            buffer.MarkRowCompleted(y);
        }

        return buffer;
    }
}
