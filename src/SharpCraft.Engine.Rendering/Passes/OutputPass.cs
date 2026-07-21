namespace SharpCraft.Engine.Rendering.Passes;

/// <summary>
/// Final HDR fp16 → SDR sRGB output transform: tone-map + gamma the resolved scene to the backbuffer,
/// compositing bloom and (when TAA is off) FXAA. Owns no GPU resources — wraps the shared
/// <see cref="PostProcessingRenderer"/>, whose lifetime the pipeline manages.
/// </summary>
public sealed class OutputPass(PostProcessingRenderer postProcessing) : IRenderPass
{
    public string Name => "Output";

    public IReadOnlyList<RenderResource> Reads => [RenderResource.ResolvedScene, RenderResource.BloomTexture];

    public IReadOnlyList<RenderResource> Writes => [RenderResource.Output];

    public bool Enabled(RenderContext context)
    {
        return true;
    }

    public void Execute(IWorld world, RenderContext context, RenderTargets targets)
    {
        postProcessing.Gamma = context.Exposure.Gamma;
        var bloomStrength = context.Effects.UseBloom ? postProcessing.BloomIntensity : 0f;
        postProcessing.Render(
            targets.ResolvedScene,
            context.Camera.ScreenWidth,
            context.Camera.ScreenHeight,
            context.IsUnderwater,
            context.Time,
            context.Exposure.Exposure,
            useFxaa: !context.Effects.UseTaa,
            bloomTexture: targets.BloomTexture,
            bloomStrength: bloomStrength);
    }

    public void Dispose()
    {
    }
}
