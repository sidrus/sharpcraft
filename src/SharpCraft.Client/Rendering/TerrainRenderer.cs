using System.Numerics;
using SharpCraft.Client.Rendering.Shaders;
using SharpCraft.Client.Rendering.Textures;
using SharpCraft.Core;
using Silk.NET.OpenGL;

namespace SharpCraft.Client.Rendering;

public class TerrainRenderer(GL gl, ChunkRenderCache cache, ChunkMeshManager meshManager) : IRenderer
{
    private readonly ShaderProgram _shader = new(gl, Shaders.Shaders.DefaultVertex, Shaders.Shaders.DefaultFragment);
    private readonly Texture2d _texture = new ColorTexture2d(gl, "Assets/Textures/terrain.png");
    private readonly Texture2d _normalMap = new LinearTexture2d(gl, "Assets/Textures/normals.png");
    private readonly Texture2d _aoMap = new LinearTexture2d(gl, "Assets/Textures/ao.png");
    private readonly Texture2d _specularMap = new LinearTexture2d(gl, "Assets/Textures/specular.png");
    private readonly Frustum _frustum = new();
    private readonly uint _vao = gl.GenVertexArray();

    public void Render(World world, RenderContext context)
    {
        meshManager.Process();
        while (meshManager.TryGetCompleted(out var completedChunk))
        {
            if (completedChunk != null)
            {
                var rc = cache.Get(completedChunk);
                rc.UpdateBuffers();
            }
        }

        _shader.Use();
        _texture.Bind();
        _normalMap.Bind(TextureUnit.Texture1);
        _aoMap.Bind(TextureUnit.Texture2);
        _specularMap.Bind(TextureUnit.Texture3);

        _shader.SetUniform("exposure", context.Exposure);
        _shader.SetUniform("gamma", context.Gamma);
        _shader.SetUniform("textureAtlas", 0);

        _shader.SetUniform("normalMap", 1);
        _shader.SetUniform("useNormalMap", context.UseNormalMap ? 1 : 0);
        _shader.SetUniform("normalStrength", context.NormalStrength);

        _shader.SetUniform("aoMap", 2);
        _shader.SetUniform("useAO", context.UseAoMap ? 1 : 0);
        _shader.SetUniform("aoMapStrength", context.AoMapStrength);

        _shader.SetUniform("specularMap", 3);
        _shader.SetUniform("useSpecular", context.UseSpecularMap ? 1 : 0);
        _shader.SetUniform("specularMapStrength", context.SpecularMapStrength);

        // Directional Light (Sun)
        _shader.SetUniform("dirLight.direction", new Vector3(0.8f, -0.5f, 0.1f));
        _shader.SetUniform("dirLight.color", new Vector3(1.0f, 0.95f, 0.8f));

        if (context.PointLights is not null)
        {
            for (var i = 0; i < context.PointLights.Length; i++)
            {
                var light = context.PointLights[i];
                _shader.SetUniform($"pointLights[{i}].position", light.Position);
                _shader.SetUniform($"pointLights[{i}].color", light.Color);
                _shader.SetUniform($"pointLights[{i}].intensity", light.Intensity);
                _shader.SetUniform($"pointLights[{i}].constant", light.Constant);
                _shader.SetUniform($"pointLights[{i}].linear", light.Linear);
                _shader.SetUniform($"pointLights[{i}].quadratic", light.Quadratic);
            }
        }

        gl.BindVertexArray(_vao);

        _shader.SetUniform("viewPos", context.CameraPosition);
        _shader.SetUniform("fogColor", context.FogColor);
        _shader.SetUniform("fogNear", context.FogNear);
        _shader.SetUniform("fogFar", context.FogFar);
        _shader.SetUniform("lightDir", new Vector3(0.8f, 0.2f, 0.1f));

        _frustum.Update(context.ViewProjection);

        foreach (var chunk in world.GetLoadedChunks())
        {
            var chunkPos = chunk.WorldPosition;
            if (!_frustum.IsBoxInFrustum(chunkPos, chunkPos + new Vector3(16, 256, 16)))
                continue;

            var renderChunk = cache.Get(chunk);
            if (chunk.IsDirty) { meshManager.Enqueue(chunk); }

            var model = Matrix4x4.CreateTranslation(chunkPos);
            _shader.SetUniform("model", model);
            _shader.SetUniform("mvp", model * context.ViewProjection);
            renderChunk.BindAndDrawOpaque();
        }
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
                _shader.Dispose();
                _texture.Dispose();
                _normalMap.Dispose();
                _aoMap.Dispose();
                _specularMap.Dispose();
            }

            gl.DeleteVertexArray(_vao);
            _disposed = true;
        }
    }

    ~TerrainRenderer()
    {
        Dispose(false);
    }

    private bool _disposed;
}