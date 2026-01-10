using SharpCraft.Core;

namespace SharpCraft.Game.Rendering;

public interface IRenderPipeline : ILifecycle, IDisposable
{
    public void Execute(World world, RenderContext context);
}