namespace SharpCraft.Engine.Rendering.Passes;

/// <summary>
/// Snapshots the opaque HDR scene into a private color buffer so the transparent water pass can
/// sample it for screen-space reflections (a surface can't read the attachment it draws into). Runs
/// only when SSR is enabled and the scene depth is available.
/// </summary>
public sealed class SsrSnapshotPass(GL gl) : IRenderPass
{
    private Framebuffer? _opaqueColor;

    public string Name => "SsrSnapshot";

    public IReadOnlyList<RenderResource> Reads => [RenderResource.HdrScene];

    public IReadOnlyList<RenderResource> Writes => [RenderResource.OpaqueColor];

    public bool Enabled(RenderContext context)
    {
        return context.Effects.UseSsr;
    }

    public void Execute(IWorld world, RenderContext context, RenderTargets targets)
    {
        if (targets.SceneDepthTexture == 0)
        {
            return;
        }

        var width = context.Camera.ScreenWidth;
        var height = context.Camera.ScreenHeight;
        if (_opaqueColor == null || _opaqueColor.Width != width || _opaqueColor.Height != height)
        {
            _opaqueColor?.Dispose();
            _opaqueColor = new Framebuffer(gl, width, height, hdr: true);
        }

        gl.BlitNamedFramebuffer(targets.HdrSceneFbo, _opaqueColor.Handle,
            0, 0, width, height, 0, 0, width, height,
            ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, targets.HdrSceneFbo);
        gl.Viewport(0, 0, (uint)width, (uint)height);
        targets.OpaqueColorTexture = _opaqueColor.TextureHandle;
    }

    public void Dispose()
    {
        _opaqueColor?.Dispose();
    }
}