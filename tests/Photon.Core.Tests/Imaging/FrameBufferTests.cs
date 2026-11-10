// -----------------------------------------------------------------------------
// Author:        Luca Lengl
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------

using System.Numerics;
using Photon.Core.Imaging;
using Photon.Core.Mathematics;

namespace Photon.Core.Tests.Imaging;

public sealed class FrameBufferTests
{
    [Fact]
    public void ConstructorSetsSizeAndStartsBlack()
    {
        var buffer = new FrameBuffer(4, 3);

        Assert.Equal(4, buffer.Width);
        Assert.Equal(3, buffer.Height);
        Assert.Equal(Vector3.Zero, buffer.GetPixel(0, 0));
        Assert.Equal(Vector3.Zero, buffer.GetPixel(3, 2));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    [InlineData(1, -1)]
    public void ConstructorRejectsInvalidSize(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FrameBuffer(width, height));
    }

    [Fact]
    public void SetPixelStoresColor()
    {
        var buffer = new FrameBuffer(4, 3);
        var color = new Vector3(0.25f, 0.5f, 1.5f);

        buffer.SetPixel(2, 1, color);

        Assert.Equal(color, buffer.GetPixel(2, 1));
        Assert.Equal(Vector3.Zero, buffer.GetPixel(1, 2));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(4, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 3)]
    public void PixelAccessOutsideImageThrows(int x, int y)
    {
        var buffer = new FrameBuffer(4, 3);

        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetPixel(x, y, Vector3.One));
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetPixel(x, y));
    }

    [Fact]
    public void GetRowReturnsPixelsLeftToRight()
    {
        var buffer = new FrameBuffer(3, 2);
        buffer.SetPixel(0, 1, Vector3.UnitX);
        buffer.SetPixel(1, 1, Vector3.UnitY);
        buffer.SetPixel(2, 1, Vector3.UnitZ);

        var row = buffer.GetRow(1).ToArray();

        Assert.Equal(new[] { Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ }, row);
    }

    [Fact]
    public void NewBufferHasNoCompletedRows()
    {
        var buffer = new FrameBuffer(2, 4);

        Assert.Equal(0, buffer.CompletedRowCount);
        Assert.True(FloatComparison.NearlyEqual(0f, buffer.Progress));
        Assert.False(buffer.IsComplete);
        Assert.False(buffer.IsRowCompleted(0));
    }

    [Fact]
    public void MarkRowCompletedUpdatesProgress()
    {
        var buffer = new FrameBuffer(2, 4);

        buffer.MarkRowCompleted(1);
        buffer.MarkRowCompleted(3);

        Assert.True(buffer.IsRowCompleted(1));
        Assert.False(buffer.IsRowCompleted(2));
        Assert.Equal(2, buffer.CompletedRowCount);
        Assert.True(FloatComparison.NearlyEqual(0.5f, buffer.Progress));
    }

    [Fact]
    public void MarkingRowTwiceCountsOnce()
    {
        var buffer = new FrameBuffer(2, 4);

        buffer.MarkRowCompleted(2);
        buffer.MarkRowCompleted(2);

        Assert.Equal(1, buffer.CompletedRowCount);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void RowAccessOutsideImageThrows(int y)
    {
        var buffer = new FrameBuffer(2, 4);

        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.MarkRowCompleted(y));
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.IsRowCompleted(y));
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetRow(y).Length);
    }

    [Fact]
    public void ParallelRowsAreAllCompleted()
    {
        const int width = 64;
        const int height = 256;
        var buffer = new FrameBuffer(width, height);

        Parallel.For(
            0,
            height,
            y =>
            {
                for (var x = 0; x < width; x++)
                {
                    buffer.SetPixel(x, y, new Vector3(x, y, 1f));
                }

                buffer.MarkRowCompleted(y);
            }
        );

        Assert.True(buffer.IsComplete);
        Assert.True(FloatComparison.NearlyEqual(1f, buffer.Progress));
        Assert.Equal(new Vector3(10f, 200f, 1f), buffer.GetPixel(10, 200));
    }
}
