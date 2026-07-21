using System.Numerics;

namespace SharpCraft.Client.Gameplay;

/// <summary>
/// The rules for placing a torch: whether the player's current footing allows it, and where the
/// torch's base sits. Pure so the placement logic is testable without a renderer or lighting system.
/// </summary>
public static class TorchPlacement
{
    /// <summary>
    /// Gets whether a torch may be placed: only on a real surface, not mid-swim and not underwater.
    /// </summary>
    public static bool CanPlace(bool blockBelowSolid, bool isSwimming, bool isUnderwater)
    {
        return blockBelowSolid && !isSwimming && !isUnderwater;
    }

    /// <summary>
    /// Gets the torch base position, centered on the supporting block's column and resting on its top face.
    /// </summary>
    public static Vector3 BasePosition(Vector3 playerPosition)
    {
        return new Vector3(
            MathF.Floor(playerPosition.X) + 0.5f,
            MathF.Floor(playerPosition.Y),
            MathF.Floor(playerPosition.Z) + 0.5f);
    }
}