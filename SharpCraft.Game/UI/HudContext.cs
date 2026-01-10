using SharpCraft.Core;
using SharpCraft.Game.Controllers;
using SharpCraft.Game.Rendering;
using SharpCraft.Game.Rendering.Lighting;

namespace SharpCraft.Game.UI;

public record struct HudContext(
    World World,
    LocalPlayerController? Player,
    ChunkMeshManager? MeshManager,
    LightingSystem? Lighting
);
