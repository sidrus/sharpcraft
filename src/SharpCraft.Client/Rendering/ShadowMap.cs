using Silk.NET.OpenGL;

namespace SharpCraft.Client.Rendering;

public class ShadowMap : IDisposable
{
    private readonly GL _gl;
    private readonly uint _fbo;
    private readonly uint _depthMap;
    private readonly uint _width;
    private readonly uint _height;

    public uint DepthMap => _depthMap;
    public uint Width => _width;
    public uint Height => _height;

    public ShadowMap(GL gl, uint width, uint height)
    {
        _gl = gl;
        _width = width;
        _height = height;

        _fbo = _gl.GenFramebuffer();
        _depthMap = _gl.GenTexture();

        _gl.BindTexture(TextureTarget.Texture2D, _depthMap);
        unsafe
        {
            _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.DepthComponent32f, _width, _height, 0, PixelFormat.DepthComponent, PixelType.Float, null);
        }

        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.CompareRefToTexture);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareFunc, (int)DepthFunction.Lequal);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
        
        float[] borderColor = { 1.0f, 1.0f, 1.0f, 1.0f };
        unsafe
        {
            fixed (float* p = borderColor)
            {
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, p);
            }
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, _depthMap, 0);
        _gl.DrawBuffer(DrawBufferMode.None);
        _gl.ReadBuffer(ReadBufferMode.None);

        if ((FramebufferStatus)_gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferStatus.FramebufferComplete)
        {
            throw new Exception("Shadow map framebuffer is not complete!");
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Bind()
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        _gl.Viewport(0, 0, _width, _height);
    }

    public void Unbind()
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Dispose()
    {
        _gl.DeleteFramebuffer(_fbo);
        _gl.DeleteTexture(_depthMap);
    }
}
