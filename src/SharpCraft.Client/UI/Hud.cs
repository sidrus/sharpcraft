namespace SharpCraft.Client.UI;

public abstract class Hud : IHud
{
    public abstract string Name { get; }

    public abstract void Draw(double deltaTime, HudContext context);
}