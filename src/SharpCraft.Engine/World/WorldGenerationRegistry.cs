using SharpCraft.Sdk.World;
using SdkIWorldGenerator = SharpCraft.Sdk.World.IWorldGenerator;

namespace SharpCraft.Engine.World;

/// <summary>
/// Runtime implementation of the world generation registry.
/// </summary>
public class WorldGenerationRegistry : Registry<SdkIWorldGenerator>, IWorldGenerationRegistry
{
}
