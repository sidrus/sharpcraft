# SharpCraft Documentation

Welcome to the SharpCraft documentation! This folder contains guides, tutorials, and technical specifications for developing with and extending SharpCraft.

## Contents

### Guides

Step-by-step tutorials for extending SharpCraft using the SDK:

| Guide | Description |
| :--- | :--- |
| [Adding a Block](adding-a-block.md) | Learn how to create and register custom blocks using both Tier 1 (compiled DLL) and Tier 2 (scripted .csx) approaches. |
| [Adding a Terrain Generator](adding-a-terrain-generator.md) | Create custom terrain generators to populate chunks with blocks during world generation. |

### Architecture

Technical specifications and architectural documentation:

| Document | Description |
| :--- | :--- |
| [SDK Specifications](architecture/sdk-specs.md) | Comprehensive technical specification for the SharpCraft Modding SDK, including security model, mod lifecycle, and API design. |

## Getting Started

If you're new to SharpCraft modding, we recommend starting with:

1. **[SDK Specifications](architecture/sdk-specs.md)** - Understand the overall architecture and security model.
2. **[Adding a Block](adding-a-block.md)** - Create your first custom content.
3. **[Adding a Terrain Generator](adding-a-terrain-generator.md)** - Learn about world generation.

## Mod Tiers

SharpCraft supports two tiers of mods:

- **Tier 1 (Trusted)**: Compiled C# DLL assemblies with full SDK API access. Best for complex logic and high-performance features.
- **Tier 2 (Untrusted)**: CSharpScript (`.csx`) files running in a sandboxed environment. Perfect for quick content additions and experimentation.

## Contributing

When adding new documentation:

1. Place guides and tutorials in the root `docs/` folder.
2. Place architectural and technical specifications in the `docs/architecture/` folder.
3. Update this README to include links to new documents.
