// -----------------------------------------------------------------------------
// Author:        Claude
// AI Assistance: Claude for test fixture implementation
// -----------------------------------------------------------------------------

using Photon.Application.ViewModels;
using Photon.Core.Imaging;

namespace Photon.Application.Tests.ViewModels;

public sealed class MainWindowViewModelExportTests
{
    [Fact]
    public async Task SavePngPassesRenderResultAndPathToExporter()
    {
        var exporter = new FakeImageExporter();
        var buffer = new FrameBuffer(2, 2);
        var viewModel = new MainWindowViewModel(null, exporter) { RenderResult = buffer };

        await viewModel.SavePngAsync("/tmp/out/render.png");

        Assert.Equal(1, exporter.CallCount);
        Assert.Same(buffer, exporter.Buffer);
        Assert.Equal("/tmp/out/render.png", exporter.Path);
        Assert.Equal("Saved render.png.", viewModel.StatusText);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task SavePngRejectsInvalidPath(string? path)
    {
        var exporter = new FakeImageExporter();
        var viewModel = new MainWindowViewModel(null, exporter)
        {
            RenderResult = new FrameBuffer(1, 1),
        };

        await Assert.ThrowsAnyAsync<ArgumentException>(() => viewModel.SavePngAsync(path!));
        Assert.Equal(0, exporter.CallCount);
    }

    [Fact]
    public async Task SavePngWithoutExporterThrows()
    {
        var viewModel = new MainWindowViewModel { RenderResult = new FrameBuffer(1, 1) };

        await Assert.ThrowsAsync<InvalidOperationException>(() => viewModel.SavePngAsync("a.png"));
    }

    [Fact]
    public async Task SavePngWithoutRenderResultThrows()
    {
        var exporter = new FakeImageExporter();
        var viewModel = new MainWindowViewModel(null, exporter);

        await Assert.ThrowsAsync<InvalidOperationException>(() => viewModel.SavePngAsync("a.png"));
        Assert.Equal(0, exporter.CallCount);
    }

    [Fact]
    public async Task SavePngPropagatesExporterFailure()
    {
        var exporter = new FakeImageExporter { Failure = new IOException("disk full") };
        var viewModel = new MainWindowViewModel(null, exporter)
        {
            RenderResult = new FrameBuffer(1, 1),
        };

        await Assert.ThrowsAsync<IOException>(() => viewModel.SavePngAsync("a.png"));
    }

    private sealed class FakeImageExporter : IImageExporter
    {
        public int CallCount { get; private set; }
        public FrameBuffer? Buffer { get; private set; }
        public string? Path { get; private set; }
        public Exception? Failure { get; init; }

        public Task ExportPngAsync(FrameBuffer buffer, string path)
        {
            CallCount++;
            Buffer = buffer;
            Path = path;
            return Failure is null ? Task.CompletedTask : Task.FromException(Failure);
        }
    }
}
