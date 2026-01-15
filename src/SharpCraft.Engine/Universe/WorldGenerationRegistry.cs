using SharpCraft.Sdk.Universe;
using SdkIWorldGenerator = SharpCraft.Sdk.Universe.IWorldGenerator;

namespace SharpCraft.Engine.Universe;

/// <summary>
/// Runtime implementation of the world generation registry.
/// </summary>
public class WorldGenerationRegistry : Registry<SdkIWorldGenerator>, IWorldGenerationRegistry
{
}
