namespace SharpCraft.Engine.Rendering.Passes;

/// <summary>
/// Builds the dual-filter bloom pyramid from the resolved HDR scene; the output transform composites
/// it. Disabled when bloom is off or its intensity is zero.
/// </summary>
public sealed class BloomPass(GL gl, PostProcessingRenderer postProcessing) : IRenderPass
{
    private readonly BloomRenderer _bloom = new(gl);

    public string Name => "Bloom";

    public IReadOnlyList<RenderResource> Reads => [RenderResource.ResolvedScene];

    public IReadOnlyList<RenderResource> Writes => [RenderResource.BloomTexture];

    public bool Enabled(RenderContext context)
    {
        return context.Effects.UseBloom && postProcessing.BloomIntensity > 0f;
    }

    public void Execute(IWorld world, RenderContext context, RenderTargets targets)
    {
        targets.BloomTexture = _bloom.Render(
            targets.ResolvedScene, context.Camera.ScreenWidth, context.Camera.ScreenHeight, postProcessing.BloomThreshold);
    }

    public void Dispose()
    {
        _bloom.Dispose();
    }
}
