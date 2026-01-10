
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SharpCraft.Core;
using SharpCraft.Game;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Steamworks;

// --- 1. Setup Logging ---
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

logger.LogInformation("SharpCraft starting...");
logger.LogInformation("Process Architecture: {Arch}", RuntimeInformation.ProcessArchitecture);
logger.LogInformation("Runtime Directory: {Dir}", AppContext.BaseDirectory);

// --- 2. Steam Integration ---
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

// --- 3. World Generation ---
logger.LogInformation("Generating world...");
var world = new World(seed: 42);
await world.GenerateAsync(32, System.Numerics.Vector3.Zero);
logger.LogInformation("World generation complete");

// --- 4. Window & Game Initialization ---
var opts = WindowOptions.Default with
{
    Size = new Vector2D<int>(2560, 1080),
    Title = "SharpCraft",
    VSync = false,
    UpdatesPerSecond = 0.0,
    FramesPerSecond = 0.0
};

logger.LogInformation("Creating game window...");
using var window = Window.Create(opts);
using var game = new Game(window, world, loggerFactory);

// --- 5. Game Loop ---
logger.LogInformation("Entering game loop...");
game.Run();

// --- 6. Cleanup ---
logger.LogInformation("Shutting down...");
SteamClient.Shutdown();
logger.LogInformation("Shutdown complete");