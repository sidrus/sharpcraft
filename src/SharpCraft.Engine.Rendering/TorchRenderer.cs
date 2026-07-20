using SharpCraft.Engine.Rendering.Shaders;
using SharpCraft.Engine.Rendering.Textures;
using System.Numerics;

namespace SharpCraft.Engine.Rendering;

/// <summary>
/// Draws placed torches as small textured 3D boxes in the world. Each torch is a thin tapered
/// column whose wooden handle is sun/ambient lit and whose burning head is emissive (so it blooms
/// in the HDR pass). The actual light it casts on the surroundings comes from a separate
/// <see cref="Lighting.PointLightData"/> registered when the torch is placed.
/// </summary>
public sealed class TorchRenderer : IDisposable
{
    private readonly GL _gl;
    private readonly ShaderProgram _shader;
    private readonly Texture2d _texture;
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly int _vertexCount;

    private readonly List<Vector3> _torches = [];
    private readonly Lock _lock = new();

    // Model dimensions, in block units (1.0 == one block edge). A Minecraft-style torch is
    // 2px wide and ~10px tall, resting with its base on the supporting block's top face.
    private const float HalfThickness = 1.0f / 16.0f; // 2px wide overall
    private const float Height = 10.0f / 16.0f;

    public TorchRenderer(GL gl)
    {
        _gl = gl;
        _shader = new ShaderProgram(gl, Shaders.Shaders.TorchVertex, Shaders.Shaders.TorchFragment);
        _shader.BindUniformBlock("SceneData", 0);

        var (width, height, pixels) = BuildTexture();
        _texture = new Texture2d(gl, width, height, pixels);

        var vertices = BuildMesh();
        _vertexCount = vertices.Length / 8; // pos(3) + uv(2) + normal(3)

        _vao = gl.GenVertexArray();
        _vbo = gl.GenBuffer();
        gl.BindVertexArray(_vao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        unsafe
        {
            fixed (float* v = vertices)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), v, BufferUsageARB.StaticDraw);
            }

            const uint stride = 8 * sizeof(float);
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));
            gl.EnableVertexAttribArray(2);
            gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, stride, (void*)(5 * sizeof(float)));
        }
        gl.BindVertexArray(0);
    }

    /// <summary>
    /// Registers a torch whose base rests at <paramref name="basePosition"/> (the centre of the
    /// supporting block's top face).
    /// </summary>
    public void AddTorch(Vector3 basePosition)
    {
        lock (_lock)
        {
            _torches.Add(basePosition);
        }
    }

    public int Count
    {
        get { lock (_lock) { return _torches.Count; } }
    }

    public void Render(RenderContext context)
    {
        Vector3[] snapshot;
        lock (_lock)
        {
            if (_torches.Count == 0) return;
            snapshot = _torches.ToArray();
        }

        var sun = context.Sun;
        var sunDir = sun?.Direction ?? Vector3.Normalize(new Vector3(0.8f, -0.5f, 0.1f));
        var sunColor = sun.HasValue ? sun.Value.Color * sun.Value.Intensity : new Vector3(1.0f);

        // Opaque, double-sided (no bottom face on the model, and the box is tiny — cheaper to skip
        // winding concerns than to risk a culled face). Depth test stays at the reversed-Z default.
        _gl.Disable(EnableCap.CullFace);

        _shader.Use();
        _shader.SetUniform("sunDirection", sunDir);
        _shader.SetUniform("sunColor", sunColor);
        _texture.Bind();
        _shader.SetUniform("torchTex", 0);

        _gl.BindVertexArray(_vao);
        foreach (var pos in snapshot)
        {
            _shader.SetUniform("model", Matrix4x4.CreateTranslation(pos));
            _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)_vertexCount);
        }
        _gl.BindVertexArray(0);

        _gl.Enable(EnableCap.CullFace);
    }

    // === Mesh ===================================================================================

    private static float[] BuildMesh()
    {
        const float h = HalfThickness;
        const float t = Height;

        // The 8 box corners (base on the y=0 plane, centred on x/z).
        var c0 = new Vector3(-h, 0, -h);
        var c1 = new Vector3(h, 0, -h);
        var c2 = new Vector3(h, 0, h);
        var c3 = new Vector3(-h, 0, h);
        var c4 = new Vector3(-h, t, -h);
        var c5 = new Vector3(h, t, -h);
        var c6 = new Vector3(h, t, h);
        var c7 = new Vector3(-h, t, h);

        // Texture regions (16x16). Side faces sample the central 2px strip over the full height;
        // the top face samples the bright flame tip.
        const float su0 = 7.0f / 16.0f;
        const float su1 = 9.0f / 16.0f;
        const float tv0 = 13.0f / 16.0f;
        const float tv1 = 15.0f / 16.0f;

        var verts = new List<float>(6 * 6 * 8);

        // Side faces: (bottom-left, bottom-right, top-right, top-left) with v running 0..1 up the model.
        AddQuad(verts, c1, c2, c6, c5, new Vector3(1, 0, 0), su0, 0f, su1, 1f);  // +X
        AddQuad(verts, c3, c0, c4, c7, new Vector3(-1, 0, 0), su0, 0f, su1, 1f); // -X
        AddQuad(verts, c2, c3, c7, c6, new Vector3(0, 0, 1), su0, 0f, su1, 1f);  // +Z
        AddQuad(verts, c0, c1, c5, c4, new Vector3(0, 0, -1), su0, 0f, su1, 1f); // -Z
        // Top face (the glowing tip).
        AddQuad(verts, c4, c5, c6, c7, new Vector3(0, 1, 0), su0, tv0, su1, tv1);

        return verts.ToArray();
    }

    private static void AddQuad(List<float> verts, Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl,
        Vector3 normal, float u0, float v0, float u1, float v1)
    {
        // Two triangles: bl, br, tr / bl, tr, tl.
        AddVertex(verts, bl, u0, v0, normal);
        AddVertex(verts, br, u1, v0, normal);
        AddVertex(verts, tr, u1, v1, normal);
        AddVertex(verts, bl, u0, v0, normal);
        AddVertex(verts, tr, u1, v1, normal);
        AddVertex(verts, tl, u0, v1, normal);
    }

    private static void AddVertex(List<float> verts, Vector3 pos, float u, float v, Vector3 normal)
    {
        verts.Add(pos.X); verts.Add(pos.Y); verts.Add(pos.Z);
        verts.Add(u); verts.Add(v);
        verts.Add(normal.X); verts.Add(normal.Y); verts.Add(normal.Z);
    }

    // === Procedural texture =====================================================================

    private static (int width, int height, byte[] pixels) BuildTexture()
    {
        const int size = 16;
        var data = new byte[size * size * 4]; // RGBA, fully transparent by default

        // Row 0 is the bottom of the texture (v=0) so the wooden base maps to the model's base.
        // Only the central 2px column (x=7,8) is sampled by the mesh; the rest stays transparent.
        for (var y = 0; y < size; y++)
        {
            (byte r, byte g, byte b) colour;
            if (y <= 8)
            {
                // Wooden handle, with a faint grain alternating per row.
                colour = (y & 1) == 0 ? ((byte)122, (byte)78, (byte)38) : ((byte)92, (byte)58, (byte)28);
            }
            else if (y <= 11)
            {
                colour = (200, 70, 20); // smouldering ember
            }
            else if (y <= 13)
            {
                colour = (255, 140, 30); // flame body
            }
            else
            {
                colour = (255, 220, 90); // bright tip
            }

            for (var x = 7; x <= 8; x++)
            {
                var i = (y * size + x) * 4;
                data[i] = colour.r;
                data[i + 1] = colour.g;
                data[i + 2] = colour.b;
                data[i + 3] = 255;
            }
        }

        return (size, size, data);
    }

    public void Dispose()
    {
        _shader.Dispose();
        _texture.Dispose();
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteVertexArray(_vao);
    }
}