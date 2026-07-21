using SharpCraft.Engine.Rendering.Shaders;
using SharpCraft.Engine.Rendering.Textures;
using System.Numerics;

namespace SharpCraft.Engine.Rendering.Renderers;

public sealed class WaterRenderer : ChunkRendererBase
{
    public WaterRenderer(GL gl, ChunkRenderCache cache, ChunkMeshManager meshManager, TextureAtlas atlas)
        : base(gl, cache, meshManager, atlas, new ShaderProgram(gl, Shaders.Shaders.WaterVertex, Shaders.Shaders.WaterFragment))
    {
        Shader.BindUniformBlock("SceneData", 0);
        Shader.BindUniformBlock("LightingData", 1);
    }

    public void Render(IWorld world, RenderContext context, RenderTargets targets)
    {
        Shader.Use();
        Atlas.Bind();

        Shader.SetUniform("textureAtlas", 0);
        Shader.SetUniform("normalMap", 1);
        Shader.SetUniform("useNormalMap", context.Pbr.UseNormalMap ? 1 : 0);
        Shader.SetUniform("normalStrength", context.Pbr.NormalStrength);
        Shader.SetUniform("time", context.Time);

        // Shadow map (cascaded depth array; water samples cascade 0).
        if (targets.ShadowMap > 0)
        {
            Gl.ActiveTexture(TextureUnit.Texture3);
            Gl.BindTexture(TextureTarget.Texture2DArray, targets.ShadowMap);
            Shader.SetUniform("shadowMap", 3);
        }

        // Only enable IBL when all maps are actually available — sampling an unbound
        // cubemap returns black, which would kill the sky reflection entirely.
        var useIbl = context.Effects.UseIbl && targets.IrradianceMap != 0 && targets.PrefilterMap != 0 && targets.BrdfLut != 0;
        BindIbl(useIbl, targets);

        // Screen-space reflections (research §7): ray-march the opaque scene snapshot.
        var useSsr = context.Effects.UseSsr && targets.OpaqueColorTexture != 0 && targets.SceneDepthTexture != 0;
        Shader.SetUniform("useSSR", useSsr ? 1 : 0);
        if (useSsr)
        {
            Gl.ActiveTexture(TextureUnit.Texture9);
            Gl.BindTexture(TextureTarget.Texture2D, targets.OpaqueColorTexture);
            Shader.SetUniform("sceneColorTex", 9);
            Gl.ActiveTexture(TextureUnit.Texture10);
            Gl.BindTexture(TextureTarget.Texture2D, targets.SceneDepthTexture);
            Shader.SetUniform("sceneDepthTex", 10);
            Shader.SetUniform("ssrInvViewProj", targets.InvViewProj);
            Shader.SetUniform("invScreenSize", new Vector2(1.0f / context.Camera.ScreenWidth, 1.0f / context.Camera.ScreenHeight));
        }

        Shader.SetUniform("clusterGridSize", new Vector3(ClusteredLighting.GridX, ClusteredLighting.GridY, ClusteredLighting.GridZ));
        Shader.SetUniform("clusterScreenSize", new Vector2(context.Camera.ScreenWidth, context.Camera.ScreenHeight));
        Shader.SetUniform("clusterZNear", ClusteredLighting.ZNear);
        Shader.SetUniform("clusterZFar", ClusteredLighting.ZFar);

        RenderChunks(world, context, renderChunk => renderChunk.BindAndDrawTransparent());
    }

    protected override void DisposeShader(bool disposing)
    {
        if (disposing)
        {
            Shader.Dispose();
        }
    }
}