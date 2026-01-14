namespace SharpCraft.Sdk.Lifecycle;

/// <summary>
/// Defines the lifecycle methods for objects managed by a lifecycle manager.
/// </summary>
public interface ILifecycle
{
    /// <summary>
    /// Called when the object is first created or added to the lifecycle manager.
    /// Use this for internal initialization that doesn't depend on other objects.
    /// </summary>
    void OnAwake() {}

    /// <summary>
    /// Called after OnAwake, once all objects are ready.
    /// Use this for initialization that depends on other objects.
    /// </summary>
    void OnStart() {}

    /// <summary>
    /// Called every frame for logic updates.
    /// </summary>
    void OnUpdate(double deltaTime) {}

    /// <summary>
    /// Called at a fixed interval for physics and other deterministic logic.
    /// </summary>
    void OnFixedUpdate(double fixedDeltaTime) {}

    /// <summary>
    /// Called every frame for rendering operations.
    /// </summary>
    void OnRender(double deltaTime) {}

    /// <summary>
    /// Called when the object is being destroyed or removed from the lifecycle manager.
    /// </summary>
    void OnDestroy() {}
}
