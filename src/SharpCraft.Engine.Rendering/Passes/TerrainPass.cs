namespace SharpCraft.Engine.Rendering.Passes;

/// <summary>
/// Opaque forward-lit geometry: the terrain chunks plus any placed static meshes. The static meshes
/// are drawn with the opaque geometry and carry no special meaning here — anything that emits light
/// does so through the clustered point lights, not this pass. Shades against the shadow map, GTAO,
/// and IBL produced by earlier passes.
/// </summary>
public sealed class TerrainPass(TerrainRenderer terrain, StaticMeshRenderer staticMeshes) : IRenderPass
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
        staticMeshes.Render(context);
    }

    public void Dispose()
    {
        terrain.Dispose();
        staticMeshes.Dispose();
    }
}