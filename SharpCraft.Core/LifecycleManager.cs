using System.Collections.Generic;

namespace SharpCraft.Core;

public class LifecycleManager
{
    private readonly List<ILifecycle> _objects = new();
    private readonly List<ILifecycle> _toAdd = new();
    private readonly List<ILifecycle> _toRemove = new();
    private bool _isIterating;

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
