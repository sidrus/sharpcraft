using SharpCraft.Core;

namespace SharpCraft.Client.Rendering;

public interface IRenderer : IDisposable
{
    public void Render(World world, RenderContext context);
}