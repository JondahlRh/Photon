// -----------------------------------------------------------------------------
// Author:        MJZgh
// AI Assistance: Claude for logging Documentation
// -----------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Photon.Application.ViewModels;

namespace Photon.Application.Tests.ViewModels;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void StartRenderWorksWithoutLogger()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.StartRenderCommand.Execute(null);

        Assert.Equal("Rendering...", viewModel.StatusText);
    }

    [Fact]
    public void StartRenderLogsRenderStart()
    {
        var logger = new ListLogger<MainWindowViewModel>();
        var viewModel = new MainWindowViewModel(logger);

        viewModel.StartRenderCommand.Execute(null);

        Assert.Contains(logger.Messages, message => message.Contains("Render started"));
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
