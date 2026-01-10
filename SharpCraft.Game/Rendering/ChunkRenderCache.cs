using SharpCraft.Core.WorldGeneration;
using Silk.NET.OpenGL;

namespace SharpCraft.Game.Rendering;

public class ChunkRenderCache(GL gl) : IDisposable
{
    private readonly Dictionary<Chunk, RenderableChunk> _cache = new();

    public RenderableChunk Get(Chunk chunk)
    {
        if (!_cache.TryGetValue(chunk, out var rc))
        {
            rc = new RenderableChunk(gl, chunk);
            _cache.Add(chunk, rc);
        }
        return rc;
    }

    public void Update(Chunk[] activeChunks)
    {
        var activeSet = activeChunks.ToHashSet();
        var toRemove = _cache.Keys.Where(c => !activeSet.Contains(c)).ToList();
        foreach (var c in toRemove) { _cache[c].Dispose(); _cache.Remove(c); }
    }

    public void Dispose() { foreach (var rc in _cache.Values) rc.Dispose(); _cache.Clear(); }
}