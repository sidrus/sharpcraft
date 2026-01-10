using SharpCraft.Core;
using SharpCraft.Game.Controllers;

namespace SharpCraft.Game.UI;

public abstract class Hud : IHud
{
    public abstract string Name { get; }

    public abstract void Draw(double deltaTime, World world, LocalPlayerController? player);
}