using System.Numerics;

namespace SharpCraft.Engine.Rendering.Shaders;

/// <summary>
/// A compute-shader program (research §2/§12.4: clustered shading leans on compute + SSBOs, core
/// in GL 4.3+). Compiles a single compute stage, dispatches workgroups, and inserts the memory
/// barrier callers need before the results are read by the next pass.
/// </summary>
public sealed class ComputeProgram : IDisposable
{
    private readonly GL _gl;
    private readonly uint _handle;
    private readonly Dictionary<string, int> _uniformLocations = new();
    private bool _disposed;

    public ComputeProgram(GL gl, string computeSource)
    {
        _gl = gl;

        var shader = _gl.CreateShader(ShaderType.ComputeShader);
        _gl.ShaderSource(shader, computeSource);
        _gl.CompileShader(shader);
        var infoLog = _gl.GetShaderInfoLog(shader);
        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            throw new ShaderCompilationException($"Error compiling compute shader: {infoLog}");
        }

        _handle = _gl.CreateProgram();
        _gl.AttachShader(_handle, shader);
        _gl.LinkProgram(_handle);
        _gl.GetProgram(_handle, ProgramPropertyARB.LinkStatus, out var linkStatus);
        if (linkStatus == 0)
        {
            throw new ShaderCompilationException($"Error linking compute program: {_gl.GetProgramInfoLog(_handle)}");
        }

        _gl.DetachShader(_handle, shader);
        _gl.DeleteShader(shader);
    }

    public void Use() => _gl.UseProgram(_handle);

    /// <summary>Dispatch the given number of workgroups and barrier on SSBO writes.</summary>
    public void Dispatch(uint groupsX, uint groupsY, uint groupsZ)
    {
        _gl.DispatchCompute(groupsX, groupsY, groupsZ);
        _gl.MemoryBarrier((uint)MemoryBarrierMask.ShaderStorageBarrierBit);
    }

    public void SetUniform(string name, int value) => _gl.Uniform1(Loc(name), value);
    public void SetUniform(string name, uint value) => _gl.Uniform1(Loc(name), value);
    public void SetUniform(string name, float value) => _gl.Uniform1(Loc(name), value);
    public void SetUniform(string name, Vector2 v) => _gl.Uniform2(Loc(name), v.X, v.Y);
    public void SetUniform(string name, Vector3 v) => _gl.Uniform3(Loc(name), v.X, v.Y, v.Z);
    public void SetUniform(string name, Vector4 v) => _gl.Uniform4(Loc(name), v.X, v.Y, v.Z, v.W);

    public unsafe void SetUniform(string name, Matrix4x4 value)
        => _gl.UniformMatrix4(Loc(name), 1, false, (float*)&value);

    private int Loc(string name)
    {
        if (!_uniformLocations.TryGetValue(name, out var location))
        {
            location = _gl.GetUniformLocation(_handle, name);
            _uniformLocations[name] = location;
        }
        return location;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _gl.DeleteProgram(_handle);
        _disposed = true;
    }
}