// -----------------------------------------------------------------------------
// Author:        BrackyDayn
// AI Assistance: ChatGPT Codex for summary.
// -----------------------------------------------------------------------------

using System.Numerics;

namespace Photon.Core.Mathematics;

/// <summary>
/// simple Triangle class for demo, represents a triangle defined by three vertices in 3D space.
/// </summary>
public readonly struct Triangle
{
    /// <summary>
    /// Initializes a triangle using three vertices.
    /// </summary>
    /// <param name="a">First vertex.</param>
    /// <param name="b">second vertex.</param>
    /// <param name="c">Third vertex.</param>
    public Triangle(Vector3 a, Vector3 b, Vector3 c)
    {
        A = a;
        B = b;
        C = c;
    }

    /// <summary>First vertex.</summary>
    public Vector3 A { get; }

    /// <summary>Second vertex.</summary>
    public Vector3 B { get; }

    /// <summary>Third Vertex.</summary>
    public Vector3 C { get; }
}
