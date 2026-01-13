# SharpCraft Modding SDK: Technical Specification

## 1. Vision and Objectives

### 1.1 Overview
The SharpCraft SDK is designed to provide a robust, secure, and performant framework for extending the voxel engine. It provides a standardized interface for both internal feature development and third-party content creation.

### 1.2 Core Objectives
- **Stability**: Provide a versioned, immutable API surface to ensure long-term mod compatibility.
- **Performance**: Facilitate high-performance extensions via Tier 1 access while maintaining system-wide stability.
- **Security**: Implement a strict sandbox for untrusted code to protect user systems and maintain game integrity.
- **Decoupling**: Enforce modularity through a Pub/Sub communication model, preventing direct dependencies between mods.
- **Tooling**: Offer industry-standard diagnostic and development tools to streamline the creation process.

### 1.3 Out of Scope
- Direct access to engine private internals or raw memory management.
- Unrestricted .NET API access for Tier 2 (External) extensions.
- Indefinite execution time or unbounded resource allocation for scripts.

## 2. Security & Trust Model

The SDK enforces a two-tier security model based on the origin and trust level of the extension.

### 2.1 Tier 1: Trusted (Internal)
- **Definition**: Extensions authored by the engine team or verified partners.
- **Format**: Compiled DLL assemblies.
- **Access**: Full SDK API surface and direct engine-level hooks.
- **Execution**: Native performance with minimal overhead.
- **Validation**: Subject to manual code review and engine-level integration tests.

### 2.2 Tier 2: Untrusted (External)
- **Definition**: General third-party mods.
- **Format**: CSharpScript (`.csx`) only.
- **Sandboxing**:
  - **API Whitelisting**: Restricted to a safe subset of .NET and SDK namespaces.
  - **No Dangerous Operations**: Explicitly prohibits IO, Networking, Reflection, and Process spawning.
  - **Resource Quotas**: Strict budgets for CPU time and memory allocation.
  - **Capability Model**: Access to specific features (e.g., UI, persistence) must be declared in the manifest.

### 2.3 Runtime Isolation
- Prohibition of dynamic assembly loading at runtime for Tier 2.
- Enforcement of `CancellationToken` patterns for all long-running script operations.
- Architecture designed for future transition to out-of-process execution if required.

## 3. Mod Distribution & Lifecycle

### 3.1 Package Specification
Mods must be packaged in a structured directory containing:
- **`mod.json` (Manifest)**:
  - Metadata: `id`, `name`, `author`, `version`.
  - Compatibility: `sdkVersionRange`, `engineVersionRange`.
  - Execution: `entrypoints`, `dependencies`.
  - Permissions: `declaredCapabilities`.
- **Directory Structure**:
  - `/assemblies/`: Compiled DLLs (Tier 1 Only).
  - `/scripts/`: `.csx` source files (Tier 2).
  - `/assets/`: Textures, sounds, and models.
  - `/ui/`: Declarative layout definitions (XML/JSON).
  - `/data/`: Configuration and data-driven definitions.
- **Asset Namespacing**: Enforcement of the `namespace:asset_id` pattern for all resources (textures, models, sounds) to prevent collisions.

### 3.2 Lifecycle Management
The SDK Runtime manages the following state transitions:
1. **OnLoad**: Resource discovery and manifest validation.
2. **OnMigrate**: Hook for transforming persisted data from older mod/SDK versions.
3. **OnEnable**: Injection of `IWorldRuntime` and registration of hooks.
4. **OnDisable**: Graceful teardown and unsubscription from events.
5. **OnUnload**: Disposal of resources and assembly/script unloading.
- **Hot Reload**: Support for runtime script updates with state migration (experimental).

### 3.3 Reliability & Diagnostics
- **Exception Isolation**: Crashes in one mod must not terminate the engine or other mods.
- **Structured Logging**: Automated context-aware logging via the SDK logger.
- **Health Reporting**: Runtime monitoring of mod performance and memory usage.

## 4. System Architecture

### 4.1 Design Principles
- **Interface-First**: All SDK interactions shall occur through versioned interfaces.
- **Handle-Based**: References to engine objects must use opaque handles or stable IDs to prevent memory leaks and dangling pointers.
- **Data-Driven**: Logic should be separated from data whenever possible using JSON/XML definitions.

### 4.2 Communication: Pub/Sub Channels
To ensure decoupling, direct mod-to-mod referencing is prohibited.
- **Channel Model**: Mods communicate via named message channels (e.g., `mod://economy/transactions`).
- **Discovery**: Mod-agnostic discovery allows subscribers to react to events without knowledge of the publisher.
- **Lifecycle**: Channels are automatically cleaned up when the providing mod is disabled.

### 4.3 Threading & Concurrency
- **Mutation**: World mutation is restricted to the main simulation thread or coordinated via the Command Queue.
- **Safety**: All SDK APIs must be async-aware and support cancellation tokens.

### 4.4 Persistence
- **Scoped Storage**: Mods are provided with isolated file/database storage.
- **Quota Enforcement**: Tier 2 mods are subject to storage size limits.

### 4.5 Networking & Synchronization
- **State Sync**: Mechanisms for synchronizing custom ECS components and block states across a client-server architecture.
- **Authority Model**: Distinction between "Server-Authoritative" logic and "Client-Predicted" visual effects.
- **Networked Channels**: Extension of the Pub/Sub system to support cross-network message broadcasting (e.g., `mod://economy/sync_balance`).

## 5. Functional Feature Areas

### 5.1 World & Terrain
- **Pipeline**: Access to the multi-stage chunk generation pipeline.
- **Determinism**: Mandatory use of SDK-provided seeded RNG for all generation logic.

### 5.2 Entity Component System (ECS)
- Registration of custom entities and components.
- Abstracted AI behavior trees and spawn rule definitions.

### 5.3 Blocks & Items
- Data-driven definition of block states and item properties.
- Hooks for interaction, placement, and destruction.
- Immutable item instances for thread-safe handling.

### 5.4 Quests & Objectives
- Data-driven quest definitions with event-based objective tracking.
- Per-player persistence for quest state and rewards.

### 5.5 Physics & Interaction
- Query-only access to the physics world for spatial queries (raycasts, overlaps).
- High-level impulse and force application via SDK handles.
- Strict performance limits on physics-related callbacks.

### 5.6 User Interface & HUD
- **HUD Registration**: Ability to register overlays and status bars.
- **Menus**: Support for custom modal dialogues and settings screens.
- **Declarative Layouts**: UI must be defined via XML/JSON with data binding to the Channel system.
- **Tiered Rendering**:
  - **Tier 1**: Direct access to engine UI primitives (ImGui).
  - **Tier 2**: Restricted to a whitelisted library of safe UI components.
- **Input Management**: Centralized focus management to prevent input hijacking.

### 5.7 Audio & Sound
- **Audio Registry**: System for registering new audio clips and "Sound Events" with variation and pitch randomization support.
- **Spatial Audio**: SDK-managed handles for attaching audio to entities or world coordinates.
- **Buses & Mixing**: Ability to output to specific audio channels (SFX, Music, Ambient) for proper volume control.

### 5.8 Input & Control
- **Action Bindings**: Registration of abstract actions (e.g., "Jump", "SpecialAbility") that users can rebind in the game settings.
- **Input Contexts**: Ability to enable/disable mod inputs based on game state (e.g., in menu, in vehicle).

### 5.9 Localization & Internationalization
- **String Tables**: Support for JSON or `.lang` files in the mod package.
- **Localization API**: Method for requesting localized strings with dynamic token support (e.g., `translate("mod.kill_count", count)`).

### 5.10 Developer Tools & Console
- **Command Registry**: Formal registration of "slash commands" (e.g., `/set_mana 100`) for debugging or admin use.
- **In-Game Debugger**: Hooks for drawing diagnostic overlays or real-time graphs on the `DeveloperHud`.

## 6. Implementation Roadmap

### 6.1 Phase 1: Minimum Viable SDK (MVS)
1. Core Block and Item registration with asset namespacing.
2. Event hook system for gameplay triggers.
3. World Command Queue for thread-safe mutation.
4. Tier 2 Script Sandbox with basic resource budgeting.
5. Pub/Sub Channel system for cross-mod communication.
6. Tier 1 HUD element registration and basic UI library.
7. Basic Command Registry for debugging.

## 7. Appendices

### 7.1 References
- [Roslyn Scripting API](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/scripting)
- [Semantic Versioning 2.0.0](https://semver.org/)
- [DocFX Documentation Generator](https://dotnet.github.io/docfx/)
