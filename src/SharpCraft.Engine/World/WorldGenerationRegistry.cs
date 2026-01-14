using SharpCraft.Sdk.World;

namespace SharpCraft.Engine.World;

/// <summary>
/// Runtime implementation of the world generation registry.
/// </summary>
public class WorldGenerationRegistry : Registry<IWorldGenerator>, IWorldGenerationRegistry
{
}
