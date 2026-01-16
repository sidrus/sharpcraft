
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SharpCraft.Client;
using SharpCraft.Client.Rendering.Lighting;
using SharpCraft.Engine;
using SharpCraft.Engine.Assets;
using SharpCraft.Engine.Blocks;
using SharpCraft.Engine.Commands;
using SharpCraft.Engine.Lifecycle;
using SharpCraft.Engine.Messaging;
using SharpCraft.Engine.UI;
using SharpCraft.Engine.Universe;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Steamworks;

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
var assets = new AssetRegistry();
var blocks = new BlockRegistry();
var channels = new ChannelManager();
var commands = new CommandRegistry();
var worldGen = new WorldGenerationRegistry();
var huds = new HudRegistry();
var lighting = new LightingSystem();
var sdk = new SharpCraftSdk(assets, blocks, channels, commands, worldGen, huds, lighting);

logger.LogInformation("SharpCraft starting...");
logger.LogInformation("Process Architecture: {Arch}", RuntimeInformation.ProcessArchitecture);
logger.LogInformation("Runtime Directory: {Dir}", AppContext.BaseDirectory);

// Load and enable mods
logger.LogInformation("Loading mods...");
var modsDirectory = Path.Combine(AppContext.BaseDirectory, "mods");

var modLoader = new ModLoader(loggerFactory.CreateLogger<ModLoader>(), sdk);
modLoader.LoadMods(modsDirectory);
if (modLoader.LoadedMods.All(m => m.Manifest.Id != "sharpcraft"))
{
    logger.LogCritical("No mods found. Ensure SharpCraft is installed correctly");
    return;
}

modLoader.EnableMods();

// Steam Integration
try
{
    logger.LogDebug("Initializing Steam...");
    SteamClient.Init(480);
    logger.LogInformation("Steam initialized: {Name}", SteamClient.Name);
}
catch (Exception e)
{
    logger.LogError(e, "Could not initialize Steam. Ensure Steam is running");
    return;
}

// World Generation
logger.LogInformation("Generating world...");
const int seed = 42;
var generator = worldGen.Get(new SharpCraft.Sdk.Resources.ResourceLocation("sharpcraft", "default"));
var world = new World(generator, seed, blocks);
await world.GenerateAsync(32, System.Numerics.Vector3.Zero);
logger.LogInformation("World generation complete");

// Window & Game Initialization
var opts = WindowOptions.Default with
{
    Size = new Vector2D<int>(1920, 1080),
    Title = "SharpCraft",
    VSync = false,
    UpdatesPerSecond = 0.0,
    FramesPerSecond = 0.0
};

logger.LogInformation("Creating game window...");
using var window = Window.Create(opts);
using var game = new Game(window, world, loggerFactory, sdk, modLoader.LoadedMods);

// Game Loop
logger.LogInformation("Entering game loop...");
game.Run();

// Cleanup
logger.LogInformation("Shutting down...");
SteamClient.Shutdown();
logger.LogInformation("Shutdown complete");