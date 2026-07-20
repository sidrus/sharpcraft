using SharpCraft.Sdk.Universe;

namespace SharpCraft.Sdk.Rendering;

public interface ILightingSystem
{
    IDirectionalLight Sun
    {
        get;
    }
    IWorldTime? WorldTime
    {
        get; set;
    }
}