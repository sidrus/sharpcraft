using SharpCraft.Client.Controllers;
using SharpCraft.Client.Rendering;
using SharpCraft.Client.Rendering.Lighting;
using SharpCraft.Engine.Universe;
using SharpCraft.Sdk;
using SharpCraft.Sdk.Diagnostics;
using SharpCraft.Sdk.Lifecycle;
using SharpCraft.Sdk.UI;
using SharpCraft.Sdk.Universe;

namespace SharpCraft.Client.UI;

public record struct HudContext(
    World World,
    LocalPlayerController? LocalPlayer,
    ChunkMeshManager? MeshManager,
    LightingSystem? Lighting,
    ISharpCraftSdk Sdk,
    IEnumerable<IMod> LoadedMods,
    IAvatarProvider? Avatar = null,
    IDiagnosticsProvider? Diagnostics = null
) : IHudContext
{
    public IPlayer? Player => LocalPlayer;
}
