using Microsoft.Extensions.Logging;

namespace SharpCraft.Engine.Rendering.Chunks;

public partial class ChunkMeshManager
{
    [LoggerMessage(LogLevel.Error, "Mesh generation failed for chunk {chunk}")]
    partial void LogMeshGenerationFailed(IChunk chunk, Exception ex);
}