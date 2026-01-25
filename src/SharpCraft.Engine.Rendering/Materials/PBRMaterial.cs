using System.Numerics;
using SharpCraft.Engine.Rendering.Shaders;

namespace SharpCraft.Engine.Rendering.Materials;

/// <summary>
/// Represents a physically-based rendering material using the metallic/roughness workflow.
/// Compatible with glTF 2.0 and UE4/5 material model.
/// Reference: https://google.github.io/filament/Filament.md.html#materialsystem/parameterization
/// </summary>
public sealed class PBRMaterial : IDisposable
{
    private readonly GL _gl;
    private bool _disposed;

    /// <summary>
    /// Base color (albedo) in sRGB color space. Alpha channel used for transparency.
    /// </summary>
    public Vector4 BaseColor { get; set; } = Vector4.One;

    /// <summary>
    /// Metallic factor [0-1]. 0 = dielectric, 1 = metal.
    /// </summary>
    public float Metallic { get; set; } = 0.0f;

    /// <summary>
    /// Roughness factor [0-1]. 0 = smooth/mirror, 1 = rough/diffuse.
    /// </summary>
    public float Roughness { get; set; } = 0.5f;

    /// <summary>
    /// Ambient occlusion factor [0-1]. 1 = no occlusion.
    /// </summary>
    public float AmbientOcclusion { get; set; } = 1.0f;

    /// <summary>
    /// Emissive color in linear RGB. Multiplied by EmissiveStrength.
    /// </summary>
    public Vector3 EmissiveColor { get; set; } = Vector3.Zero;

    /// <summary>
    /// Emissive strength multiplier for HDR emission.
    /// </summary>
    public float EmissiveStrength { get; set; } = 1.0f;

    /// <summary>
    /// Normal map strength [0-1].
    /// </summary>
    public float NormalScale { get; set; } = 1.0f;

    /// <summary>
    /// Alpha mode for transparency handling.
    /// </summary>
    public AlphaMode AlphaMode { get; set; } = AlphaMode.Opaque;

    /// <summary>
    /// Alpha cutoff threshold for Mask mode.
    /// </summary>
    public float AlphaCutoff { get; set; } = 0.5f;

    /// <summary>
    /// Whether the material is double-sided (disables backface culling).
    /// </summary>
    public bool DoubleSided { get; set; } = false;

    // Texture handles (0 = no texture, use factor instead)
    public uint BaseColorTexture { get; set; }
    public uint MetallicRoughnessTexture { get; set; }  // G = roughness, B = metallic (glTF convention)
    public uint NormalTexture { get; set; }
    public uint OcclusionTexture { get; set; }
    public uint EmissiveTexture { get; set; }

    /// <summary>
    /// Optional ORM packed texture (Occlusion, Roughness, Metallic in RGB).
    /// If set, overrides individual MetallicRoughness and Occlusion textures.
    /// </summary>
    public uint OrmTexture { get; set; }

    public PBRMaterial(GL gl)
    {
        _gl = gl;
    }

    /// <summary>
    /// Binds the material textures and sets shader uniforms.
    /// </summary>
    /// <param name="shader">Shader program to set uniforms on</param>
    /// <param name="startTextureUnit">First texture unit to use</param>
    public void Bind(ShaderProgram shader, int startTextureUnit = 0)
    {
        // Set material factors
        shader.SetUniform("material.baseColor", BaseColor);
        shader.SetUniform("material.metallic", Metallic);
        shader.SetUniform("material.roughness", Roughness);
        shader.SetUniform("material.ao", AmbientOcclusion);
        shader.SetUniform("material.emissive", EmissiveColor * EmissiveStrength);
        shader.SetUniform("material.normalScale", NormalScale);
        shader.SetUniform("material.alphaCutoff", AlphaCutoff);

        var unit = startTextureUnit;

        // Base color texture
        if (BaseColorTexture != 0)
        {
            _gl.ActiveTexture(TextureUnit.Texture0 + unit);
            _gl.BindTexture(TextureTarget.Texture2D, BaseColorTexture);
            shader.SetUniform("material.baseColorMap", unit);
            shader.SetUniform("material.hasBaseColorMap", 1);
            unit++;
        }
        else
        {
            shader.SetUniform("material.hasBaseColorMap", 0);
        }

        // ORM or separate metallic/roughness/occlusion
        if (OrmTexture != 0)
        {
            _gl.ActiveTexture(TextureUnit.Texture0 + unit);
            _gl.BindTexture(TextureTarget.Texture2D, OrmTexture);
            shader.SetUniform("material.ormMap", unit);
            shader.SetUniform("material.hasOrmMap", 1);
            unit++;
        }
        else
        {
            shader.SetUniform("material.hasOrmMap", 0);

            if (MetallicRoughnessTexture != 0)
            {
                _gl.ActiveTexture(TextureUnit.Texture0 + unit);
                _gl.BindTexture(TextureTarget.Texture2D, MetallicRoughnessTexture);
                shader.SetUniform("material.metallicRoughnessMap", unit);
                shader.SetUniform("material.hasMetallicRoughnessMap", 1);
                unit++;
            }
            else
            {
                shader.SetUniform("material.hasMetallicRoughnessMap", 0);
            }

            if (OcclusionTexture != 0)
            {
                _gl.ActiveTexture(TextureUnit.Texture0 + unit);
                _gl.BindTexture(TextureTarget.Texture2D, OcclusionTexture);
                shader.SetUniform("material.occlusionMap", unit);
                shader.SetUniform("material.hasOcclusionMap", 1);
                unit++;
            }
            else
            {
                shader.SetUniform("material.hasOcclusionMap", 0);
            }
        }

        // Normal map
        if (NormalTexture != 0)
        {
            _gl.ActiveTexture(TextureUnit.Texture0 + unit);
            _gl.BindTexture(TextureTarget.Texture2D, NormalTexture);
            shader.SetUniform("material.normalMap", unit);
            shader.SetUniform("material.hasNormalMap", 1);
            unit++;
        }
        else
        {
            shader.SetUniform("material.hasNormalMap", 0);
        }

        // Emissive map
        if (EmissiveTexture != 0)
        {
            _gl.ActiveTexture(TextureUnit.Texture0 + unit);
            _gl.BindTexture(TextureTarget.Texture2D, EmissiveTexture);
            shader.SetUniform("material.emissiveMap", unit);
            shader.SetUniform("material.hasEmissiveMap", 1);
        }
        else
        {
            shader.SetUniform("material.hasEmissiveMap", 0);
        }

        // Set culling based on double-sided
        if (DoubleSided)
        {
            _gl.Disable(EnableCap.CullFace);
        }
        else
        {
            _gl.Enable(EnableCap.CullFace);
        }

        // Set blend mode based on alpha mode
        switch (AlphaMode)
        {
            case AlphaMode.Opaque:
                _gl.Disable(EnableCap.Blend);
                shader.SetUniform("material.alphaMode", 0);
                break;
            case AlphaMode.Mask:
                _gl.Disable(EnableCap.Blend);
                shader.SetUniform("material.alphaMode", 1);
                break;
            case AlphaMode.Blend:
                _gl.Enable(EnableCap.Blend);
                _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                shader.SetUniform("material.alphaMode", 2);
                break;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        // Note: We don't own the textures, so we don't delete them here
        // The texture manager should handle texture lifetime

        _disposed = true;
    }
}

/// <summary>
/// Alpha blending mode for materials.
/// </summary>
public enum AlphaMode
{
    /// <summary>
    /// Fully opaque, alpha channel ignored.
    /// </summary>
    Opaque,

    /// <summary>
    /// Alpha testing with cutoff threshold.
    /// </summary>
    Mask,

    /// <summary>
    /// Alpha blending for transparency.
    /// </summary>
    Blend
}
