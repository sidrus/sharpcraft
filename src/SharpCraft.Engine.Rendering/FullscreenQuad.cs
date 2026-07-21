namespace SharpCraft.Engine.Rendering;

/// <summary>
/// A screen-covering two-triangle quad with position (NDC) + UV attributes, shared by the
/// post-process passes so the VAO/VBO setup and the vertex layout live in exactly one place.
/// Attribute 0 is vec2 position, attribute 1 is vec2 UV.
/// </summary>
public sealed class FullscreenQuad : IDisposable
{
    private static readonly float[] Vertices =
    {
        -1f,  1f, 0f, 1f,  -1f, -1f, 0f, 0f,   1f, -1f, 1f, 0f,
        -1f,  1f, 0f, 1f,   1f, -1f, 1f, 0f,   1f,  1f, 1f, 1f
    };

    private readonly GL _gl;
    private readonly uint _vao;
    private readonly uint _vbo;
    private bool _disposed;

    public FullscreenQuad(GL gl)
    {
        _gl = gl;
        _vao = gl.GenVertexArray();
        _vbo = gl.GenBuffer();
        gl.BindVertexArray(_vao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        unsafe
        {
            fixed (float* p = Vertices)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(Vertices.Length * sizeof(float)), p, BufferUsageARB.StaticDraw);
            }

            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)0);
            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));
        }
    }

    /// <summary>Binds the quad and draws its two triangles.</summary>
    public void Draw()
    {
        _gl.BindVertexArray(_vao);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _gl.DeleteVertexArray(_vao);
        _gl.DeleteBuffer(_vbo);
        _disposed = true;
    }
}