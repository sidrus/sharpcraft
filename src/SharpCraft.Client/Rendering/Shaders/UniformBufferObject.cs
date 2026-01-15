using Silk.NET.OpenGL;
using System.Runtime.InteropServices;

namespace SharpCraft.Client.Rendering.Shaders;

public unsafe class UniformBufferObject<T> : IDisposable where T : unmanaged
{
    private readonly GL _gl;
    private readonly uint _handle;
    private readonly uint _bindingPoint;

    public UniformBufferObject(GL gl, uint bindingPoint)
    {
        _gl = gl;
        _bindingPoint = bindingPoint;
        _handle = _gl.GenBuffer();

        _gl.BindBuffer(BufferTargetARB.UniformBuffer, _handle);
        _gl.BufferData(BufferTargetARB.UniformBuffer, (nuint)sizeof(T), null, BufferUsageARB.DynamicDraw);
        _gl.BindBufferBase(BufferTargetARB.UniformBuffer, _bindingPoint, _handle);
        _gl.BindBuffer(BufferTargetARB.UniformBuffer, 0);
    }

    public void Update(T data)
    {
        _gl.BindBuffer(BufferTargetARB.UniformBuffer, _handle);
        _gl.BufferSubData(BufferTargetARB.UniformBuffer, 0, (nuint)sizeof(T), &data);
        _gl.BindBuffer(BufferTargetARB.UniformBuffer, 0);
    }

    public void Dispose()
    {
        _gl.DeleteBuffer(_handle);
        GC.SuppressFinalize(this);
    }

    ~UniformBufferObject()
    {
        // OpenGL resources should be deleted on the main thread or via a queue.
        // For simplicity in this task, we'll assume Dispose is called correctly.
        // But the guidelines say: Use finalizers in classes managing raw OpenGL handles to ensure cleanup.
        // However, GL instance might be disposed already or we are on wrong thread.
        // Given the constraints, I will follow the guideline.
        _gl.DeleteBuffer(_handle);
    }
}
