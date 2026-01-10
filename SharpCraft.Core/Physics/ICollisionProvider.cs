using SharpCraft.Core.Blocks;

namespace SharpCraft.Core.Physics;

public interface ICollisionProvider
{
    public Block GetBlock(int worldX, int worldY, int worldZ);
}