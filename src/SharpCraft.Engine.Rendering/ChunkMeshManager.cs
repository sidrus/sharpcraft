using System.Collections.Concurrent;

namespace SharpCraft.Engine.Rendering;

public class ChunkMeshManager(IWorld world, IChunk.UvResolver uvResolver)
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
                    chunk.GenerateMesh(world, uvResolver);
                    _completedChunks.Enqueue(chunk);
                }
                finally
                {
                    _processingChunks.TryRemove(chunk, out _);
                }
            });
        }
    }

    public bool TryGetCompleted(out IChunk? chunk)
    {
        return _completedChunks.TryDequeue(out chunk);
    }
}