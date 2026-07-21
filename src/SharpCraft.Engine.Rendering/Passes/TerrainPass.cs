namespace SharpCraft.Engine.Rendering.Passes;

/// <summary>
/// Opaque forward-lit geometry: the terrain chunks plus the placed torch models (torches are just a
/// small emissive box drawn with the opaque geometry — their light comes from the clustered point
/// lights, not this pass). Shades against the shadow map, GTAO, and IBL produced by earlier passes.
/// </summary>
public sealed class TerrainPass(TerrainRenderer terrain, TorchRenderer torches) : IRenderPass
{
    public string Name => "Terrain";

    public IReadOnlyList<RenderResource> Reads =>
        [RenderResource.ShadowMap, RenderResource.SceneDepth, RenderResource.Gtao,
         RenderResource.IrradianceMap, RenderResource.PrefilterMap, RenderResource.BrdfLut];

    public IReadOnlyList<RenderResource> Writes => [RenderResource.HdrScene];

    public bool Enabled(RenderContext context)
    {
        return true;
    }

    public void Execute(IWorld world, RenderContext context, RenderTargets targets)
    {
        terrain.Render(world, context, targets);
        torches.Render(context);
    }

    public void Dispose()
    {
        terrain.Dispose();
        torches.Dispose();
    }
}
