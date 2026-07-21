using SharpCraft.Engine.Rendering.Shaders;
using SharpCraft.Engine.Rendering.Textures;
using System.Numerics;

namespace SharpCraft.Engine.Rendering.Renderers;

public sealed class TerrainRenderer : ChunkRendererBase
{
    public TerrainRenderer(
        GL gl,
        ChunkRenderCache cache,
        ChunkMeshManager meshManager,
        TextureAtlas atlas,
        ShaderProgram shader)
        : base(gl, cache, meshManager, atlas, shader)
    {
        Shader.BindUniformBlock("SceneData", 0);
        Shader.BindUniformBlock("LightingData", 1);
        Shader.BindUniformBlock("CsmData", 2);
    }

    public void Render(IWorld world, RenderContext context, RenderTargets targets)
    {
        MeshManager.Process();
        while (MeshManager.TryGetCompleted(out var completedChunk))
        {
            if (completedChunk != null)
            {
                var rc = Cache.Get(completedChunk);
                rc.UpdateBuffers();
            }
        }

        Shader.Use();
        Atlas.Bind();

        Shader.SetUniform("textureAtlas", 0);
        Shader.SetUniform("normalMap", 1);
        Shader.SetUniform("useNormalMap", context.Pbr.UseNormalMap ? 1 : 0);
        Shader.SetUniform("normalStrength", context.Pbr.NormalStrength);

        Shader.SetUniform("aoMap", 2);
        Shader.SetUniform("useAO", context.Pbr.UseAoMap ? 1 : 0);
        Shader.SetUniform("aoMapStrength", context.Pbr.AoMapStrength);

        Shader.SetUniform("metallicMap", 4);
        Shader.SetUniform("useMetallic", context.Pbr.UseMetallicMap ? 1 : 0);
        Shader.SetUniform("metallicStrength", context.Pbr.MetallicStrength);

        Shader.SetUniform("roughnessMap", 5);
        Shader.SetUniform("useRoughness", context.Pbr.UseRoughnessMap ? 1 : 0);
        Shader.SetUniform("roughnessStrength", context.Pbr.RoughnessStrength);

        BindIbl(context.Effects.UseIbl && targets.IrradianceMap != 0, targets);

        Gl.ActiveTexture(TextureUnit.Texture9);
        Gl.BindTexture(TextureTarget.Texture2DArray, targets.ShadowMap);
        Shader.SetUniform("shadowMap", 9);

        // Screen-space AO (research §7): multiplied into ambient in the shader.
        Shader.SetUniform("useGtao", context.Effects.UseSsao && targets.GtaoTexture > 0 ? 1 : 0);
        Gl.ActiveTexture(TextureUnit.Texture10);
        Gl.BindTexture(TextureTarget.Texture2D, targets.GtaoTexture);
        Shader.SetUniform("gtaoTexture", 10);
        Shader.SetUniform("invScreenSize", new Vector2(1.0f / context.Camera.ScreenWidth, 1.0f / context.Camera.ScreenHeight));

        // Contact shadows (research §7/§8): short screen-space ray toward the sun against the
        // opaque depth, filling the small contact gaps CSM misses.
        var useContact = context.Effects.UseContactShadows && targets.SceneDepthTexture > 0;
        Shader.SetUniform("useContactShadows", useContact ? 1 : 0);
        if (useContact)
        {
            Gl.ActiveTexture(TextureUnit.Texture11);
            Gl.BindTexture(TextureTarget.Texture2D, targets.SceneDepthTexture);
            Shader.SetUniform("sceneDepthTex", 11);
            Shader.SetUniform("contactInvViewProj", targets.InvViewProj);
        }

        // Clustered forward+ light culling (research §2). Buffers are bound by the pipeline; here we
        // just hand the shader the grid parameters so it can find each fragment's cluster.
        Shader.SetUniform("clusterGridSize", new Vector3(ClusteredLighting.GridX, ClusteredLighting.GridY, ClusteredLighting.GridZ));
        Shader.SetUniform("clusterScreenSize", new Vector2(context.Camera.ScreenWidth, context.Camera.ScreenHeight));
        Shader.SetUniform("clusterZNear", ClusteredLighting.ZNear);
        Shader.SetUniform("clusterZFar", ClusteredLighting.ZFar);

        RenderChunks(world, context, renderChunk => renderChunk.BindAndDrawOpaque());
    }

    protected override void DisposeShader(bool disposing)
    {
    }
}
