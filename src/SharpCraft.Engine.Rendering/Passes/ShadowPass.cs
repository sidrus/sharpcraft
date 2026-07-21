using SharpCraft.Engine.Rendering.Shaders;

namespace SharpCraft.Engine.Rendering.Passes;

/// <summary>
/// Renders the cascaded shadow map: one conventional-depth ortho pass per cascade into a depth array.
/// Uses conventional (non-reversed) depth with depth-clamped casters and no face culling, then restores
/// the reversed-Z main-pass policy on exit so later passes see the expected depth state. Always runs.
/// </summary>
public sealed class ShadowPass(GL gl, ChunkRenderCache cache, uint shadowMapSize, int cascadeCount) : IRenderPass
{
    private readonly CascadedShadowMap _csm = new(gl, shadowMapSize, cascadeCount);

    private readonly ShadowMapRenderer _renderer =
        new(gl, cache, new ShaderProgram(gl, Shaders.Shaders.ShadowVertex, Shaders.Shaders.ShadowFragment));

    public string Name => "Shadow";

    public IReadOnlyList<RenderResource> Reads => [];

    public IReadOnlyList<RenderResource> Writes => [RenderResource.ShadowMap];

    public bool Enabled(RenderContext context)
    {
        return true;
    }

    public void Execute(IWorld world, RenderContext context, RenderTargets targets)
    {
        gl.Enable(EnableCap.DepthTest);
        gl.Enable(EnableCap.DepthClamp);
        gl.DepthFunc(DepthFunction.Less);
        gl.ClearDepth(1.0f);
        gl.Disable(EnableCap.CullFace);

        var matrices = targets.CascadeLightMatrices;
        for (int c = 0; c < matrices.Length; c++)
        {
            _csm.BindLayer(c);
            gl.Clear(ClearBufferMask.DepthBufferBit);
            _renderer.Render(world, matrices[c]);
        }
        _csm.Unbind();

        gl.Enable(EnableCap.CullFace);
        gl.CullFace(TriangleFace.Back);

        gl.Disable(EnableCap.DepthClamp);
        gl.DepthFunc(DepthFunction.Greater);
        gl.ClearDepth(0.0f);

        targets.ShadowMap = _csm.DepthArray;
    }

    public void Dispose()
    {
        _csm.Dispose();
        _renderer.Dispose();
    }
}