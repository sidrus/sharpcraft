using SharpCraft.Sdk.Lifecycle;

namespace SharpCraft.Engine.Lifecycle;

/// <summary>
/// Manages the lifecycle of game objects, ensuring they are updated and rendered in order.
/// </summary>
public sealed class LifecycleManager
{
    private readonly List<ILifecycle> _objects = [];
    private readonly List<ILifecycle> _toAdd = [];
    private readonly List<ILifecycle> _toRemove = [];

    /// <summary>
    /// Registers an object to be managed by the LifecycleManager.
    /// </summary>
    /// <param name="obj">The object to register.</param>
    public void Register(ILifecycle obj)
    {
        _toAdd.Add(obj);
    }

    /// <summary>
    /// Unregisters an object from the LifecycleManager.
    /// </summary>
    /// <param name="obj">The object to unregister.</param>
    public void Unregister(ILifecycle obj)
    {
        _toRemove.Add(obj);
    }

    /// <summary>
    /// Processes additions and removals of objects.
    /// </summary>
    public void Flush()
    {
        foreach (var obj in _toAdd)
        {
            _objects.Add(obj);
            obj.OnAwake();
            obj.OnStart();
        }
        _toAdd.Clear();

        foreach (var obj in _toRemove)
        {
            obj.OnDestroy();
            _objects.Remove(obj);
        }
        _toRemove.Clear();
    }

    /// <summary>
    /// Updates all managed objects.
    /// </summary>
    /// <param name="deltaTime">The time since the last update.</param>
    public void Update(double deltaTime)
    {
        Flush();
        foreach (var obj in _objects)
        {
            obj.OnUpdate(deltaTime);
        }
    }

    /// <summary>
    /// Updates all managed objects at a fixed interval.
    /// </summary>
    /// <param name="fixedDeltaTime">The fixed time interval.</param>
    public void FixedUpdate(double fixedDeltaTime)
    {
        foreach (var obj in _objects)
        {
            obj.OnFixedUpdate(fixedDeltaTime);
        }
    }

    /// <summary>
    /// Renders all managed objects.
    /// </summary>
    /// <param name="deltaTime">The time since the last frame.</param>
    public void Render(double deltaTime)
    {
        foreach (var obj in _objects)
        {
            obj.OnRender(deltaTime);
        }
    }
}
