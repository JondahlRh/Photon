// -----------------------------------------------------------------------------
// Author:        BrackyDayn
// AI Assistance: ChatGPT Codex for editable point-light properties and conversion to PointLight
// -----------------------------------------------------------------------------

using System.Numerics;
using CommunityToolkit.Mvvm.ComponentModel;
using Photon.Core.Scenes;

namespace Photon.Application.ViewModels;

/// <summary>
/// Provides editable position and intensity values for a point light.
/// </summary>
public sealed partial class LightViewModel : ObservableObject
{
    /// <summary>The light's world position on the X axis.</summary>
    [ObservableProperty]
    private float _positionX;

    /// <summary>The light's world position on the Y axis.</summary>
    [ObservableProperty]
    private float _positionY;

    /// <summary>The light's world position on the Z axis.</summary>
    [ObservableProperty]
    private float _positionZ;

    /// <summary>The intensity of the white light.</summary>
    [ObservableProperty]
    private float _intensity = 1f;

    /// <summary>
    /// Creates an immutable point light from the current editor values.
    /// </summary>
    /// <returns>The point light used by the scene.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if a position is not finite or the intensity is invalid.
    /// </exception>
    public PointLight ToPointLight()
    {
        return new PointLight(new Vector3(PositionX, PositionY, PositionZ), new Vector3(Intensity));
    }
}
