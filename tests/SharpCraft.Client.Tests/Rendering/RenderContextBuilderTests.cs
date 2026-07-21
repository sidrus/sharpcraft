using AwesomeAssertions;
using SharpCraft.Engine.Rendering;
using SharpCraft.Engine.Rendering.Lighting;
using SharpCraft.Sdk.UI;
using System.Numerics;

namespace SharpCraft.Client.Tests.Rendering;

public class RenderContextBuilderTests
{
    private static RenderContext Build(IGraphicsSettings settings, bool isUnderwater, float viewDistance)
    {
        return RenderContextBuilder.Build(
            Matrix4x4.Identity, Matrix4x4.Identity, Vector3.Zero,
            fogColor: Vector3.One, viewDistance: viewDistance,
            screenWidth: 800, screenHeight: 600,
            sun: new DirectionalLightData(Vector3.UnitY, Vector3.One, 1f),
            pointLights: [],
            isUnderwater: isUnderwater, time: 0f,
            settings: settings,
            atmosphereRayleigh: 1f, atmosphereMie: 1f, atmosphereOzone: 1f, atmosphereMieG: 0.8f);
    }

    [Fact]
    public void Build_WhenUnderwater_ShouldClampFogNearAndFar()
    {
        var context = Build(new GraphicsSettings(), isUnderwater: true, viewDistance: 128f);

        context.Fog.FogNear.Should().Be(0f);
        context.Fog.FogFar.Should().Be(20f);
    }

    [Fact]
    public void Build_WhenNotUnderwater_ShouldScaleFogByViewDistanceAndSettings()
    {
        var settings = new GraphicsSettings { FogNearFactor = 0.25f, FogFarFactor = 0.9f };

        var context = Build(settings, isUnderwater: false, viewDistance: 200f);

        context.Fog.FogNear.Should().Be(200f * 0.25f);
        context.Fog.FogFar.Should().Be(200f * 0.9f);
    }

    [Fact]
    public void Build_WithMorePointLightsThanCap_ShouldTruncateToMaxPointLights()
    {
        var settings = new GraphicsSettings { MaxPointLights = 2 };
        var lights = new[]
        {
            new PointLightData(Vector3.Zero, Vector3.One, 1f, 1f, 0.1f, 0.1f),
            new PointLightData(Vector3.One, Vector3.One, 1f, 1f, 0.1f, 0.1f),
            new PointLightData(new Vector3(2f), Vector3.One, 1f, 1f, 0.1f, 0.1f),
        };

        var context = RenderContextBuilder.Build(
            Matrix4x4.Identity, Matrix4x4.Identity, Vector3.Zero,
            fogColor: Vector3.One, viewDistance: 100f,
            screenWidth: 800, screenHeight: 600,
            sun: new DirectionalLightData(Vector3.UnitY, Vector3.One, 1f),
            pointLights: lights,
            isUnderwater: false, time: 0f,
            settings: settings,
            atmosphereRayleigh: 1f, atmosphereMie: 1f, atmosphereOzone: 1f, atmosphereMieG: 0.8f);

        context.Lighting.PointLights.Should().HaveCount(2);
    }

    [Fact]
    public void Build_WithFewerPointLightsThanCap_ShouldKeepAll()
    {
        var settings = new GraphicsSettings { MaxPointLights = 8 };
        var lights = new[] { new PointLightData(Vector3.Zero, Vector3.One, 1f, 1f, 0.1f, 0.1f) };

        var context = RenderContextBuilder.Build(
            Matrix4x4.Identity, Matrix4x4.Identity, Vector3.Zero,
            fogColor: Vector3.One, viewDistance: 100f,
            screenWidth: 800, screenHeight: 600,
            sun: new DirectionalLightData(Vector3.UnitY, Vector3.One, 1f),
            pointLights: lights,
            isUnderwater: false, time: 0f,
            settings: settings,
            atmosphereRayleigh: 1f, atmosphereMie: 1f, atmosphereOzone: 1f, atmosphereMieG: 0.8f);

        context.Lighting.PointLights.Should().HaveCount(1);
    }

    [Fact]
    public void Build_WithConfiguredSettings_ShouldMapThemToContext()
    {
        var settings = new GraphicsSettings
        {
            UseIbl = false,
            SsaoRadius = 3.3f,
            Gamma = 1.9f,
            NormalStrength = 4.2f,
            UseContactShadows = false,
        };

        var context = Build(settings, isUnderwater: false, viewDistance: 100f);

        context.Effects.UseIbl.Should().BeFalse();
        context.Effects.SsaoRadius.Should().Be(3.3f);
        context.Exposure.Gamma.Should().Be(1.9f);
        context.Pbr.NormalStrength.Should().Be(4.2f);
        context.Effects.UseContactShadows.Should().BeFalse();
    }
}
