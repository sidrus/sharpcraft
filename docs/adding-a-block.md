# Adding a Custom Block to SharpCraft

This guide explains how to add a new block to the game using the SharpCraft SDK. We will use a "Smooth Stone" block as an example.

## Prerequisites
- **Namespacing**: All block IDs must follow the `namespace:block_id` pattern (e.g., `mymod:smooth_stone`).
- **Assets**: Texture paths are relative to the mod's `assets/` directory.

---

## Tier 1: Compiled Mod (C# DLL)
Tier 1 mods are best for complex logic and high-performance features.

### 1. Implement `IMod`
Create a class that implements the `IMod` interface. The SDK instance will be provided by the engine (typically via constructor injection).

```csharp
using SharpCraft.Sdk;
using SharpCraft.Sdk.Blocks;
using SharpCraft.Sdk.Lifecycle;

namespace MyStoneMod;

public class StoneMod : IMod
{
    private readonly ISharpCraftSdk _sdk;

    // The SDK entry point is injected by the engine loader
    public StoneMod(ISharpCraftSdk sdk)
    {
        _sdk = sdk;
    }

    public ModManifest Manifest => new ModManifest(
        Id: "my_stone_mod",
        Name: "Stone Expansion",
        Author: "Developer",
        Version: "1.0.0",
        Dependencies: Array.Empty<string>(),
        Capabilities: Array.Empty<string>()
    );

    public void OnEnable()
    {
        // Define the block properties
        var smoothStone = new BlockDefinition(
            Id: "my_stone_mod:blocks/stone/smooth",
            Name: "Smooth Stone",
            IsSolid: true,
            Friction: 0.6f,
            TextureTop: "my_stone_mod:textures/block/smooth_stone_top",
            TextureSides: "my_stone_mod:textures/block/smooth_stone_side"
        );

        // Register the block in the global registry
        _sdk.Blocks.Register(smoothStone.Id, smoothStone);
    }

    public void OnDisable()
    {
        // Optional: Cleanup logic when mod is disabled
    }
}
```

---

## Tier 2: Scripted Mod (.csx)
Tier 2 mods are lightweight scripts that are perfect for quick content additions.

### 1. Create `mod.json`
Every mod needs a manifest file in its root directory.

```json
{
  "id": "script_stone_mod",
  "name": "Scripted Stone",
  "author": "Modder",
  "version": "1.0.0",
  "entrypoints": ["scripts/main.csx"]
}
```

### 2. Write the Script (`scripts/main.csx`)
The script has direct access to the `Sdk` global object.

```csharp
using SharpCraft.Sdk.Blocks;

// Define the block
var scriptedStone = new BlockDefinition(
    Id: "script_stone_mod:blocks/stone/magic",
    Name: "Magic Stone",
    IsSolid: true,
    Friction: 0.8f
);

// Register via the global Sdk object
Sdk.Blocks.Register(scriptedStone.Id, scriptedStone);

Console.WriteLine("Magic Stone has been registered!");
```

---

## Common Block Properties

The `BlockDefinition` record supports several properties:

| Property | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `Id` | `string` | *Required* | Unique ID in `namespace:blocks/type/variant` format. |
| `Name` | `string` | *Required* | Display name in the UI. |
| `IsSolid` | `bool` | `true` | Whether the block has collision. |
| `IsTransparent` | `bool` | `false` | Whether light passes through and adjacent faces are rendered. |
| `Friction` | `float` | `0.5f` | Movement friction on top of the block. |
| `TextureTop` | `string?` | `null` | Asset path for the top face. |
| `TextureBottom` | `string?` | `null` | Asset path for the bottom face. |
| `TextureSides` | `string?` | `null` | Asset path for all side faces. |
