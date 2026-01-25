namespace SharpCraft.Engine.Rendering;

public interface IRenderPipeline : ILifecycle, IDisposable
{
    public ChunkMeshManager MeshManager { get; }
    public void Execute(IWorld world, RenderContext context);
}