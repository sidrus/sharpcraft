using SharpCraft.Engine.Universe;
using SharpCraft.Sdk.Lifecycle;

namespace SharpCraft.Client.Rendering;

public interface IRenderPipeline : ILifecycle, IDisposable
{
    public ChunkMeshManager MeshManager { get; }
    public void Execute(World world, RenderContext context);
}