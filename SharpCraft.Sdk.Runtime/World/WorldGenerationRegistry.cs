using SharpCraft.Sdk.World;

namespace SharpCraft.Sdk.Runtime.World;

/// <summary>
/// Runtime implementation of the world generation registry.
/// </summary>
public class WorldGenerationRegistry : Registry<IWorldGenerator>, IWorldGenerationRegistry
{
}
