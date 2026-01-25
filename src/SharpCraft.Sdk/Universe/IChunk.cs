using System.Numerics;
using SharpCraft.Sdk.Blocks;

namespace SharpCraft.Sdk.Universe;

/// <summary>
/// Represents a chunk in the world for rendering purposes.
/// </summary>
public interface IChunk
{
    /// <summary>
    /// Gets the chunk's X coordinate (in chunk units).
    /// </summary>
    int X { get; }

    /// <summary>
    /// Gets the chunk's Z coordinate (in chunk units).
    /// </summary>
    int Z { get; }

    /// <summary>
    /// Gets the world position of the chunk's origin.
    /// </summary>
    Vector3 WorldPosition { get; }

    /// <summary>
    /// Gets a value indicating whether the chunk needs to be re-meshed.
    /// </summary>
    bool IsDirty { get; }

    /// <summary>
    /// Gets the mesh for opaque blocks.
    /// </summary>
    IChunkMesh OpaqueMesh { get; }

    /// <summary>
    /// Gets the mesh for transparent blocks.
    /// </summary>
    IChunkMesh TransparentMesh { get; }

    /// <summary>
    /// Gets the block at the specified local coordinates.
    /// </summary>
    Block GetBlock(int x, int y, int z);

    /// <summary>
    /// A delegate that resolves UV coordinates for a block type and face direction.
    /// </summary>
    delegate void UvResolver(BlockType type, Direction dir, Span<float> uvs);

    /// <summary>
    /// Generates the visual meshes for the chunk.
    /// </summary>
    void GenerateMesh(IWorld world, UvResolver uvResolver);
}
