namespace SharpCraft.Engine.Rendering.Passes;

/// <summary>
/// Half-res ray-march of height fog + sun shafts against the CSM, composited into the HDR scene
/// (scene·T + inscatter) before the TAA resolve. Disabled when volumetrics are off or zero-intensity.
/// </summary>
public sealed class VolumetricPass(GL gl, PostProcessingRenderer postProcessing, float maxShadowDistance) : IRenderPass
{
    private readonly VolumetricRenderer _renderer = new(gl);

    public string Name => "Volumetrics";

    public IReadOnlyList<RenderResource> Reads => [RenderResource.HdrScene, RenderResource.ShadowMap, RenderResource.InvViewProj];

    public IReadOnlyList<RenderResource> Writes => [RenderResource.HdrScene];

    public bool Enabled(RenderContext context)
    {
        return postProcessing.VolumetricEnabled && postProcessing.VolumetricIntensity > 0f;
    }

    public void Execute(IWorld world, RenderContext context, RenderTargets targets)
    {
        _renderer.Render(
            targets.HdrSceneDepth, targets.ShadowMap, context, targets.InvViewProj,
            postProcessing.DensityMultiplier, postProcessing.ExtinctionMultiplier,
            postProcessing.VolumetricIntensity, postProcessing.VolumetricSamples,
            postProcessing.ScatteringG, maxShadowDistance);

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, targets.HdrSceneFbo);
        _renderer.Composite(context.Camera.ScreenWidth, context.Camera.ScreenHeight,
            targets.HdrSceneDepth, context.Camera.Projection.M43);
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Dispose()
    {
        _renderer.Dispose();
    }
}