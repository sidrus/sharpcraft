using SharpCraft.Engine.Rendering.Shaders;
using System.Numerics;

namespace SharpCraft.Engine.Rendering.Renderers;

/// <summary>
/// Draws placed <see cref="StaticMesh"/> instances as opaque, sun-lit geometry. Meshes are
/// registered once (typically at startup) and then placed at world positions by gameplay code. The
/// renderer owns the shared shader; each mesh owns its own buffers. The pipeline treats this as a
/// generic opaque draw and knows nothing about the meshes' game meaning.
/// </summary>
public sealed class StaticMeshRenderer : IDisposable
{
    private readonly GL _gl;
    private readonly ShaderProgram _shader;
    private readonly List<StaticMesh> _meshes = [];

    public StaticMeshRenderer(GL gl)
    {
        _gl = gl;
        _shader = new ShaderProgram(gl, Shaders.Shaders.StaticMeshVertex, Shaders.Shaders.StaticMeshFragment);
        _shader.BindUniformBlock("SceneData", 0);
    }

    /// <summary>
    /// Registers a mesh (vertex layout pos(3)/uv(2)/normal(3)) with its texture. Call at startup,
    /// before the render loop begins.
    /// </summary>
    public StaticMesh Register(float[] mesh, int textureWidth, int textureHeight, byte[] texturePixels)
    {
        var staticMesh = new StaticMesh(_gl, mesh, textureWidth, textureHeight, texturePixels);
        _meshes.Add(staticMesh);
        return staticMesh;
    }

    public void Render(RenderContext context)
    {
        if (_meshes.Count == 0)
        {
            return;
        }

        var sun = context.Lighting.Sun;
        var sunDirection = sun?.Direction ?? Vector3.Normalize(new Vector3(0.8f, -0.5f, 0.1f));
        var sunColor = sun.HasValue ? sun.Value.Color * sun.Value.Intensity : new Vector3(1.0f);

        // Opaque, double-sided (models are tiny — cheaper to skip winding concerns than risk a
        // culled face). Depth test stays at the reversed-Z default.
        _gl.Disable(EnableCap.CullFace);

        _shader.Use();
        _shader.SetUniform("sunDirection", sunDirection);
        _shader.SetUniform("sunColor", sunColor);
        _shader.SetUniform("baseColorTex", 0);

        foreach (var mesh in _meshes)
        {
            mesh.Draw(_gl, _shader);
        }

        _gl.Enable(EnableCap.CullFace);
    }

    public void Dispose()
    {
        _shader.Dispose();
        foreach (var mesh in _meshes)
        {
            mesh.Dispose();
        }
    }
}
