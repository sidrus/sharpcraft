namespace SharpCraft.Engine.Rendering;

/// <summary>
/// Cascaded shadow map (research §8): a single depth <see cref="TextureTarget.Texture2DArray"/>
/// with one layer per cascade. The view frustum is split along Z into N cascades; each cascade is
/// rendered from the sun into its own layer, near cascades getting high effective resolution and
/// far ones covering more area. Conventional (non-reversed) ortho depth per cascade, so the layers
/// keep the classic LEQUAL compare; reversed-Z only applies to the main camera passes.
/// </summary>
public sealed class CascadedShadowMap : IDisposable
{
    private readonly GL _gl;
    private readonly uint _fbo;
    private readonly uint _depthArray;
    private readonly uint _size;

    public uint DepthArray => _depthArray;
    public uint Size => _size;
    public int CascadeCount
    {
        get;
    }

    public CascadedShadowMap(GL gl, uint size, int cascadeCount)
    {
        _gl = gl;
        _size = size;
        CascadeCount = cascadeCount;

        _depthArray = _gl.CreateTexture(TextureTarget.Texture2DArray);
        _gl.TextureStorage3D(_depthArray, 1, (GLEnum)SizedInternalFormat.DepthComponent32f, size, size, (uint)cascadeCount);

        // Hardware PCF: linear filtering + depth comparison.
        _gl.TextureParameter(_depthArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TextureParameter(_depthArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        _gl.TextureParameter(_depthArray, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.CompareRefToTexture);
        _gl.TextureParameter(_depthArray, TextureParameterName.TextureCompareFunc, (int)DepthFunction.Lequal);
        _gl.TextureParameter(_depthArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
        _gl.TextureParameter(_depthArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);

        // Border = far depth (1.0) so samples outside a cascade read as fully lit, never shadowed.
        Span<float> border = stackalloc float[] { 1.0f, 1.0f, 1.0f, 1.0f };
        unsafe
        {
            fixed (float* p = border)
            {
                _gl.TextureParameter(_depthArray, (GLEnum)TextureParameterName.TextureBorderColor, p);
            }
        }

        _fbo = _gl.CreateFramebuffer();
        _gl.NamedFramebufferDrawBuffer(_fbo, ColorBuffer.None);
        _gl.NamedFramebufferReadBuffer(_fbo, ColorBuffer.None);
        // Attach layer 0 just to validate completeness; the pass re-attaches per cascade.
        _gl.NamedFramebufferTextureLayer(_fbo, (GLEnum)FramebufferAttachment.DepthAttachment, _depthArray, 0, 0);

        var status = (FramebufferStatus)_gl.CheckNamedFramebufferStatus(_fbo, FramebufferTarget.Framebuffer);
        if (status != FramebufferStatus.Complete)
        {
            throw new Exception($"Cascaded shadow map framebuffer is not complete! Status: {status}");
        }
    }

    /// <summary>Bind the FBO and route depth writes into the given cascade layer.</summary>
    public void BindLayer(int cascade)
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        _gl.NamedFramebufferTextureLayer(_fbo, (GLEnum)FramebufferAttachment.DepthAttachment, _depthArray, 0, cascade);
        _gl.Viewport(0, 0, _size, _size);
    }

    public void Unbind()
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Dispose()
    {
        _gl.DeleteFramebuffer(_fbo);
        _gl.DeleteTexture(_depthArray);
    }
}