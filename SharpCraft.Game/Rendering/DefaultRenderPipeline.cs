using SharpCraft.Core;
using Silk.NET.OpenGL;

namespace SharpCraft.Game.Rendering;

public class DefaultRenderPipeline : IRenderPipeline
{
    private readonly GL _gl;
    private readonly TerrainRenderer _terrainRenderer;
    private readonly WaterRenderer _waterRenderer;
    private readonly ChunkRenderCache _cache;

    private World? _world;
    private RenderContext? _context;

    public DefaultRenderPipeline(GL gl, World world)
    {
        _gl = gl;
        _world = world;
        _cache = new ChunkRenderCache(gl);
        var meshManager = new ChunkMeshManager(world);
        _terrainRenderer = new TerrainRenderer(gl, _cache, meshManager);
        _waterRenderer = new WaterRenderer(gl, _cache, meshManager);
    }

    public void OnRender(double deltaTime)
    {
        if (_world == null || !_context.HasValue) return;
        Execute(_world, _context.Value);
    }

    public void SetContext(World world, RenderContext context)
    {
        _world = world;
        _context = context;
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
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _cache.Dispose();
                _terrainRenderer.Dispose();
                _waterRenderer.Dispose();
            }

            _disposed = true;
        }
    }

    private bool _disposed;
}