using System.Collections.Concurrent;
using SharpCraft.Engine.Universe;

namespace SharpCraft.Client.Rendering;

public class ChunkMeshManager(World world, Chunk.UvResolver uvResolver)
{
    private readonly ConcurrentQueue<Chunk> _dirtyChunks = new();
    private readonly ConcurrentDictionary<Chunk, bool> _processingChunks = new();
    private readonly ConcurrentQueue<Chunk> _completedChunks = new();

    public int DirtyChunksCount => _dirtyChunks.Count;
    public int ProcessingChunksCount => _processingChunks.Count;

    public void Enqueue(Chunk chunk)
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

    public bool TryGetCompleted(out Chunk? chunk)
    {
        return _completedChunks.TryDequeue(out chunk);
    }
}