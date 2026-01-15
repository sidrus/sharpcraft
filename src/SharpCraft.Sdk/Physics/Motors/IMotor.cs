namespace SharpCraft.Sdk.Physics.Motors;

public interface IMotor
{
    void ApplyForces(IPhysicsEntity entity, MovementIntent intent, float deltaTime);
}