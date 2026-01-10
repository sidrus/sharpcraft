using SharpCraft.Core;
using SharpCraft.Game.Controllers;

namespace SharpCraft.Game.UI;

public interface IHud
{
    public void Draw(double deltaTime, World world, LocalPlayerController? player);
}