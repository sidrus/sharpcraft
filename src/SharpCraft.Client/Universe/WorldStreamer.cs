using SharpCraft.Engine.Universe;
using SharpCraft.Sdk.Numerics;
using System.Numerics;

namespace SharpCraft.Client.Universe;

/// <summary>
/// Drives chunk streaming as the player moves: when the player crosses into a new chunk and no
/// generation is already in flight, it triggers the supplied async generate/unload operation. The
/// operation is injected so the crossing/in-flight decision is testable without a live world.
/// </summary>
public sealed class WorldStreamer(Func<int, Vector3, Task> generate)
{
    private Vector2<int>? _lastChunk;
    private Task? _task;

    /// <summary>
    /// Updates streaming for the player's current position, starting a new generation pass if the
    /// player has entered a different chunk and the previous pass has finished.
    /// </summary>
    public void Update(Vector3 playerPosition, int renderDistance)
    {
        var chunk = new Vector2<int>(ChunkCoords.ToChunk(playerPosition.X), ChunkCoords.ToChunk(playerPosition.Z));

        var canStartNew = _task == null || (_task.IsCompleted && !_task.IsFaulted && !_task.IsCanceled);
        if ((_lastChunk == null || chunk != _lastChunk.Value) && canStartNew)
        {
            _lastChunk = chunk;
            _task = generate(renderDistance, playerPosition);
        }
    }

    /// <summary>
    /// Forces the next <see cref="Update"/> to regenerate even if the player has not moved chunks,
    /// e.g. after the render distance changes.
    /// </summary>
    public void ForceRefresh()
    {
        _lastChunk = null;
    }
}