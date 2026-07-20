using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SharpCraft.Engine.Rendering;

public partial class ChunkMeshManager(IWorld world, IChunk.UvResolver uvResolver, ILogger<ChunkMeshManager> logger)
{
    private readonly ConcurrentQueue<IChunk> _dirtyChunks = new();
    private readonly ConcurrentDictionary<IChunk, bool> _processingChunks = new();
    private readonly ConcurrentQueue<IChunk> _completedChunks = new();

    public int DirtyChunksCount => _dirtyChunks.Count;
    public int ProcessingChunksCount => _processingChunks.Count;

    public void Enqueue(IChunk chunk)
    {
        if (_processingChunks.TryAdd(chunk, true))
        {
            _dirtyChunks.Enqueue(chunk);
        }
    }

    public void Process()
    {
        while (_dirtyChunks.TryDequeue(out var chunk))
        {
            Task.Run(() =>
            {
                try
                {
                    // Validate chunk is still loaded before meshing
                    if (!IsChunkLoaded(chunk))
                    {
                        return;
                    }

                    chunk.GenerateMesh(world, uvResolver);
                    _completedChunks.Enqueue(chunk);
                }
                catch (Exception e)
                {
                    LogMeshGenerationFailed(chunk, e);
                }
                finally
                {
                    _processingChunks.TryRemove(chunk, out _);
                }
            });
        }
    }

    private bool IsChunkLoaded(IChunk chunk)
    {
        return world.GetLoadedChunks().Contains(chunk);
    }

    public bool TryGetCompleted(out IChunk? chunk)
    {
        return _completedChunks.TryDequeue(out chunk);
    }
}