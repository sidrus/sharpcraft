using SharpCraft.Engine.Rendering.Shaders;

namespace SharpCraft.Engine.Rendering.Passes;

/// <summary>
/// Renders opaque terrain depth at screen resolution into a private depth buffer, producing the scene
/// depth that GTAO, SSR, and contact shadows march against. Runs only when one of those effects is on.
/// Owns its own depth-only <see cref="ShadowMapRenderer"/> (position-only shader) and framebuffer.
/// </summary>
public sealed class DepthPrepassPass(GL gl, ChunkRenderCache cache) : IRenderPass
{
    private readonly ShadowMapRenderer _depthRenderer =
        new(gl, cache, new ShaderProgram(gl, Shaders.Shaders.ShadowVertex, Shaders.Shaders.ShadowFragment));

    private Framebuffer? _depth;

    public string Name => "DepthPrepass";

    public IReadOnlyList<RenderResource> Reads => [];

    public IReadOnlyList<RenderResource> Writes => [RenderResource.SceneDepth];

    public bool Enabled(RenderContext context)
    {
        return context.Effects.UseSsao || context.Effects.UseSsr || context.Effects.UseContactShadows;
    }

    public void Execute(IWorld world, RenderContext context, RenderTargets targets)
    {
        var width = context.Camera.ScreenWidth;
        var height = context.Camera.ScreenHeight;
        if (_depth == null || _depth.Width != width || _depth.Height != height)
        {
            _depth?.Dispose();
            _depth = new Framebuffer(gl, width, height, hdr: false);
        }

        _depth.Bind();
        gl.Viewport(0, 0, (uint)width, (uint)height);
        gl.Clear(ClearBufferMask.DepthBufferBit);
        _depthRenderer.Render(world, targets.MainViewProj);
        _depth.Unbind();
        targets.SceneDepthTexture = _depth.DepthTextureHandle;
    }

    public void Dispose()
    {
        _depthRenderer.Dispose();
        _depth?.Dispose();
    }
}