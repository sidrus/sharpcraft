using SharpCraft.Core;
using SharpCraft.Game.Controllers;

namespace SharpCraft.Game.UI;

public interface IHud
{
    public string Name { get; }
    public void Update(double deltaTime);
    public void Draw(double deltaTime, World world, LocalPlayerController? player);
}