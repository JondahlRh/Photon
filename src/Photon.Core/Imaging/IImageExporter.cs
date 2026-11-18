// -----------------------------------------------------------------------------
// Author:        Florian
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------

using System.Threading.Tasks;

namespace Photon.Core.Imaging;

/// <summary>
/// Saves a rendered image to a file.
/// </summary>
public interface IImageExporter
{
    /// <summary>
    /// Saves the frame buffer as PNG file.
    /// </summary>
    /// <param name="buffer">The rendered image.</param>
    /// <param name="path">The target file path.</param>
    /// <returns>A task that finishes when the file is written.</returns>
    Task ExportPngAsync(FrameBuffer buffer, string path);
}