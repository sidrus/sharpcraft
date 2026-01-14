using SharpCraft.Core;
using SharpCraft.Client.Controllers;

namespace SharpCraft.Client.UI;

public interface IHud : ILifecycle
{
    public string Name { get; }
    public void Draw(double deltaTime, HudContext context);
}