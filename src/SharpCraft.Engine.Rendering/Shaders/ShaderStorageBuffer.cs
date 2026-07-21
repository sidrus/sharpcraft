using System.Runtime.CompilerServices;

namespace SharpCraft.Engine.Rendering.Shaders;

/// <summary>
/// A shader storage buffer object (SSBO) on a fixed binding point. Used for the clustered-shading
/// data the compute passes produce and the shading pass reads: cluster AABBs, the light list, the
/// per-cluster light grid, and the global light-index list (research §2/§12.4).
/// </summary>
public sealed unsafe class ShaderStorageBuffer : IDisposable
{
    private readonly GL _gl;
    private nuint _capacity;
    private bool _disposed;

    public uint Binding
    {
        get;
    }
    public uint Handle
    {
        get;
    }

    public ShaderStorageBuffer(GL gl, uint binding)
    {
        _gl = gl;
        Binding = binding;
        Handle = _gl.CreateBuffer();
    }

    /// <summary>Reserve (and zero) <paramref name="sizeBytes"/> of storage.</summary>
    public void Allocate(nuint sizeBytes)
    {
        _gl.NamedBufferData(Handle, sizeBytes, null, BufferUsageARB.DynamicDraw);
        _capacity = sizeBytes;
    }

    /// <summary>Upload an array of unmanaged elements, growing the storage if needed.</summary>
    public void Update<T>(ReadOnlySpan<T> data) where T : unmanaged
    {
        var bytes = (nuint)(data.Length * Unsafe.SizeOf<T>());
        fixed (T* p = data)
        {
            if (bytes > _capacity)
            {
                _gl.NamedBufferData(Handle, bytes, p, BufferUsageARB.DynamicDraw);
                _capacity = bytes;
            }
            else if (bytes > 0)
            {
                _gl.NamedBufferSubData(Handle, 0, bytes, p);
            }
        }
    }

    /// <summary>Write a single uint at offset 0 (used to reset the global index counter each frame).</summary>
    public void SetUInt(uint value)
    {
        _gl.NamedBufferSubData(Handle, 0, sizeof(uint), &value);
    }

    /// <summary>Write a single float at offset 0 (used to seed the persistent exposure value).</summary>
    public void SetFloat(float value)
    {
        _gl.NamedBufferSubData(Handle, 0, sizeof(float), &value);
    }

    public void BindBase()
    {
        _gl.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, Binding, Handle);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _gl.DeleteBuffer(Handle);
        _disposed = true;
    }
}