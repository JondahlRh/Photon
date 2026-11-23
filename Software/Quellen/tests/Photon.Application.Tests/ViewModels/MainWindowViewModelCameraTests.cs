// -----------------------------------------------------------------------------
// Author:        ChatGPT Codex
// AI Assistance: ChatGPT Codex for free camera rotation and render regression tests
// -----------------------------------------------------------------------------

using System.Numerics;
using Photon.Application.Tests.Support;
using Photon.Application.ViewModels;
using Photon.Core.Mathematics;
using Photon.Core.Scenes;

namespace Photon.Application.Tests.ViewModels;

public sealed class MainWindowViewModelCameraTests
{
    [Fact]
    public void OrbitReachesAndPassesThePole()
    {
        var viewModel = new MainWindowViewModel { CameraOrientation = Quaternion.Identity };
        var bounds = new Aabb(-Vector3.One, Vector3.One);

        viewModel.RotateCamera(0f, 90f);
        var atPole = Camera.FrameBounds(bounds, 40, 40, viewModel.CameraOrientation);
        Assert.True(Vector3.Distance(-Vector3.UnitY, Vector3.Normalize(atPole.Position)) < 1e-4f);
        Assert.True(VectorMath.IsFinite(atPole.Up));

        viewModel.RotateCamera(0f, 90f);
        var beyondPole = Camera.FrameBounds(bounds, 40, 40, viewModel.CameraOrientation);
        Assert.True(
            Vector3.Distance(-Vector3.UnitZ, Vector3.Normalize(beyondPole.Position)) < 1e-4f
        );
        Assert.True(Vector3.Distance(-Vector3.UnitY, beyondPole.Up) < 1e-4f);
    }

    [Fact]
    public void RollTurnsTheImageWithoutMovingTheViewpoint()
    {
        var viewModel = new MainWindowViewModel();
        var bounds = new Aabb(-Vector3.One, Vector3.One);
        var before = Camera.FrameBounds(bounds, 40, 40, viewModel.CameraOrientation);

        viewModel.RotateCamera(0f, 0f, 90f);

        var after = Camera.FrameBounds(bounds, 40, 40, viewModel.CameraOrientation);
        Assert.True(Vector3.Distance(before.Position, after.Position) < 1e-4f);
        Assert.True(MathF.Abs(Vector3.Dot(before.Up, after.Up)) < 1e-4f);
    }

    [Fact]
    public void ResetRestoresTheInitialOrientation()
    {
        var viewModel = new MainWindowViewModel();
        Quaternion initial = viewModel.CameraOrientation;

        viewModel.RotateCamera(70f, 135f, 90f);
        Assert.True(MathF.Abs(Quaternion.Dot(initial, viewModel.CameraOrientation)) < 0.99f);

        viewModel.ResetCameraCommand.Execute(null);

        Assert.True(MathF.Abs(Quaternion.Dot(initial, viewModel.CameraOrientation)) > 0.99999f);
    }

    [Fact]
    public async Task RenderUsesTheRolledOrientation()
    {
        using var model = new TestObjFile();
        const int size = 41;
        var viewModel = new MainWindowViewModel
        {
            RenderWidth = size,
            RenderHeight = size,
            CameraOrientation = Quaternion.Identity,
        };
        await viewModel.LoadModelAsync(model.Path);
        await viewModel.StartRenderCommand.ExecuteAsync(null);
        var original = viewModel.RenderResult;
        Assert.NotNull(original);

        viewModel.RotateCamera(0f, 0f, 90f);
        await viewModel.StartRenderCommand.ExecuteAsync(null);
        var rolled = viewModel.RenderResult;
        Assert.NotNull(rolled);
        bool silhouetteChanged = false;

        for (int y = 1; y < size - 1; y++)
        {
            for (int x = 1; x < size - 1; x++)
            {
                Vector3 expected = original.GetPixel(x, y);
                silhouetteChanged |= expected != rolled.GetPixel(x, y);

                // Skip silhouette edges, where roundoff may change which triangle owns a pixel.
                if (
                    expected != original.GetPixel(x - 1, y)
                    || expected != original.GetPixel(x + 1, y)
                    || expected != original.GetPixel(x, y - 1)
                    || expected != original.GetPixel(x, y + 1)
                )
                {
                    continue;
                }

                Assert.True(Vector3.Distance(expected, rolled.GetPixel(size - 1 - y, x)) < 1e-4f);
            }
        }

        Assert.True(silhouetteChanged);
    }
}
