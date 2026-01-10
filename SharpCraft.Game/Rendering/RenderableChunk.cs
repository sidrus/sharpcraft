using SharpCraft.Core.WorldGeneration;
using Silk.NET.OpenGL;

namespace SharpCraft.Game.Rendering;

public class RenderableChunk(GL gl, Chunk chunk) : IDisposable
{
    private uint _opaqueVbo, _opaqueEbo;
    private int _opaqueIndexCount;

    private uint _transparentVbo, _transparentEbo;
    private int _transparentIndexCount;

    private bool _isInitialized;

    public unsafe void UpdateBuffers()
    {
        if (!_isInitialized) {
            _opaqueVbo = gl.GenBuffer(); _opaqueEbo = gl.GenBuffer();
            _transparentVbo = gl.GenBuffer(); _transparentEbo = gl.GenBuffer();
            _isInitialized = true;
        }

        UpdateBuffer(_opaqueVbo, _opaqueEbo, chunk.OpaqueMesh, out _opaqueIndexCount);
        UpdateBuffer(_transparentVbo, _transparentEbo, chunk.TransparentMesh, out _transparentIndexCount);
    }

    private unsafe void UpdateBuffer(uint vbo, uint ebo, ChunkMesh mesh, out int count)
    {
        count = mesh.Indices.Length;
        if (count == 0) return;

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        fixed (float* v = mesh.Vertices)
            gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(mesh.Vertices.Length * sizeof(float)), v, BufferUsageARB.StaticDraw);

        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
        fixed (uint* i = mesh.Indices)
            gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(mesh.Indices.Length * sizeof(uint)), i, BufferUsageARB.StaticDraw);
    }

    public void BindAndDrawOpaque() => Draw(_opaqueVbo, _opaqueEbo, _opaqueIndexCount);

    public void BindAndDrawTransparent() => Draw(_transparentVbo, _transparentEbo, _transparentIndexCount);

    private unsafe void Draw(uint vbo, uint ebo, int count)
    {
        if (!_isInitialized || count == 0) return;

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);

        // Stride is 8 floats: pos(3), uv(2), normal(3)
        const uint stride = 8 * sizeof(float);

        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
        gl.EnableVertexAttribArray(0);

        gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));
        gl.EnableVertexAttribArray(1);

        gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, stride, (void*)(5 * sizeof(float)));
        gl.EnableVertexAttribArray(2);

        gl.DrawElements(PrimitiveType.Triangles, (uint)count, DrawElementsType.UnsignedInt, (void*)0);
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
            if (_isInitialized)
            {
                gl.DeleteBuffer(_opaqueVbo);
                gl.DeleteBuffer(_opaqueEbo);
                gl.DeleteBuffer(_transparentVbo);
                gl.DeleteBuffer(_transparentEbo);
            }

            _disposed = true;
        }
    }

    private bool _disposed;
}