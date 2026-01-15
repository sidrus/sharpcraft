using SharpCraft.Engine.Universe;
using Silk.NET.OpenGL;

namespace SharpCraft.Client.Rendering;

public class DefaultRenderPipeline(
    GL gl,
    World world,
    ChunkRenderCache cache,
    ChunkMeshManager meshManager,
    TerrainRenderer terrainRenderer,
    WaterRenderer waterRenderer)
    : IRenderPipeline
{
    public ChunkMeshManager MeshManager { get; } = meshManager;

    private World? _world = world;
    private RenderContext? _context;

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
        // Update the cache for the entire frame
        var activeChunks = world.GetLoadedChunks();
        cache.Update(activeChunks);

        // Opaque Pass
        gl.Enable(EnableCap.DepthTest);
        gl.Enable(EnableCap.CullFace);
        gl.Disable(EnableCap.Blend);
        terrainRenderer.Render(world, context);

        // Transparent Pass
        gl.Enable(EnableCap.Blend);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        gl.Disable(EnableCap.CullFace);
        waterRenderer.Render(world, context);
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
                cache.Dispose();
                terrainRenderer.Dispose();
                waterRenderer.Dispose();
            }

            _disposed = true;
        }
    }

    private bool _disposed;
}