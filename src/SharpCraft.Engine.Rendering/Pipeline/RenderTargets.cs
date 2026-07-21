using System.Numerics;

namespace SharpCraft.Engine.Rendering.Pipeline;

/// <summary>
/// GPU handles produced during a frame and consumed by later passes. Owned by the pipeline and
/// mutated as passes run — never constructed by callers, unlike <see cref="RenderContext"/>.
/// </summary>
public sealed class RenderTargets
{
    /// <summary>Cascaded shadow depth array.</summary>
    public uint ShadowMap;

    /// <summary>Per-cascade light-space matrices the shadow pass renders each cascade with.</summary>
    public Matrix4x4[] CascadeLightMatrices = [];

    /// <summary>IBL diffuse irradiance cubemap (0 when the bake is not ready).</summary>
    public uint IrradianceMap;

    /// <summary>IBL specular prefilter cubemap.</summary>
    public uint PrefilterMap;

    /// <summary>IBL BRDF integration LUT.</summary>
    public uint BrdfLut;

    /// <summary>Opaque HDR scene color, snapshotted for SSR.</summary>
    public uint OpaqueColorTexture;

    /// <summary>Opaque scene depth from the pre-pass (GTAO / SSR / contact shadows).</summary>
    public uint SceneDepthTexture;

    /// <summary>Screen-space AO from GTAO.</summary>
    public uint GtaoTexture;

    /// <summary>Inverse of the main (jittered) view-projection, for depth reconstruction.</summary>
    public Matrix4x4 InvViewProj;

    /// <summary>Main (TAA-jittered) view-projection used by the forward + depth-prepass passes.</summary>
    public Matrix4x4 MainViewProj;

    /// <summary>Main (TAA-jittered) projection, for screen-space passes that need it alone (GTAO).</summary>
    public Matrix4x4 MainProjection;

    /// <summary>Framebuffer handle of the main HDR scene, for passes that draw/composite into it.</summary>
    public uint HdrSceneFbo;

    /// <summary>Reversed-Z depth texture of the main HDR scene (forward-pass depth).</summary>
    public uint HdrSceneDepth;

    /// <summary>Resolved HDR scene color after the forward pass (and TAA), input to the post chain.</summary>
    public uint ResolvedScene;

    /// <summary>Bloom pyramid result, composited by the output transform (0 when bloom is off).</summary>
    public uint BloomTexture;

    /// <summary>
    /// Clears every produced handle back to its empty state at the start of a frame.
    /// </summary>
    public void Reset()
    {
        ShadowMap = 0;
        CascadeLightMatrices = [];
        IrradianceMap = 0;
        PrefilterMap = 0;
        BrdfLut = 0;
        OpaqueColorTexture = 0;
        SceneDepthTexture = 0;
        GtaoTexture = 0;
        InvViewProj = default;
        MainViewProj = default;
        MainProjection = default;
        HdrSceneFbo = 0;
        HdrSceneDepth = 0;
        ResolvedScene = 0;
        BloomTexture = 0;
    }
}