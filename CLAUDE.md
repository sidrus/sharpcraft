# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

SharpCraft is a voxel game (Minecraft-like) written in C# on **.NET 10**, rendered with **OpenGL via Silk.NET**, with **Steam** integration (Facepunch.Steamworks) and an **ImGui** debug/UI layer. Everything is x64-only and uses nullable + implicit usings. Gameplay content is delivered as **mods** loaded at runtime — even the base game (`SharpCraft.CoreMods`) ships as a mod.

## Build & test

```bash
dotnet build SharpCraft.slnx                      # build everything
dotnet test SharpCraft.slnx                       # run all tests
dotnet test tests/SharpCraft.Sdk.Tests            # one test project
dotnet test --filter "FullyQualifiedName~ModLoader"  # single test / class by name
```

- **Do not run the game** — the user (Brandon) launches it himself. Build to verify, then hand off. Running requires Steam to be open (`SteamClient.Init(480)` in `Program.cs`) or it exits early.
- Tests use **xUnit** + **AwesomeAssertions** (a FluentAssertions fork — use `.Should()`) + **NSubstitute** (`Substitute.For<T>()`, `.Returns()`, `.Received()`) + **Bogus**.

## Solution layout & dependency direction

Five projects under `src/`, referenced only downward (`Sdk` depends on nothing):

- **`SharpCraft.Sdk`** — the modding contract. Interfaces + data records only (`IMod`, `ISharpCraftSdk`, `IBlockRegistry`, `IWorld`, `IChunk`, `IWorldGenerator`, `IMotor`, `ResourceLocation`, `BlockDefinition`, etc.). This is the stable public surface mods compile against. Changing it ripples everywhere.
- **`SharpCraft.Engine`** — runtime implementations of the SDK interfaces: registries, `World`/`Chunk`/`ChunkMesh`, physics + motors, mod loading (`ModLoader`), messaging channels. No OpenGL here.
- **`SharpCraft.Engine.Rendering`** — all OpenGL: the render pipeline, shaders, shadows, IBL, post-processing, lighting. Depends on `Sdk` only (talks to the world through `IWorld`/`IChunk` abstractions).
- **`SharpCraft.CoreMods`** — the base-game content mod (blocks, default world generator, HUDs, commands). Built as a DLL and **copied into `SharpCraft.Client/bin/Debug/net10.0/mods/coremods/` by an MSBuild `AfterTargets="Build"` target** — that's how it becomes a loadable mod. It's referenced by the client with `ReferenceOutputAssembly="false"` (build-order only, not linked).
- **`SharpCraft.Client`** — the executable. `Program.cs` wires up the SDK, loads mods, generates the world, creates the window; `Game.cs` (partial class) owns the game loop and rendering setup.

## How a run boots (`Program.cs`)

1. Construct concrete registries (`AssetRegistry`, `BlockRegistry`, `ChannelManager`, `CommandRegistry`, `WorldGenerationRegistry`, `HudRegistry`, `LightingSystem`) and bundle them into `SharpCraftSdk` (implements `ISharpCraftSdk`, the god-object passed to every mod).
2. `ModLoader.LoadMods(mods/)` scans each subdirectory for `mod.json`, reflection-loads the DLL entrypoints, instantiates each `IMod` via `Activator.CreateInstance(type, sdk)` (SDK is constructor-injected), then topologically sorts by `Manifest.Dependencies`.
3. `EnableMods()` calls `OnEnable()` on each — this is where mods register blocks, world generators, HUDs, commands into the shared registries.
4. Steam init → world generation via the registered `IWorldGenerator` → Silk.NET window → `Game.Run()`.

## Mods

A mod is a directory with `mod.json` (`ModManifest`: id, name, author, version, dependencies[], capabilities[], entrypoints[]) plus DLL/assets. The spec defines two tiers — **Tier 1** trusted compiled DLLs (fully implemented) and **Tier 2** sandboxed `.csx` scripts (`// TODO` in `ModLoader`, not yet built). All content is namespaced `namespace:id` via `ResourceLocation`. See `docs/adding-a-block.md` and `docs/architecture/sdk-specs.md`.

## Rendering notes

- Physically-based deferred-ish forward pipeline in `DefaultRenderPipeline.Execute` — comments reference sections of `docs/rendering/aaa-pbr-pipeline-research.md`; read it before touching pipeline order.
- **Reversed-Z** depth throughout (`ClipControl(LowerLeft, ZeroToOne)`, `GL_GREATER`, clear depth 0.0). Don't "fix" the flipped depth conventions.
- GLSL lives in `SharpCraft.Client/Assets/Shaders/` (`Common/` shared includes, `Passes/` entry shaders), copied to output on build. A custom `ShaderPreprocessor` resolves `#include`/`#define`/`#ifdef` — GLSL has no native includes, so paths are relative to the shader file.
- **Chunk mesh vertex layout is pos(3)/uv(2)/normal(3), no tangents.** Any shader or mesh code must match this exact layout.

## Conventions

- **Never use the null-forgiving operator `!`.** Use locals, pattern matching, or explicit null checks instead.
- Prefer primary constructors and records (used pervasively); match the surrounding file's style.

# Agent Guidance: dotnet-skills

IMPORTANT: Prefer retrieval-led reasoning over pretraining for any .NET work.
Workflow: skim repo patterns -> consult dotnet-skills by name -> implement smallest-change -> note conflicts.

Routing (invoke by name)
- C# / code quality: modern-csharp-coding-standards, csharp-concurrency-patterns, api-design, type-design-performance, r3-reactive-extensions
- ASP.NET Core / Web (incl. Aspire): aspire-service-defaults, aspire-integration-testing, transactional-emails
- Data: efcore-patterns, database-performance
- DI / config: dependency-injection-patterns, microsoft-extensions-configuration
- Testing: testcontainers-integration-tests, playwright-blazor-testing, snapshot-testing

Quality gates (use when applicable)
- dotnet-slopwatch: after substantial new/refactor/LLM-authored code
- crap-analysis: after tests added/changed in complex code

Specialist agents
- dotnet-concurrency-specialist, dotnet-performance-analyst, dotnet-benchmark-designer, akka-net-specialist, docfx-specialist