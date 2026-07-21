namespace SharpCraft.Engine.Rendering.Chunks;

public class ChunkRenderCache(GL gl) : IDisposable
{
    private int _currentGeneration;
    private readonly Dictionary<IChunk, (RenderableChunk RenderChunk, int Generation)> _cache = new();
    private readonly List<IChunk> _toRemove = [];

    public RenderableChunk Get(IChunk chunk)
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

    public void Update(IEnumerable<IChunk> activeChunks)
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
        if (_disposed)
        {
            return;
        }

        foreach (var entry in _cache.Values)
        {
            entry.RenderChunk.Dispose();
        }
        _cache.Clear();
        _disposed = true;
    }

    private bool _disposed;
}