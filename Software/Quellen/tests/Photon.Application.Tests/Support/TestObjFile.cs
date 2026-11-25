// -----------------------------------------------------------------------------
// Author:        ChatGPT Codex
// AI Assistance: ChatGPT Codex for test fixture implementation
// -----------------------------------------------------------------------------

namespace Photon.Application.Tests.Support;

internal sealed class TestObjFile : IDisposable
{
    public TestObjFile(string? geometry = null)
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"photon-test-{Guid.NewGuid():N}.obj"
        );

        File.WriteAllText(
            Path,
            geometry
                ?? """
                v -3 -3 0
                v 3 -3 0
                v 0 3 0
                f 1 2 3
                """
        );
    }

    public string Path { get; }

    public void Dispose() => File.Delete(Path);
}
