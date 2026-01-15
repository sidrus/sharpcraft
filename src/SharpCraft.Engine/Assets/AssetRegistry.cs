using SharpCraft.Sdk.Assets;
using SharpCraft.Sdk.Resources;
using SharpCraft.Engine.Resources;

namespace SharpCraft.Engine.Assets;

/// <summary>
/// Runtime implementation of the asset registry.
/// </summary>
public class AssetRegistry : ResourceRegistry<TextureData>, IAssetRegistry;
