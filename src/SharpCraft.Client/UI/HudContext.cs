using SharpCraft.Client.Controllers;
using SharpCraft.Client.Rendering;
using SharpCraft.Client.Rendering.Lighting;
using SharpCraft.Engine.Universe;

namespace SharpCraft.Client.UI;

public record struct HudContext(
    World World,
    LocalPlayerController? Player,
    ChunkMeshManager? MeshManager,
    LightingSystem? Lighting
);
