// -----------------------------------------------------------------------------
// Author:        MJZgh
// AI Assistance: Claude for Test Setup; ChatGPT Codex for real render workflow tests
// -----------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Photon.Application.Tests.Support;
using Photon.Application.ViewModels;

namespace Photon.Application.Tests.ViewModels;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task StartRenderWorksWithoutLogger()
    {
        using var model = new TestObjFile();
        var viewModel = new MainWindowViewModel { RenderWidth = 16, RenderHeight = 16 };
        await viewModel.LoadModelAsync(model.Path);

        await viewModel.StartRenderCommand.ExecuteAsync(null);

        Assert.Equal("Render finished.", viewModel.StatusText);
        Assert.NotNull(viewModel.RenderResult);
        Assert.True(viewModel.RenderResult.IsComplete);
        Assert.True(viewModel.HasRenderResult);
    }

    [Fact]
    public async Task StartRenderLogsRenderStart()
    {
        using var model = new TestObjFile();
        var logger = new ListLogger<MainWindowViewModel>();
        var viewModel = new MainWindowViewModel(logger) { RenderWidth = 16, RenderHeight = 16 };
        await viewModel.LoadModelAsync(model.Path);

        await viewModel.StartRenderCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasRenderResult);
        Assert.Contains(logger.Messages, message => message.Contains("Render started"));
    }

    [Fact]
    public async Task InvalidResolutionLogsRenderFailure()
    {
        using var model = new TestObjFile();
        var logger = new ListLogger<MainWindowViewModel>();
        var viewModel = new MainWindowViewModel(logger) { RenderWidth = 0, RenderHeight = 16 };
        await viewModel.LoadModelAsync(model.Path);

        await viewModel.StartRenderCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasRenderResult);
        Assert.Contains(logger.Messages, message => message.Contains("Rendering failed."));
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        ) => Messages.Add(formatter(state, exception));
    }
}
