using SharpCraft.Core.WorldGeneration;
using Silk.NET.OpenGL;

namespace SharpCraft.Game.Rendering;

public class ChunkRenderCache(GL gl) : IDisposable
{
    private int _currentGeneration;
    private readonly Dictionary<Chunk, (RenderableChunk RenderChunk, int Generation)> _cache = new();
    private readonly List<Chunk> _toRemove = new();

    public RenderableChunk Get(Chunk chunk)
    {
        if (!_cache.TryGetValue(chunk, out var entry))
        {
            var rc = new RenderableChunk(gl, chunk);
            entry = (rc, _currentGeneration);
            _cache.Add(chunk, entry);
        }
        else
        {
            entry.Generation = _currentGeneration;
            _cache[chunk] = entry;
        }
        return entry.RenderChunk;
    }

    public void Update(IEnumerable<Chunk> activeChunks)
    {
        _currentGeneration++;
        
        // Mark active chunks with current generation
        foreach (var chunk in activeChunks)
        {
            if (_cache.TryGetValue(chunk, out var entry))
            {
                entry.Generation = _currentGeneration;
                _cache[chunk] = entry;
            }
            else
            {
                // If it's not in cache, Get will add it later during rendering
                // or we can add it here if needed. 
                // TerrainRenderer calls Get(chunk) for every active chunk anyway.
            }
        }

        // Identify and remove stale chunks
        _toRemove.Clear();
        foreach (var (chunk, entry) in _cache)
        {
            if (entry.Generation < _currentGeneration)
            {
                _toRemove.Add(chunk);
            }
        }

        foreach (var c in _toRemove)
        {
            _cache[c].RenderChunk.Dispose();
            _cache.Remove(c);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                foreach (var entry in _cache.Values)
                {
                    entry.RenderChunk.Dispose();
                }
                _cache.Clear();
            }

            _disposed = true;
        }
    }

    private bool _disposed;
}