using SharpCraft.Sdk.Lifecycle;

namespace SharpCraft.Engine.Lifecycle;

/// <summary>
/// Manages the lifecycle of multiple objects, handling registration, updates, and destruction.
/// </summary>
public sealed class LifecycleManager
{
    private readonly List<ILifecycle> _objects = [];
    private readonly List<ILifecycle> _toAdd = [];
    private readonly List<ILifecycle> _toRemove = [];
    private bool _isIterating;

    /// <summary>
    /// Registers an object to be managed.
    /// </summary>
    /// <param name="obj">The object to register.</param>
    public void Register(ILifecycle obj)
    {
        if (_isIterating)
        {
            _toAdd.Add(obj);
        }
        else
        {
            _objects.Add(obj);
            obj.OnAwake();
        }
    }

    /// <summary>
    /// Unregisters an object from the manager.
    /// </summary>
    /// <param name="obj">The object to unregister.</param>
    public void Unregister(ILifecycle obj)
    {
        if (_isIterating)
        {
            _toRemove.Add(obj);
        }
        else
        {
            if (_objects.Remove(obj))
            {
                obj.OnDestroy();
            }
        }
    }

    /// <summary>
    /// Starts the lifecycle for all registered objects.
    /// </summary>
    public void Start()
    {
        ProcessPending();
        _isIterating = true;
        foreach (var obj in _objects)
        {
            obj.OnStart();
        }
        _isIterating = false;
        ProcessPending();
    }

    /// <summary>
    /// Updates all registered objects.
    /// </summary>
    /// <param name="deltaTime">The time since the last update.</param>
    public void Update(double deltaTime)
    {
        _isIterating = true;
        foreach (var obj in _objects)
        {
            obj.OnUpdate(deltaTime);
        }
        _isIterating = false;
        ProcessPending();
    }

    /// <summary>
    /// Performs a fixed update on all registered objects.
    /// </summary>
    /// <param name="fixedDeltaTime">The fixed time step.</param>
    public void FixedUpdate(double fixedDeltaTime)
    {
        _isIterating = true;
        foreach (var obj in _objects)
        {
            obj.OnFixedUpdate(fixedDeltaTime);
        }
        _isIterating = false;
        ProcessPending();
    }

    /// <summary>
    /// Renders all registered objects.
    /// </summary>
    /// <param name="deltaTime">The time since the last render.</param>
    public void Render(double deltaTime)
    {
        _isIterating = true;
        foreach (var obj in _objects)
        {
            obj.OnRender(deltaTime);
        }
        _isIterating = false;
        ProcessPending();
    }

    /// <summary>
    /// Destroys all managed objects and clears the manager.
    /// </summary>
    public void Destroy()
    {
        _isIterating = true;
        foreach (var obj in _objects)
        {
            obj.OnDestroy();
        }
        _objects.Clear();
        _toAdd.Clear();
        _toRemove.Clear();
        _isIterating = false;
    }

    private void ProcessPending()
    {
        foreach (var obj in _toAdd)
        {
            _objects.Add(obj);
            obj.OnAwake();
            obj.OnStart();
        }
        _toAdd.Clear();

        foreach (var obj in _toRemove.Where(obj => _objects.Remove(obj)))
        {
            obj.OnDestroy();
        }
        _toRemove.Clear();
    }
}
