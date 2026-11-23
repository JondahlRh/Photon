// -----------------------------------------------------------------------------
// Author:        MJZgh
// AI Assistance: Claude for code documentation and feedback
// -----------------------------------------------------------------------------

using Photon.Core.Scenes;

namespace Photon.Core.InputOutput;

/// <summary>
/// The result of importing an OBJ file: the model and the warnings for recoverable problems.
/// </summary>
/// <param name="Model">The imported model.</param>
/// <param name="Warnings">Messages for unsupported statements, missing libraries and undefined materials.</param>
public sealed record ImportResult(Model Model, IReadOnlyList<string> Warnings);
