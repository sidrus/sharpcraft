using Silk.NET.OpenGL;

namespace SharpCraft.Client.Rendering.IBL;

/// <summary>
/// Manages a complete IBL environment including environment cubemap,
/// irradiance map, prefiltered specular map, and BRDF LUT.
/// </summary>
public sealed class IBLEnvironment : IDisposable
{
    private readonly GL _gl;
    private bool _disposed;

    /// <summary>
    /// The environment cubemap converted from equirectangular HDR.
    /// </summary>
    public Cubemap? EnvironmentMap { get; private set; }

    /// <summary>
    /// Diffuse irradiance cubemap for ambient diffuse lighting.
    /// </summary>
    public Cubemap? IrradianceMap { get; private set; }

    /// <summary>
    /// Prefiltered specular cubemap with roughness levels in mip chain.
    /// </summary>
    public Cubemap? PrefilterMap { get; private set; }

    /// <summary>
    /// BRDF integration LUT for split-sum approximation.
    /// </summary>
    public uint BrdfLut { get; private set; }

    /// <summary>
    /// Whether the IBL environment is fully initialized and ready for use.
    /// </summary>
    public bool IsReady => EnvironmentMap != null && IrradianceMap != null && 
                           PrefilterMap != null && BrdfLut != 0;

    public IBLEnvironment(GL gl)
    {
        _gl = gl;
    }

    /// <summary>
    /// Generates all IBL resources from an equirectangular HDR texture.
    /// </summary>
    /// <param name="generator">IBL generator instance</param>
    /// <param name="hdrTexture">Source HDR equirectangular texture handle</param>
    /// <param name="envMapSize">Environment cubemap size (default 512)</param>
    /// <param name="irradianceSize">Irradiance map size (default 32)</param>
    /// <param name="prefilterSize">Prefilter map size (default 128)</param>
    /// <param name="brdfLutSize">BRDF LUT size (default 512)</param>
    public void GenerateFromHdr(
        IBLGenerator generator,
        uint hdrTexture,
        int envMapSize = 512,
        int irradianceSize = 32,
        int prefilterSize = 128,
        int brdfLutSize = 512)
    {
        // Dispose existing resources
        DisposeResources();

        // Store current viewport to restore later
        Span<int> viewport = stackalloc int[4];
        _gl.GetInteger(GetPName.Viewport, viewport);

        // Generate environment cubemap from equirectangular
        EnvironmentMap = generator.GenerateEnvironmentCubemap(hdrTexture, envMapSize);

        // Generate irradiance map for diffuse IBL
        IrradianceMap = generator.GenerateIrradianceMap(EnvironmentMap, irradianceSize);

        // Generate prefiltered specular map
        PrefilterMap = generator.GeneratePrefilterMap(EnvironmentMap, prefilterSize);

        // Generate BRDF LUT (only needs to be done once, could be cached)
        BrdfLut = generator.GenerateBrdfLut(brdfLutSize);

        // Restore viewport
        _gl.Viewport(viewport[0], viewport[1], (uint)viewport[2], (uint)viewport[3]);
    }

    /// <summary>
    /// Generates IBL from a procedural sky (atmosphere model).
    /// Useful when no HDRI is available.
    /// </summary>
    /// <param name="generator">IBL generator instance</param>
    /// <param name="skyCubemap">Pre-rendered sky cubemap</param>
    /// <param name="irradianceSize">Irradiance map size</param>
    /// <param name="prefilterSize">Prefilter map size</param>
    /// <param name="brdfLutSize">BRDF LUT size</param>
    public void GenerateFromSkyCubemap(
        IBLGenerator generator,
        Cubemap skyCubemap,
        int irradianceSize = 32,
        int prefilterSize = 128,
        int brdfLutSize = 512)
    {
        // Dispose existing resources (except environment map which is provided)
        IrradianceMap?.Dispose();
        PrefilterMap?.Dispose();
        if (BrdfLut != 0) _gl.DeleteTexture(BrdfLut);

        // Store current viewport
        Span<int> viewport = stackalloc int[4];
        _gl.GetInteger(GetPName.Viewport, viewport);

        EnvironmentMap = skyCubemap;
        IrradianceMap = generator.GenerateIrradianceMap(skyCubemap, irradianceSize);
        PrefilterMap = generator.GeneratePrefilterMap(skyCubemap, prefilterSize);
        BrdfLut = generator.GenerateBrdfLut(brdfLutSize);

        // Restore viewport
        _gl.Viewport(viewport[0], viewport[1], (uint)viewport[2], (uint)viewport[3]);
    }

    /// <summary>
    /// Binds all IBL textures to consecutive texture units starting from the specified unit.
    /// </summary>
    /// <param name="startUnit">First texture unit to use</param>
    public void Bind(uint startUnit = 6)
    {
        if (!IsReady) return;

        IrradianceMap?.Bind(startUnit);
        PrefilterMap?.Bind(startUnit + 1);

        _gl.ActiveTexture(TextureUnit.Texture0 + (int)(startUnit + 2));
        _gl.BindTexture(TextureTarget.Texture2D, BrdfLut);
    }

    private void DisposeResources()
    {
        EnvironmentMap?.Dispose();
        EnvironmentMap = null;

        IrradianceMap?.Dispose();
        IrradianceMap = null;

        PrefilterMap?.Dispose();
        PrefilterMap = null;

        if (BrdfLut != 0)
        {
            _gl.DeleteTexture(BrdfLut);
            BrdfLut = 0;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        DisposeResources();
        _disposed = true;
    }

    ~IBLEnvironment()
    {
        // Note: OpenGL resources should be deleted on the GL thread
    }
}
