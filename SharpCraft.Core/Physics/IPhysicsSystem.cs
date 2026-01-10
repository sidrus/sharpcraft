using System.Numerics;

namespace SharpCraft.Core.Physics;

public interface IPhysicsSystem
{
    public Vector3 MoveAndResolve(Vector3 position, Vector3 velocity, Vector3 size);
}