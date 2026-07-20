namespace SharpCraft.Engine.Rendering.Lighting;

public class LightingSystem : ILightingSystem
{
    private readonly List<PointLight> _pointLights = [];
    public DirectionalLight Sun { get; } = new();
    IDirectionalLight ILightingSystem.Sun => Sun;
    public IWorldTime? WorldTime { get; set; }

    public void AddPointLight(PointLight light) => _pointLights.Add(light);
    public void RemovePointLight(PointLight light) => _pointLights.Remove(light);

    public IEnumerable<PointLight> GetActivePointLights() => _pointLights.Where(l => l.IsEnabled);
}