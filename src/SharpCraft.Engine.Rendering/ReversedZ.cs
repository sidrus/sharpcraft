using System.Numerics;

namespace SharpCraft.Engine.Rendering;

/// <summary>
/// Reversed-Z projection helpers (research §1, §12.2).
///
/// We pair <c>glClipControl(GL_LOWER_LEFT, GL_ZERO_TO_ONE)</c> with a projection that maps the
/// near plane to depth 1.0 and the far plane to 0.0. Combined with a floating-point depth buffer
/// this gives near-uniform precision across the whole frustum, which is what keeps distant voxel
/// geometry free of z-fighting at long view distances. Requires a <c>GL_GREATER</c> depth test and
/// a depth clear of 0.0.
///
/// System.Numerics matrices use the row-vector convention (clip = v * M), so these are the
/// transpose of the column-vector forms found in most references.
/// </summary>
public static class ReversedZ
{
    /// <summary>
    /// Reversed-Z perspective with an infinite far plane. Maps the near plane to depth 1.0 and
    /// z → -∞ to depth 0.0 in a [0,1] NDC range (so it must be used with
    /// <c>ClipControlDepth.ZeroToOne</c>). The infinite far plane removes far-plane z-fighting
    /// entirely — ideal for a voxel world with a long view distance.
    /// </summary>
    /// <param name="fovY">Vertical field of view, in radians.</param>
    /// <param name="aspect">Width / height aspect ratio.</param>
    /// <param name="near">Near plane distance (must be &gt; 0).</param>
    public static Matrix4x4 InfinitePerspective(float fovY, float aspect, float near)
    {
        var f = 1.0f / MathF.Tan(fovY * 0.5f);

        // Column-vector form (clip = P * v):
        //   [ f/aspect 0   0   0 ]
        //   [ 0        f   0   0 ]
        //   [ 0        0   0   n ]   → clipZ = near * w
        //   [ 0        0  -1   0 ]   → clipW = -z
        // depth = clipZ / clipW = near / (-z): near → 1, -∞ → 0.
        // The fields below are the row-vector transpose used by System.Numerics.
        return new Matrix4x4(
            f / aspect, 0.0f, 0.0f, 0.0f,
            0.0f, f, 0.0f, 0.0f,
            0.0f, 0.0f, 0.0f, -1.0f,
            0.0f, 0.0f, near, 0.0f);
    }
}