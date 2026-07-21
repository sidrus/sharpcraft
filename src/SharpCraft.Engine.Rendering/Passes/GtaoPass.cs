namespace SharpCraft.Engine.Rendering.Passes;

/// <summary>
/// Ground-truth ambient occlusion from the scene depth, multiplied into ambient by the forward pass.
/// Runs only when SSAO is enabled; requires the depth pre-pass to have produced scene depth first.
/// </summary>
public sealed class GtaoPass(GL gl) : IRenderPass
{
    private readonly GtaoRenderer _gtao = new(gl);

    public string Name => "Gtao";

    public IReadOnlyList<RenderResource> Reads => [RenderResource.SceneDepth];

    public IReadOnlyList<RenderResource> Writes => [RenderResource.Gtao];

    public bool Enabled(RenderContext context)
    {
        return context.Effects.UseSsao;
    }

    public void Execute(IWorld world, RenderContext context, RenderTargets targets)
    {
        targets.GtaoTexture = _gtao.Render(
            targets.SceneDepthTexture, targets.MainProjection,
            context.Camera.ScreenWidth, context.Camera.ScreenHeight,
            context.Effects.SsaoRadius, context.Effects.SsaoIntensity);
    }

    public void Dispose()
    {
        _gtao.Dispose();
    }
}