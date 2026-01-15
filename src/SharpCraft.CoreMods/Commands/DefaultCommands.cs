using System.Numerics;
using SharpCraft.Sdk;
using SharpCraft.Sdk.Commands;
using SharpCraft.Sdk.UI;

namespace SharpCraft.CoreMods.Commands;

internal static class DefaultCommands
{
    public static void Register(ISharpCraftSdk sdk)
    {
        sdk.Commands.RegisterCommand("tp", ctx => HandleTeleport(sdk, ctx));
        sdk.Commands.RegisterCommand("teleport", ctx => HandleTeleport(sdk, ctx));
        sdk.Commands.RegisterCommand("give", ctx => HandleGive(sdk, ctx));
        sdk.Commands.RegisterCommand("gamemode", ctx => HandleGameMode(sdk, ctx));
    }

    private static void HandleTeleport(ISharpCraftSdk sdk, CommandContext ctx)
    {
        if (ctx.Player == null) return;

        if (ctx.Args.Length != 3)
        {
            SendChat(sdk, "Usage: /teleport <x> <y> <z>", new Vector4(1, 0.3f, 0.3f, 1));
            return;
        }

        if (float.TryParse(ctx.Args[0], out var x) &&
            float.TryParse(ctx.Args[1], out var y) &&
            float.TryParse(ctx.Args[2], out var z))
        {
            ctx.Player.Entity.SetPosition(new Vector3(x, y, z));
            SendChat(sdk, $"Teleported to {x}, {y}, {z}", new Vector4(0.3f, 1, 0.3f, 1));
        }
        else
        {
            SendChat(sdk, "Invalid coordinates", new Vector4(1, 0.3f, 0.3f, 1));
        }
    }

    private static void HandleGive(ISharpCraftSdk sdk, CommandContext ctx)
    {
        SendChat(sdk, "The /give command is not yet implemented.", new Vector4(1, 1, 0, 1));
    }

    private static void HandleGameMode(ISharpCraftSdk sdk, CommandContext ctx)
    {
        SendChat(sdk, "The /gamemode command is not yet implemented.", new Vector4(1, 1, 0, 1));
    }

    private static void SendChat(ISharpCraftSdk sdk, string text, Vector4 color)
    {
        sdk.Channels.GetChannel("chat").Publish(new ChatMessage(text, color));
    }
}
