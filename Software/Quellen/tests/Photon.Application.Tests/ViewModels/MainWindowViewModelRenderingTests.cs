// -----------------------------------------------------------------------------
// Author:        ChatGPT Codex
// AI Assistance: ChatGPT Codex for render integration tests
// -----------------------------------------------------------------------------

using System.Numerics;
using Photon.Application.Tests.Support;
using Photon.Application.ViewModels;
using Photon.Core.Imaging;
using Photon.Core.Rendering;

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
        viewModel.AddLightCommand.Execute(null);
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

    [Fact]
    public async Task PointLightFromUiLightsAndShadowsTheRender()
    {
        // A floor (8 x 8 at y = 0) and a small square (2 x 2 at y = 1) above its center.
        using var model = new TestObjFile(
            """
            v -4 0 -4
            v 4 0 -4
            v 4 0 4
            v -4 0 4
            v -1 1 -1
            v 1 1 -1
            v 1 1 1
            v -1 1 1
            f 1 4 3
            f 1 3 2
            f 5 8 7
            f 5 7 6
            """
        );
        var viewModel = new MainWindowViewModel { RenderWidth = 64, RenderHeight = 64 };
        await viewModel.LoadModelAsync(model.Path);

        // Look straight down; image right is +x.
        viewModel.CameraOrientation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI / 2f);

        await viewModel.StartRenderCommand.ExecuteAsync(null);
        FrameBuffer unlit = Assert.IsType<FrameBuffer>(viewModel.RenderResult);

        viewModel.AddLightCommand.Execute(null);
        LightViewModel light = Assert.IsType<LightViewModel>(viewModel.Light);
        light.PositionX = 0f;
        light.PositionY = 2f;
        light.PositionZ = 0f;
        await viewModel.StartRenderCommand.ExecuteAsync(null);
        FrameBuffer lit = Assert.IsType<FrameBuffer>(viewModel.RenderResult);

        // The camera frames the scene from about 13 units above the floor, so one floor unit
        // covers about 4.3 pixels. Seen from the light at height 2, the square shades the
        // floor up to |x| = 2; seen from the camera, it hides the floor only up to |x| ~ 1.1.
        const int row = 32;
        const int shadowColumn = 38; // floor at x ~ 1.5: in the shadow
        const int litFloorColumn = 44; // floor at x ~ 2.9: lit

        Vector3 litFloor = lit.GetPixel(litFloorColumn, row);
        Assert.True(
            Brightness(litFloor) > Brightness(unlit.GetPixel(litFloorColumn, row)) + 0.5f,
            $"The light does not brighten the floor: {litFloor}."
        );

        // In the shadow only the ambient light remains, exactly as without any light source.
        Vector3 shadow = lit.GetPixel(shadowColumn, row);
        Assert.True(
            Vector3.Distance(unlit.GetPixel(shadowColumn, row), shadow) < 1e-4f,
            $"The floor next to the square is not shadowed: {shadow}."
        );
        Assert.True(Brightness(shadow) > 0f);
    }

    [Theory]
    [InlineData(100f, 200f, 150f, 60f, 48, 32)]
    [InlineData(0.01f, 0f, 0f, 0f, 48, 32)]
    [InlineData(20f, -200f, -150f, -60f, 24, 48)]
    public async Task RenderFramesModelsWithDifferentSizesAndPositions(
        float scale,
        float x,
        float y,
        float z,
        int width,
        int height
    )
    {
        using var model = new TestObjFile(
            FormattableString.Invariant(
                $"""
                v {x - scale} {y - scale} {z}
                v {x + scale} {y - scale} {z}
                v {x} {y + scale} {z}
                f 1 2 3
                """
            )
        );
        var viewModel = new MainWindowViewModel { RenderWidth = width, RenderHeight = height };
        await viewModel.LoadModelAsync(model.Path);

        await viewModel.StartRenderCommand.ExecuteAsync(null);

        var result = viewModel.RenderResult;
        Assert.NotNull(result);
        Assert.NotEqual(
            RenderSettings.Default.BackgroundColor,
            result.GetPixel(width / 2, height / 2)
        );
        Assert.Equal(RenderSettings.Default.BackgroundColor, result.GetPixel(0, 0));
        Assert.Equal(
            RenderSettings.Default.BackgroundColor,
            result.GetPixel(width - 1, height - 1)
        );
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

    private static float Brightness(Vector3 color)
    {
        return color.X + color.Y + color.Z;
    }
}
