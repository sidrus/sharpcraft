namespace SharpCraft.Engine.Rendering;

public interface IRenderer : IDisposable
{
    public void Render(IWorld world, RenderContext context);
}