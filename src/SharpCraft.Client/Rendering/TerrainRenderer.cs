using System.Numerics;
using SharpCraft.Client.Rendering.Shaders;
using SharpCraft.Client.Rendering.Textures;
using SharpCraft.Engine.Universe;
using SharpCraft.Sdk.Blocks;
using Silk.NET.OpenGL;

namespace SharpCraft.Client.Rendering;

public sealed class TerrainRenderer : IRenderer
{
    private readonly GL _gl;
    private readonly ChunkRenderCache _cache;
    private readonly ChunkMeshManager _meshManager;
    private readonly TextureAtlas _atlas;
    private readonly IBlockRegistry _blocks;
    private readonly ShaderProgram _shader;
    private readonly Frustum _frustum = new();
    private readonly uint _vao;

    public TerrainRenderer(
        GL gl,
        ChunkRenderCache cache,
        ChunkMeshManager meshManager,
        TextureAtlas atlas,
        IBlockRegistry blocks,
        ShaderProgram shader)
    {
        _gl = gl;
        _cache = cache;
        _meshManager = meshManager;
        _atlas = atlas;
        _blocks = blocks;
        _vao = gl.GenVertexArray();
        _shader = shader;

        _shader.BindUniformBlock("SceneData", 0);
        _shader.BindUniformBlock("LightingData", 1);
    }

    public void Render(World world, RenderContext context)
    {
        _meshManager.Process();
        while (_meshManager.TryGetCompleted(out var completedChunk))
        {
            if (completedChunk != null)
            {
                var rc = _cache.Get(completedChunk);
                rc.UpdateBuffers();
            }
        }

        _shader.Use();
        _atlas.Bind(
            TextureUnit.Texture0, 
            TextureUnit.Texture1, 
            TextureUnit.Texture2, 
            TextureUnit.Texture3,
            TextureUnit.Texture4,
            TextureUnit.Texture5);

        _shader.SetUniform("textureAtlas", 0);
        _shader.SetUniform("normalMap", 1);
        _shader.SetUniform("useNormalMap", context.UseNormalMap ? 1 : 0);
        _shader.SetUniform("normalStrength", context.NormalStrength);

        _shader.SetUniform("aoMap", 2);
        _shader.SetUniform("useAO", context.UseAoMap ? 1 : 0);
        _shader.SetUniform("aoMapStrength", context.AoMapStrength);

        _shader.SetUniform("metallicMap", 4);
        _shader.SetUniform("useMetallic", context.UseMetallicMap ? 1 : 0);
        _shader.SetUniform("metallicStrength", context.MetallicStrength);

        _shader.SetUniform("roughnessMap", 5);
        _shader.SetUniform("useRoughness", context.UseRoughnessMap ? 1 : 0);
        _shader.SetUniform("roughnessStrength", context.RoughnessStrength);

        _gl.BindVertexArray(_vao);

        _frustum.Update(context.ViewProjection);

        foreach (var chunk in world.GetLoadedChunks())
        {
            var chunkPos = chunk.WorldPosition;
            if (!_frustum.IsBoxInFrustum(chunkPos, chunkPos + new Vector3(16, 256, 16)))
                continue;

            var renderChunk = _cache.Get(chunk);
            if (chunk.IsDirty) { _meshManager.Enqueue(chunk); }

            var model = Matrix4x4.CreateTranslation(chunkPos);
            _shader.SetUniform("model", model);
            renderChunk.BindAndDrawOpaque();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Shader is shared and managed elsewhere
            }

            _gl.DeleteVertexArray(_vao);
            _disposed = true;
        }
    }

    ~TerrainRenderer()
    {
        Dispose(false);
    }

    private bool _disposed;
}