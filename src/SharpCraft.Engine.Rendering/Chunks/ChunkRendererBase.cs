using SharpCraft.Engine.Rendering.Shaders;
using SharpCraft.Engine.Rendering.Textures;
using System.Numerics;

namespace SharpCraft.Engine.Rendering.Chunks;

/// <summary>
/// Shared behavior for the chunk renderers (opaque terrain, transparent water): the frustum-culled
/// per-chunk draw loop and the image-based-lighting texture binding, which the renderers would
/// otherwise duplicate. Subclasses set up their own material uniforms and choose the per-chunk draw.
/// </summary>
public abstract class ChunkRendererBase : IDisposable
{
    private static readonly Vector3 ChunkExtent = new(16, 256, 16);

    protected GL Gl { get; }
    protected ChunkRenderCache Cache { get; }
    protected ChunkMeshManager MeshManager { get; }
    protected TextureAtlas Atlas { get; }
    protected ShaderProgram Shader { get; }

    private readonly Frustum _frustum = new();
    private readonly uint _vao;
    private bool _disposed;

    protected ChunkRendererBase(GL gl, ChunkRenderCache cache, ChunkMeshManager meshManager, TextureAtlas atlas, ShaderProgram shader)
    {
        Gl = gl;
        Cache = cache;
        MeshManager = meshManager;
        Atlas = atlas;
        Shader = shader;
        _vao = gl.GenVertexArray();
    }

    /// <summary>
    /// Binds the IBL cubemaps/LUT (units 6/7/8) and sets the <c>useIBL</c> flag. The caller decides
    /// whether IBL is available, since each renderer has its own readiness guard.
    /// </summary>
    protected void BindIbl(bool useIbl, RenderTargets targets)
    {
        Shader.SetUniform("useIBL", useIbl ? 1 : 0);
        if (!useIbl)
        {
            return;
        }

        Gl.ActiveTexture(TextureUnit.Texture6);
        Gl.BindTexture(TextureTarget.TextureCubeMap, targets.IrradianceMap);
        Shader.SetUniform("irradianceMap", 6);

        Gl.ActiveTexture(TextureUnit.Texture7);
        Gl.BindTexture(TextureTarget.TextureCubeMap, targets.PrefilterMap);
        Shader.SetUniform("prefilterMap", 7);

        Gl.ActiveTexture(TextureUnit.Texture8);
        Gl.BindTexture(TextureTarget.Texture2D, targets.BrdfLut);
        Shader.SetUniform("brdfLUT", 8);
    }

    /// <summary>
    /// Draws every frustum-visible loaded chunk, enqueuing dirty chunks for re-meshing and setting the
    /// per-chunk model matrix. The renderer supplies how a chunk's geometry is drawn.
    /// </summary>
    protected void RenderChunks(IWorld world, RenderContext context, Action<RenderableChunk> draw)
    {
        Gl.BindVertexArray(_vao);
        _frustum.Update(context.Camera.ViewProjection);

        foreach (var chunk in world.GetLoadedChunks())
        {
            var chunkPos = chunk.WorldPosition;
            if (!_frustum.IsBoxInFrustum(chunkPos, chunkPos + ChunkExtent))
            {
                continue;
            }

            var renderChunk = Cache.Get(chunk);
            if (chunk.IsDirty)
            {
                MeshManager.Enqueue(chunk);
            }

            Shader.SetUniform("model", Matrix4x4.CreateTranslation(chunkPos));
            draw(renderChunk);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        DisposeShader(disposing);
        Gl.DeleteVertexArray(_vao);
        _disposed = true;
    }

    /// <summary>Disposes the renderer's shader if it owns it; renderers with a shared shader do nothing.</summary>
    protected abstract void DisposeShader(bool disposing);

    ~ChunkRendererBase()
    {
        Dispose(false);
    }
}
