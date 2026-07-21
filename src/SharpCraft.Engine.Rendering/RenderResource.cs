namespace SharpCraft.Engine.Rendering;

/// <summary>
/// Logical identity of a GPU resource exchanged between render passes, mirroring
/// <see cref="RenderTargets"/>. Passes declare their reads/writes against these keys so the pipeline
/// can validate ordering (and, later, a frame graph can generate barriers) without touching raw GL handles.
/// </summary>
public enum RenderResource
{
    ShadowMap,
    SceneDepth,
    Gtao,
    IrradianceMap,
    PrefilterMap,
    BrdfLut,
    OpaqueColor,
    InvViewProj,
    HdrScene,
    ResolvedScene,
    BloomTexture,
    Output,
}
