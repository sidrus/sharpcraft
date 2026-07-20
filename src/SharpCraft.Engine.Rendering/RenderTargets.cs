using System.Numerics;

namespace SharpCraft.Engine.Rendering;

/// <summary>
/// GPU handles produced during a frame and consumed by later passes. Owned by the pipeline and
/// mutated as passes run — never constructed by callers, unlike <see cref="RenderContext"/>.
/// </summary>
public sealed class RenderTargets
{
    /// <summary>Cascaded shadow depth array.</summary>
    public uint ShadowMap;

    /// <summary>IBL diffuse irradiance cubemap (0 when the bake is not ready).</summary>
    public uint IrradianceMap;

    /// <summary>IBL specular prefilter cubemap.</summary>
    public uint PrefilterMap;

    /// <summary>IBL BRDF integration LUT.</summary>
    public uint BrdfLut;

    /// <summary>Opaque HDR scene colour, snapshotted for SSR.</summary>
    public uint OpaqueColorTexture;

    /// <summary>Opaque scene depth from the pre-pass (GTAO / SSR / contact shadows).</summary>
    public uint SceneDepthTexture;

    /// <summary>Screen-space AO from GTAO.</summary>
    public uint GtaoTexture;

    /// <summary>Inverse of the main (jittered) view-projection, for depth reconstruction.</summary>
    public Matrix4x4 InvViewProj;

    /// <summary>
    /// Clears every produced handle back to its empty state at the start of a frame.
    /// </summary>
    public void Reset()
    {
        ShadowMap = 0;
        IrradianceMap = 0;
        PrefilterMap = 0;
        BrdfLut = 0;
        OpaqueColorTexture = 0;
        SceneDepthTexture = 0;
        GtaoTexture = 0;
        InvViewProj = default;
    }
}
