using System.Numerics;
using Silk.NET.OpenGL;

namespace SharpCraft.Client.Rendering.Shaders;

public class ShaderProgram : IDisposable
{
    private readonly GL _gl;
    private readonly uint _handle;
    private readonly Dictionary<string, int> _uniformLocations = new();

    public ShaderProgram(GL gl, string vertexSource, string fragmentSource)
    {
        _gl = gl;

        var vertex = CompileShader(ShaderType.VertexShader, vertexSource);
        var fragment = CompileShader(ShaderType.FragmentShader, fragmentSource);

        _handle = gl.CreateProgram();
        gl.AttachShader(_handle, vertex);
        gl.AttachShader(_handle, fragment);
        gl.LinkProgram(_handle);

        _gl.DetachShader(_handle, vertex);
        _gl.DetachShader(_handle, fragment);
        gl.DeleteShader(vertex);
        gl.DeleteShader(fragment);
    }

    public void Use() => _gl.UseProgram(_handle);

    public void SetUniform(string name, int value) => _gl.Uniform1(GetUniformLocation(name), value);

    public void SetUniform(string name, float value) => _gl.Uniform1(GetUniformLocation(name), value);

    public void SetUniform(string name, Vector3 value) => _gl.Uniform3(GetUniformLocation(name), value.X, value.Y, value.Z);

    public unsafe void SetUniform(string name, Matrix4x4 value)
    {
        _gl.UniformMatrix4(GetUniformLocation(name), 1, false, (float*)&value);
    }

    private int GetUniformLocation(string name)
    {
        if (!_uniformLocations.TryGetValue(name, out var location))
        {
            location = _gl.GetUniformLocation(_handle, name);
            _uniformLocations[name] = location;
        }
        return location;
    }

    private uint CompileShader(ShaderType type, string source)
    {
        var handle = _gl.CreateShader(type);
        _gl.ShaderSource(handle, source);
        _gl.CompileShader(handle);
        var infoLog = _gl.GetShaderInfoLog(handle);
        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            throw new ShaderCompilationException($"Error compiling {type}: {infoLog}");
        }

        return handle;
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
            if (disposing)
            {
                // Dispose managed state (none)
            }

            _gl.DeleteProgram(_handle);
            _disposed = true;
        }
    }

    ~ShaderProgram()
    {
        Dispose(false);
    }

    private bool _disposed;
}