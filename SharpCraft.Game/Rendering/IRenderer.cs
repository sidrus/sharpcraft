using SharpCraft.Core;

namespace SharpCraft.Game.Rendering;

public interface IRenderer : IDisposable
{
    public void Render(World world, RenderContext context);
}