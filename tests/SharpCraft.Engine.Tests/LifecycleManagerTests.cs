using AwesomeAssertions;
using SharpCraft.Engine.Lifecycle;
using SharpCraft.Sdk.Lifecycle;

namespace SharpCraft.Engine.Tests;

public class LifecycleManagerTests
{
    private class FakeLifecycle : ILifecycle
    {
        public int AwakeCount { get; private set; }
        public int StartCount { get; private set; }
        public int UpdateCount { get; private set; }
        public int FixedUpdateCount { get; private set; }
        public int RenderCount { get; private set; }
        public int DestroyCount { get; private set; }

        public Action<LifecycleManager>? OnAwakeAction { get; set; }
        public Action<LifecycleManager>? OnStartAction { get; set; }
        public Action<LifecycleManager>? OnUpdateAction { get; set; }
        public Action<LifecycleManager>? OnFixedUpdateAction { get; set; }
        public Action<LifecycleManager>? OnRenderAction { get; set; }
        public Action<LifecycleManager>? OnDestroyAction { get; set; }

        public void OnAwake()
        {
            AwakeCount++;
            OnAwakeAction?.Invoke(null!);
        }

        public void OnStart()
        {
            StartCount++;
            OnStartAction?.Invoke(null!);
        }

        public void OnUpdate(double deltaTime)
        {
            UpdateCount++;
            OnUpdateAction?.Invoke(null!);
        }

        public void OnFixedUpdate(double fixedDeltaTime)
        {
            FixedUpdateCount++;
            OnFixedUpdateAction?.Invoke(null!);
        }

        public void OnRender(double deltaTime)
        {
            RenderCount++;
            OnRenderAction?.Invoke(null!);
        }

        public void OnDestroy()
        {
            DestroyCount++;
            OnDestroyAction?.Invoke(null!);
        }
    }

    [Fact]
    public void Register_ShouldCallOnAwake_WhenNotIterating()
    {
        var manager = new LifecycleManager();
        var obj = new FakeLifecycle();

        manager.Register(obj);

        obj.AwakeCount.Should().Be(1);
    }

    [Fact]
    public void Update_ShouldCallOnUpdate_OnRegisteredObjects()
    {
        var manager = new LifecycleManager();
        var obj = new FakeLifecycle();
        manager.Register(obj);

        manager.Update(0.16);

        obj.UpdateCount.Should().Be(1);
    }

    [Fact]
    public void Register_DuringIteration_ShouldDeferRegistrationUntilAfterIteration()
    {
        var manager = new LifecycleManager();
        var obj1 = new FakeLifecycle();
        var obj2 = new FakeLifecycle();
        manager.Register(obj1);
        obj1.OnUpdateAction = _ => manager.Register(obj2);

        manager.Update(0.16);

        obj2.AwakeCount.Should().Be(1); // ProcessPending is called after loop
        obj2.UpdateCount.Should().Be(0); // Should not have updated in same frame
    }

    [Fact]
    public void Unregister_ShouldCallOnDestroy_WhenNotIterating()
    {
        var manager = new LifecycleManager();
        var obj = new FakeLifecycle();
        manager.Register(obj);

        manager.Unregister(obj);

        obj.DestroyCount.Should().Be(1);
    }

    [Fact]
    public void Start_ShouldCallOnStart_OnRegisteredObjects()
    {
        var manager = new LifecycleManager();
        var obj = new FakeLifecycle();
        manager.Register(obj);

        manager.Start();

        obj.StartCount.Should().Be(1);
    }

    [Fact]
    public void FixedUpdate_ShouldCallOnFixedUpdate_OnRegisteredObjects()
    {
        var manager = new LifecycleManager();
        var obj = new FakeLifecycle();
        manager.Register(obj);

        manager.FixedUpdate(0.02);

        obj.FixedUpdateCount.Should().Be(1);
    }

    [Fact]
    public void Render_ShouldCallOnRender_OnRegisteredObjects()
    {
        var manager = new LifecycleManager();
        var obj = new FakeLifecycle();
        manager.Register(obj);

        manager.Render(0.16);

        obj.RenderCount.Should().Be(1);
    }

    [Fact]
    public void Destroy_ShouldCallOnDestroy_OnAllObjects()
    {
        var manager = new LifecycleManager();
        var obj1 = new FakeLifecycle();
        var obj2 = new FakeLifecycle();
        manager.Register(obj1);
        manager.Register(obj2);

        manager.Destroy();

        obj1.DestroyCount.Should().Be(1);
        obj2.DestroyCount.Should().Be(1);
    }

    [Fact]
    public void Unregister_DuringIteration_ShouldDeferRemovalUntilAfterIteration()
    {
        var manager = new LifecycleManager();
        var obj = new FakeLifecycle();
        manager.Register(obj);
        obj.OnUpdateAction = _ => manager.Unregister(obj);

        manager.Update(0.16);

        obj.DestroyCount.Should().Be(1);
        
        // Verify it's actually removed by running update again
        manager.Update(0.16);
        obj.UpdateCount.Should().Be(1); // Only the first update should have hit it
    }

    [Fact]
    public void Unregister_NonRegisteredObject_ShouldNotCallOnDestroy()
    {
        var manager = new LifecycleManager();
        var obj = new FakeLifecycle();

        manager.Unregister(obj);

        obj.DestroyCount.Should().Be(0);
    }

    [Fact]
    public void Register_DuringStart_ShouldCallAwakeAndStartAfterIteration()
    {
        var manager = new LifecycleManager();
        var obj1 = new FakeLifecycle();
        var obj2 = new FakeLifecycle();
        manager.Register(obj1);
        obj1.OnStartAction = _ => manager.Register(obj2);

        manager.Start();

        obj2.AwakeCount.Should().Be(1);
        obj2.StartCount.Should().Be(1);
    }

    [Fact]
    public void Register_DuringFixedUpdate_ShouldCallAwakeAndStartAfterIteration()
    {
        var manager = new LifecycleManager();
        var obj1 = new FakeLifecycle();
        var obj2 = new FakeLifecycle();
        manager.Register(obj1);
        obj1.OnFixedUpdateAction = _ => manager.Register(obj2);

        manager.FixedUpdate(0.02);

        obj2.AwakeCount.Should().Be(1);
        obj2.StartCount.Should().Be(1);
    }

    [Fact]
    public void Register_DuringRender_ShouldCallAwakeAndStartAfterIteration()
    {
        var manager = new LifecycleManager();
        var obj1 = new FakeLifecycle();
        var obj2 = new FakeLifecycle();
        manager.Register(obj1);
        obj1.OnRenderAction = _ => manager.Register(obj2);

        manager.Render(0.16);

        obj2.AwakeCount.Should().Be(1);
        obj2.StartCount.Should().Be(1);
    }

    [Fact]
    public void Unregister_DuringDestroy_ShouldClearEverything()
    {
        var manager = new LifecycleManager();
        var obj1 = new FakeLifecycle();
        var obj2 = new FakeLifecycle();
        manager.Register(obj1);
        manager.Register(obj2);
        
        obj1.OnDestroyAction = _ => manager.Unregister(obj2);

        manager.Destroy();

        obj1.DestroyCount.Should().Be(1);
        obj2.DestroyCount.Should().Be(1);
    }
    [Fact]
    public void RegisterAndUnregister_DuringIteration_ShouldCallLifecycleMethodsInOrder()
    {
        var manager = new LifecycleManager();
        var obj1 = new FakeLifecycle();
        var obj2 = new FakeLifecycle();
        manager.Register(obj1);
        
        obj1.OnUpdateAction = _ =>
        {
            manager.Register(obj2);
            manager.Unregister(obj2);
        };

        manager.Update(0.16);

        obj2.AwakeCount.Should().Be(1);
        obj2.StartCount.Should().Be(1);
        obj2.DestroyCount.Should().Be(1);
        
        manager.Update(0.16);
        obj2.UpdateCount.Should().Be(0); // Should not have been updated as it was removed
    }
    [Fact]
    public void Unregister_TwiceDuringIteration_ShouldCallOnDestroyOnce()
    {
        var manager = new LifecycleManager();
        var obj1 = new FakeLifecycle();
        var obj2 = new FakeLifecycle();
        manager.Register(obj1);
        manager.Register(obj2);
        
        obj1.OnUpdateAction = _ =>
        {
            manager.Unregister(obj2);
            manager.Unregister(obj2);
        };

        manager.Update(0.16);

        obj2.DestroyCount.Should().Be(1);
    }
    [Fact]
    public void Register_DuringDestroy_ShouldBeClearedAndNotRegistered()
    {
        var manager = new LifecycleManager();
        var obj1 = new FakeLifecycle();
        var obj2 = new FakeLifecycle();
        manager.Register(obj1);
        
        obj1.OnDestroyAction = _ => manager.Register(obj2);

        manager.Destroy();

        obj2.AwakeCount.Should().Be(0);
        
        // Ensure it wasn't processed after Destroy finished
        manager.Update(0.16);
        obj2.UpdateCount.Should().Be(0);
    }
}
