// -----------------------------------------------------------------------------
// Author:        ChatGPT Codex
// AI Assistance: ChatGPT Codex for render integration tests
// -----------------------------------------------------------------------------

using Photon.Application.Tests.Support;
using Photon.Application.ViewModels;

namespace Photon.Application.Tests.ViewModels;

public sealed class MainWindowViewModelRenderingTests
{
    [Fact]
    public async Task StartRenderWithoutModelShowsMessage()
    {
        var viewModel = new MainWindowViewModel();

        await viewModel.StartRenderCommand.ExecuteAsync(null);

        Assert.Equal("Import a model before rendering.", viewModel.StatusText);
        Assert.Null(viewModel.RenderResult);
        Assert.False(viewModel.HasRenderResult);
        Assert.False(viewModel.IsResultVisible);
        Assert.Equal(0d, viewModel.ProgressPercent);
    }

    [Fact]
    public async Task RenderPublishesImportedGeometryAtRequestedResolution()
    {
        using var model = new TestObjFile();
        var viewModel = new MainWindowViewModel { RenderWidth = 24, RenderHeight = 16 };
        await viewModel.LoadModelAsync(model.Path);
        bool? completeWhenPublished = null;
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.RenderResult))
            {
                completeWhenPublished = viewModel.RenderResult?.IsComplete;
            }
        };

        await viewModel.StartRenderCommand.ExecuteAsync(null);

        var result = viewModel.RenderResult;
        Assert.NotNull(result);
        Assert.Equal(24, result.Width);
        Assert.Equal(16, result.Height);
        Assert.True(completeWhenPublished);
        Assert.True(result.GetPixel(result.Width / 2, result.Height / 2).LengthSquared() > 0.01f);
        Assert.True(viewModel.HasRenderResult);
        Assert.True(viewModel.IsResultVisible);
        Assert.Equal(100d, viewModel.ProgressPercent);
        Assert.Equal("Render finished.", viewModel.StatusText);
    }

    [Theory]
    [InlineData(0, 16)]
    [InlineData(-1, 16)]
    [InlineData(16, 0)]
    [InlineData(16, -1)]
    [InlineData(int.MaxValue, 2)]
    public async Task InvalidResolutionDoesNotPublishResult(int width, int height)
    {
        using var model = new TestObjFile();
        var viewModel = new MainWindowViewModel { RenderWidth = width, RenderHeight = height };
        await viewModel.LoadModelAsync(model.Path);

        await viewModel.StartRenderCommand.ExecuteAsync(null);

        Assert.StartsWith("Render failed:", viewModel.StatusText);
        Assert.Null(viewModel.RenderResult);
        Assert.False(viewModel.HasRenderResult);
        Assert.False(viewModel.IsResultVisible);
        Assert.Equal(0d, viewModel.ProgressPercent);
    }

    [Fact]
    public async Task RenderingAgainReplacesFrameAndUsesNewResolution()
    {
        using var model = new TestObjFile();
        var viewModel = new MainWindowViewModel { RenderWidth = 16, RenderHeight = 16 };
        await viewModel.LoadModelAsync(model.Path);
        await viewModel.StartRenderCommand.ExecuteAsync(null);
        var firstResult = viewModel.RenderResult;
        Assert.NotNull(firstResult);

        viewModel.RenderWidth = 32;
        viewModel.RenderHeight = 12;
        await viewModel.StartRenderCommand.ExecuteAsync(null);

        var secondResult = viewModel.RenderResult;
        Assert.NotNull(secondResult);
        Assert.NotSame(firstResult, secondResult);
        Assert.Equal(32, secondResult.Width);
        Assert.Equal(12, secondResult.Height);
        Assert.True(firstResult.IsComplete);
        Assert.True(secondResult.IsComplete);
    }

    [Fact]
    public async Task EditSceneAndShowResultPreserveCompletedFrame()
    {
        using var model = new TestObjFile();
        var viewModel = new MainWindowViewModel { RenderWidth = 16, RenderHeight = 16 };
        await viewModel.LoadModelAsync(model.Path);
        await viewModel.StartRenderCommand.ExecuteAsync(null);
        var result = viewModel.RenderResult;
        Assert.NotNull(result);

        viewModel.EditSceneCommand.Execute(null);

        Assert.False(viewModel.IsResultVisible);
        Assert.True(viewModel.HasRenderResult);
        Assert.Same(result, viewModel.RenderResult);

        viewModel.ShowResultCommand.Execute(null);

        Assert.True(viewModel.IsResultVisible);
        Assert.Same(result, viewModel.RenderResult);
    }
}
