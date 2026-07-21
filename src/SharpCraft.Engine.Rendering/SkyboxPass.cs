namespace SharpCraft.Engine.Rendering;

/// <summary>
/// Fills the background sky into the HDR scene before opaque geometry. Draws with depth test off (the
/// sky loses to any geometry via the reversed-Z clear), reading the inverse view-projection to
/// reconstruct per-pixel view rays.
/// </summary>
public sealed class SkyboxPass(GL gl) : IRenderPass
{
    private readonly SkyboxRenderer _renderer = new(gl);

    public string Name => "Skybox";

    public IReadOnlyList<RenderResource> Reads => [RenderResource.InvViewProj];

    public IReadOnlyList<RenderResource> Writes => [RenderResource.HdrScene];

    public bool Enabled(RenderContext context)
    {
        return true;
    }

    public void Execute(IWorld world, RenderContext context, RenderTargets targets)
    {
        gl.Disable(EnableCap.DepthTest);
        _renderer.Render(context, targets);
        gl.Enable(EnableCap.DepthTest);
    }

    public void Dispose()
    {
        _renderer.Dispose();
    }
}
