# Adding a Custom Terrain Generator to SharpCraft

This guide explains how to create and register a custom terrain generator using the SharpCraft SDK. Terrain generators are responsible for populating chunks with blocks during world generation.

## Prerequisites
- **Namespacing**: Generator IDs must follow the `namespace:id` pattern (e.g., `mymod:floating_islands`).
- **Determinism**: You **must** use the provided `seed` to ensure the world is identical for all players on the same seed.

---

## Tier 1: Compiled Mod (C# DLL)
Tier 1 mods offer the best performance for complex noise-based generation.

### 1. Implement `IWorldGenerator`
Create a class that implements the `SharpCraft.Sdk.World.IWorldGenerator` interface.

```csharp
using SharpCraft.Sdk;
using SharpCraft.Sdk.World;
using SharpCraft.Sdk.Lifecycle;

namespace MyBiomeMod;

public class SuperFlatGenerator : IWorldGenerator
{
    public void GenerateChunk(IChunkData chunk, long seed)
    {
        // Simple flat world: 1 layer of bedrock, 3 layers of dirt, 1 layer of grass
        for (int x = 0; x < 16; x++)
        {
            for (int z = 0; z < 16; z++)
            {
                chunk.SetBlock(x, 0, z, "sharpcraft:bedrock");
                
                for (int y = 1; y < 4; y++)
                {
                    chunk.SetBlock(x, y, z, "sharpcraft:dirt");
                }
                
                chunk.SetBlock(x, 4, z, "sharpcraft:grass");
            }
        }
    }
}

public class MyMod : IMod
{
    private readonly ISharpCraftSdk _sdk;

    public MyMod(ISharpCraftSdk sdk) => _sdk = sdk;

    public ModManifest Manifest => new ModManifest(
        Id: "my_biome_mod",
        Name: "Super Flat World",
        Author: "Developer",
        Version: "1.0.0",
        Dependencies: Array.Empty<string>(),
        Capabilities: Array.Empty<string>()
    );

    public void OnEnable()
    {
        // Register the generator
        _sdk.World.Register("my_biome_mod:super_flat", new SuperFlatGenerator());
    }

    public void OnDisable() { }
}
```

---

## Tier 2: Scripted Mod (.csx)
Tier 2 scripts are perfect for experimenting with generation logic.

### 1. Write the Script (`scripts/main.csx`)
You can define a class that implements the interface directly in your script.

```csharp
using SharpCraft.Sdk.World;

public class DesertGenerator : IWorldGenerator
{
    public void GenerateChunk(IChunkData chunk, long seed)
    {
        // Use the seed for deterministic randomness if needed
        // var random = new Random((int)seed + chunk.X * 31 + chunk.Z);

        for (int x = 0; x < 16; x++)
        {
            for (int z = 0; z < 16; z++)
            {
                // Simple desert: bedrock + sand
                chunk.SetBlock(x, 0, z, "sharpcraft:bedrock");
                
                for (int y = 1; y < 5; y++)
                {
                    chunk.SetBlock(x, y, z, "sharpcraft:sand");
                }
            }
        }
    }
}

// Register via the global Sdk object
Sdk.World.Register("script_mod:desert", new DesertGenerator());

Console.WriteLine("Desert Generator registered!");
```

---

## Best Practices

### 1. Performance
The `GenerateChunk` method is called frequently on background threads. Avoid:
- Allocating large arrays or objects inside the loops.
- Performing synchronous I/O or network requests.
- Using heavy reflection.

### 2. Using Noise
For procedural terrain, use a noise library (like Simplex or Perlin noise). Ensure you initialize your noise generators using the provided `seed`.

### 3. Block IDs
Always use namespaced IDs for `SetBlock`. Built-in blocks use the `sharpcraft` namespace:
- `sharpcraft:air`
- `sharpcraft:stone`
- `sharpcraft:dirt`
- `sharpcraft:grass`
- `sharpcraft:sand`
- `sharpcraft:water`
- `sharpcraft:bedrock`
