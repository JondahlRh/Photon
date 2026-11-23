// -----------------------------------------------------------------------------
// Author:        Luca Lengl
// AI Assistance: Claude for Information, Bug and Warning
// -----------------------------------------------------------------------------

namespace Photon.Core.Scenes;

/// <summary>
/// One corner of a face as passed to <see cref="ModelBuilder.AddFace"/>.
/// </summary>
/// <param name="PositionIndex">The zero-based index of a position added with <see cref="ModelBuilder.AddPosition"/>.</param>
/// <param name="NormalIndex">
/// The zero-based index of a normal added with <see cref="ModelBuilder.AddNormal"/>,
/// or <c>null</c> if the builder should calculate the normal.
/// </param>
public readonly record struct FaceCorner(int PositionIndex, int? NormalIndex = null);
