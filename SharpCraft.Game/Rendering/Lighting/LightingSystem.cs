namespace SharpCraft.Game.Rendering.Lighting;

public class LightingSystem
{
    private readonly List<PointLight> _pointLights = [];
    private readonly List<SpotLight> _spotLights = [];
    public DirectionalLight Sun { get; } = new();

    public void AddPointLight(PointLight light) => _pointLights.Add(light);
    public void RemovePointLight(PointLight light) => _pointLights.Remove(light);

    public void AddSpotLight(SpotLight light) => _spotLights.Add(light);
    public void RemoveSpotLight(SpotLight light) => _spotLights.Remove(light);

    public IEnumerable<PointLight> GetActivePointLights() => _pointLights.Where(l => l.IsEnabled);
    public IEnumerable<SpotLight> GetActiveSpotLights() => _spotLights.Where(l => l.IsEnabled);
}