using SharpCraft.Sdk.Lifecycle;
using SharpCraft.Sdk.Physics;

namespace SharpCraft.Sdk;

public interface IActor : ILifecycle, IComponentProvider
{
    public Transform Transform { get; }

    public string Name { get; }

    public string Id { get; }
}