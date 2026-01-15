using SharpCraft.Client.Controllers;
using SharpCraft.Client.Rendering;
using SharpCraft.Client.Rendering.Lighting;
using SharpCraft.Engine.Universe;
using SharpCraft.Sdk;
using SharpCraft.Sdk.Lifecycle;

namespace SharpCraft.Client.UI;

public record struct HudContext(
    World World,
    LocalPlayerController? Player,
    ChunkMeshManager? MeshManager,
    LightingSystem? Lighting,
    ISharpCraftSdk? Sdk,
    IEnumerable<IMod>? LoadedMods
);
