using SharpCraft.Engine.Rendering.IBL;
using System.Numerics;

namespace SharpCraft.Engine.Rendering.Passes;

/// <summary>
/// Bakes the image-based lighting environment (diffuse irradiance + specular prefilter + BRDF LUT)
/// from the current sun/atmosphere, exposing the maps to the forward pass. Throttled internally, so
/// most frames the update is a no-op. Disabled when IBL is off.
/// </summary>
public sealed class IblPass(GL gl) : IRenderPass
{
    private readonly IblBaker _baker = new(gl);

    public string Name => "IblBake";

    public IReadOnlyList<RenderResource> Reads => [];

    public IReadOnlyList<RenderResource> Writes => [RenderResource.IrradianceMap, RenderResource.PrefilterMap, RenderResource.BrdfLut];

    public bool Enabled(RenderContext context)
    {
        return context.Effects.UseIbl;
    }

    public void Execute(IWorld world, RenderContext context, RenderTargets targets)
    {
        var lightDir = context.Lighting.Sun?.Direction ?? Vector3.Normalize(new Vector3(0.8f, -0.5f, 0.1f));
        var toSun = Vector3.Normalize(-lightDir);
        var sunColor = context.Lighting.Sun?.Color ?? new Vector3(1.0f, 0.95f, 0.8f);
        _baker.Update(toSun, sunColor, context.Atmosphere.MieG, captureIntensity: 4.0f);

        if (_baker.IsReady)
        {
            targets.IrradianceMap = _baker.IrradianceMap;
            targets.PrefilterMap = _baker.PrefilterMap;
            targets.BrdfLut = _baker.BrdfLut;
        }
    }

    public void Dispose()
    {
        _baker.Dispose();
    }
}
