using SharpCraft.Client.Controllers;
using SharpCraft.Engine.Rendering;
using SharpCraft.Engine.Rendering.Lighting;
using SharpCraft.Sdk;
using SharpCraft.Sdk.Diagnostics;
using SharpCraft.Sdk.Lifecycle;
using SharpCraft.Sdk.UI;
using SharpCraft.Sdk.Universe;

namespace SharpCraft.Client.UI;

public record struct HudContext(
    LocalPlayerController? LocalPlayer,
    ChunkMeshManager? MeshManager,
    LightingSystem? Lighting,
    PostProcessingRenderer? PostProcessing,
    ISharpCraftSdk Sdk,
    IEnumerable<IMod> LoadedMods,
    IAvatarProvider? Avatar = null,
    IDiagnosticsProvider? Diagnostics = null
) : IHudContext
{
    public IPlayer? Player => LocalPlayer;
}