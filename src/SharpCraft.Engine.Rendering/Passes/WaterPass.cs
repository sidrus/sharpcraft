namespace SharpCraft.Engine.Rendering.Passes;

/// <summary>
/// Transparent water, forward and blended, drawn after the opaque scene. Tests against the opaque
/// depth without writing it (the z-fighting fix) and draws double-sided (the water mesh has no bottom
/// faces). Samples the opaque-colour snapshot for SSR. Sets and restores its own blend/cull/depth state.
/// </summary>
public sealed class WaterPass(GL gl, WaterRenderer water) : IRenderPass
{
    public string Name => "Water";

    public IReadOnlyList<RenderResource> Reads =>
        [RenderResource.OpaqueColor, RenderResource.SceneDepth, RenderResource.ShadowMap];

    public IReadOnlyList<RenderResource> Writes => [RenderResource.HdrScene];

    public bool Enabled(RenderContext context)
    {
        return true;
    }

    public void Execute(IWorld world, RenderContext context, RenderTargets targets)
    {
        gl.Enable(EnableCap.Blend);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        gl.Disable(EnableCap.CullFace);
        gl.DepthMask(false);

        water.Render(world, context, targets);

        gl.DepthMask(true);
        gl.Enable(EnableCap.CullFace);
        gl.Disable(EnableCap.Blend);
    }

    public void Dispose()
    {
        water.Dispose();
    }
}
