using SharpCraft.Core;
using Silk.NET.OpenGL;

namespace SharpCraft.Game.Rendering;

public class DefaultRenderPipeline : IRenderPipeline
{
    private readonly GL _gl;
    private readonly TerrainRenderer _terrainRenderer;
    private readonly WaterRenderer _waterRenderer;
    private readonly ChunkRenderCache _cache;

    public DefaultRenderPipeline(GL gl)
    {
        _gl = gl;
        _cache = new ChunkRenderCache(gl);
        _terrainRenderer = new TerrainRenderer(gl, _cache);
        _waterRenderer = new WaterRenderer(gl, _cache);
    }

    public void Execute(World world, RenderContext context)
    {
        // 1. Update the cache once for the entire frame
        var activeChunks = world.GetLoadedChunks().ToArray();
        _cache.Update(activeChunks);

        // 2. Opaque Pass
        _gl.Enable(EnableCap.DepthTest);
        _gl.Enable(EnableCap.CullFace);
        _gl.Disable(EnableCap.Blend);
        _terrainRenderer.Render(world, context);

        // 3. Transparent Pass
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _gl.Disable(EnableCap.CullFace);
        _waterRenderer.Render(world, context);
    }

    public void Dispose()
    {
        _cache.Dispose();
        _terrainRenderer.Dispose();
        _waterRenderer.Dispose();
    }
}