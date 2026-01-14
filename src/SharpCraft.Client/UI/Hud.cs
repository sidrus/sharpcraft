namespace SharpCraft.Client.UI;

public abstract class Hud : IHud
{
    public abstract string Name { get; }

    public virtual void OnUpdate(double deltaTime)
    {
    }
    
    public abstract void Draw(double deltaTime, HudContext context);
}