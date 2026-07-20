namespace SharpCraft.Engine.Rendering.Lighting;

public class LightingSystem : ILightingSystem
{
    private readonly List<PointLightData> _pointLights = [];
    public DirectionalLight Sun { get; } = new();
    IDirectionalLight ILightingSystem.Sun => Sun;
    public IWorldTime? WorldTime
    {
        get; set;
    }

    public void AddPointLight(PointLightData light) => _pointLights.Add(light);

    public IReadOnlyList<PointLightData> GetActivePointLights() => _pointLights;
}