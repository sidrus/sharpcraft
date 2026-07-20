using Microsoft.Extensions.Logging;
using SharpCraft.Client;
using SharpCraft.Engine;
using SharpCraft.Engine.Blocks;
using SharpCraft.Engine.Commands;
using SharpCraft.Engine.Lifecycle;
using SharpCraft.Engine.Messaging;
using SharpCraft.Engine.Rendering.Lighting;
using SharpCraft.Engine.UI;
using SharpCraft.Engine.Universe;
using SharpCraft.Sdk.Resources;
using SharpCraft.Sdk.UI;
using SharpCraft.Sdk.Universe;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Steamworks;
using System.Numerics;
using System.Runtime.InteropServices;

// Setup Logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        })
        .SetMinimumLevel(LogLevel.Debug);
});

var logger = loggerFactory.CreateLogger<Program>();

// SDK Initialization
var assets = new Registry<TextureData>();
var blocks = new BlockRegistry();
var channels = new ChannelManager(loggerFactory);
var commands = new CommandRegistry();
var worldGen = new Registry<IWorldGenerator>();
var huds = new HudRegistry();
var lighting = new LightingSystem();
var graphicsSettings = new GraphicsSettings();
var sdk = new SharpCraftSdk
{
    Assets = assets,
    Blocks = blocks,
    Channels = channels,
    Commands = commands,
    World = worldGen,
    Huds = huds,
    Lighting = lighting,
    GraphicsSettings = graphicsSettings,
};

ProgramLog.Starting(logger);
ProgramLog.ProcessArchitecture(logger, RuntimeInformation.ProcessArchitecture);
ProgramLog.RuntimeDirectory(logger, AppContext.BaseDirectory);

// Load and enable mods
ProgramLog.LoadingMods(logger);
var modsDirectory = Path.Combine(AppContext.BaseDirectory, "mods");

var modLoader = new ModLoader(loggerFactory.CreateLogger<ModLoader>(), sdk);
modLoader.LoadMods(modsDirectory);
if (modLoader.LoadedMods.All(m => m.Manifest.Id != "sharpcraft"))
{
    ProgramLog.NoModsFound(logger);
    return;
}

modLoader.EnableMods();

// Steam Integration
try
{
    ProgramLog.InitializingSteam(logger);
    SteamClient.Init(480);
    ProgramLog.SteamInitialized(logger, SteamClient.Name);
}
catch (Exception e)
{
    ProgramLog.SteamInitFailed(logger, e);
    return;
}

// World Generation
ProgramLog.GeneratingWorld(logger);
const int seed = 42;
var generator = worldGen.Get(new ResourceLocation("sharpcraft", "default"));
var world = new World(generator, seed, blocks);
await world.GenerateAsync(32, Vector3.Zero);
ProgramLog.WorldGenerationComplete(logger);

// Window & Game Initialization
var opts = WindowOptions.Default with
{
    Size = new Vector2D<int>(1920, 1080),
    Title = "SharpCraft",
    VSync = false,
    UpdatesPerSecond = 0.0,
    FramesPerSecond = 0.0
};

ProgramLog.CreatingWindow(logger);
using var window = Window.Create(opts);
using var game = new Game(window, world, loggerFactory, sdk, modLoader.LoadedMods);

// Game Loop
ProgramLog.EnteringGameLoop(logger);
game.Run();

// Cleanup
ProgramLog.ShuttingDown(logger);
SteamClient.Shutdown();
ProgramLog.ShutdownComplete(logger);