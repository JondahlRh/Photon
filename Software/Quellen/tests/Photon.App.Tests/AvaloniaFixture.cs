// -----------------------------------------------------------------------------
// Author:        ChatGPT Codex
// AI Assistance: ChatGPT Codex for headless bitmap test setup
// -----------------------------------------------------------------------------

using Avalonia;
using Avalonia.Headless;

namespace Photon.App.Tests;

public sealed class AvaloniaFixture
{
    public AvaloniaFixture()
    {
        AppBuilder
            .Configure<Avalonia.Application>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .SetupWithoutStarting();
    }
}
