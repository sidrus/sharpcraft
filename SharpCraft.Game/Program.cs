
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SharpCraft.Core;
using SharpCraft.Core.Numerics;
using SharpCraft.Game;
using Silk.NET.Maths;
using Silk.NET.Windowing;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Debug); // Control how much you see
});

var logger = loggerFactory.CreateLogger<Program>();

logger.LogInformation("Generating world...");
var world = new World(seed: 42);
world.Generate(8);

var opts = WindowOptions.Default with
{
    Size = new Vector2D<int>(2560, 1080),
    Title = "SharpCraft"
};

using var window = Window.Create(opts);
using var game = new Game(window, world, loggerFactory);

game.Run();