using SharpCraft.Core;
using SharpCraft.Game.Controllers;

namespace SharpCraft.Game.UI;

public interface IHud : ILifecycle
{
    public string Name { get; }
    public void Draw(double deltaTime, HudContext context);
}