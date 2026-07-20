using System.Numerics;
using SharpCraft.Engine.Rendering.Shaders;
using SharpCraft.Engine.Rendering.Textures;

namespace SharpCraft.Engine.Rendering;

public sealed class TerrainRenderer : IDisposable
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
        _shader.BindUniformBlock("CsmData", 2);
    }

    public void Render(IWorld world, RenderContext context, RenderTargets targets)
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

        var useIbl = context.UseIBL && targets.IrradianceMap != 0;
        _shader.SetUniform("useIBL", useIbl ? 1 : 0);
        if (useIbl)
        {
            _gl.ActiveTexture(TextureUnit.Texture6);
            _gl.BindTexture(TextureTarget.TextureCubeMap, targets.IrradianceMap);
            _shader.SetUniform("irradianceMap", 6);

            _gl.ActiveTexture(TextureUnit.Texture7);
            _gl.BindTexture(TextureTarget.TextureCubeMap, targets.PrefilterMap);
            _shader.SetUniform("prefilterMap", 7);

            _gl.ActiveTexture(TextureUnit.Texture8);
            _gl.BindTexture(TextureTarget.Texture2D, targets.BrdfLut);
            _shader.SetUniform("brdfLUT", 8);
        }

        _gl.ActiveTexture(TextureUnit.Texture9);
        _gl.BindTexture(TextureTarget.Texture2DArray, targets.ShadowMap);
        _shader.SetUniform("shadowMap", 9);

        // Screen-space AO (research §7): multiplied into ambient in the shader.
        _shader.SetUniform("useGtao", context.UseSSAO && targets.GtaoTexture > 0 ? 1 : 0);
        _gl.ActiveTexture(TextureUnit.Texture10);
        _gl.BindTexture(TextureTarget.Texture2D, targets.GtaoTexture);
        _shader.SetUniform("gtaoTexture", 10);
        _shader.SetUniform("invScreenSize", new Vector2(1.0f / context.ScreenWidth, 1.0f / context.ScreenHeight));

        // Contact shadows (research §7/§8): short screen-space ray toward the sun against the
        // opaque depth, filling the small contact gaps CSM misses.
        var useContact = context.UseContactShadows && targets.SceneDepthTexture > 0;
        _shader.SetUniform("useContactShadows", useContact ? 1 : 0);
        if (useContact)
        {
            _gl.ActiveTexture(TextureUnit.Texture11);
            _gl.BindTexture(TextureTarget.Texture2D, targets.SceneDepthTexture);
            _shader.SetUniform("sceneDepthTex", 11);
            _shader.SetUniform("contactInvViewProj", targets.InvViewProj);
        }

        // Clustered forward+ light culling (research §2). Buffers are bound by the pipeline; here we
        // just hand the shader the grid parameters so it can find each fragment's cluster.
        _shader.SetUniform("clusterGridSize", new Vector3(ClusteredLighting.GridX, ClusteredLighting.GridY, ClusteredLighting.GridZ));
        _shader.SetUniform("clusterScreenSize", new Vector2(context.ScreenWidth, context.ScreenHeight));
        _shader.SetUniform("clusterZNear", ClusteredLighting.ZNear);
        _shader.SetUniform("clusterZFar", ClusteredLighting.ZFar);

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