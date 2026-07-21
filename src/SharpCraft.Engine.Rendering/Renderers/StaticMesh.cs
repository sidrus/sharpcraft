using SharpCraft.Engine.Rendering.Shaders;
using SharpCraft.Engine.Rendering.Textures;
using System.Numerics;

namespace SharpCraft.Engine.Rendering.Renderers;

/// <summary>
/// A placeable model: one shared mesh + texture, drawn once per world position it has been placed
/// at. Owns its GPU buffers and texture; the shader is shared and supplied by
/// <see cref="StaticMeshRenderer"/>. The pipeline draws static meshes as opaque geometry and has no
/// knowledge of what any given mesh represents.
/// </summary>
public sealed class StaticMesh : IDisposable
{
    private readonly GL _gl;
    private readonly Texture2D _texture;
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly int _vertexCount;
    private readonly List<Vector3> _positions = [];
    private readonly Lock _lock = new();

    internal StaticMesh(GL gl, float[] mesh, int textureWidth, int textureHeight, byte[] texturePixels)
    {
        _gl = gl;
        _texture = new Texture2D(gl, textureWidth, textureHeight, texturePixels);
        _vertexCount = mesh.Length / 8; // pos(3) + uv(2) + normal(3)

        _vao = gl.GenVertexArray();
        _vbo = gl.GenBuffer();
        gl.BindVertexArray(_vao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        unsafe
        {
            fixed (float* v = mesh)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(mesh.Length * sizeof(float)), v, BufferUsageARB.StaticDraw);
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
    /// Registers an instance of this mesh at <paramref name="position"/> (its model-space origin).
    /// </summary>
    public void Place(Vector3 position)
    {
        lock (_lock)
        {
            _positions.Add(position);
        }
    }

    /// <summary>Gets the number of placed instances.</summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _positions.Count;
            }
        }
    }

    internal void Draw(GL gl, ShaderProgram shader)
    {
        Vector3[] snapshot;
        lock (_lock)
        {
            if (_positions.Count == 0)
            {
                return;
            }

            snapshot = _positions.ToArray();
        }

        _texture.Bind();
        gl.BindVertexArray(_vao);
        foreach (var pos in snapshot)
        {
            shader.SetUniform("model", Matrix4x4.CreateTranslation(pos));
            gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)_vertexCount);
        }
        gl.BindVertexArray(0);
    }

    public void Dispose()
    {
        _texture.Dispose();
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteVertexArray(_vao);
    }
}
