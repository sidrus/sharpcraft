namespace SharpCraft.Engine.Rendering.Pipeline;

/// <summary>
/// Thrown when a render pass declares a read whose resource is not produced by any earlier pass —
/// a mis-ordered pipeline, caught at construction rather than as a black screen at runtime.
/// </summary>
public sealed class RenderPassDependencyException(string passName, RenderResource resource)
    : Exception($"Render pass '{passName}' reads {resource} before any earlier pass writes it.")
{
    /// <summary>Name of the offending pass.</summary>
    public string PassName { get; } = passName;

    /// <summary>The resource read before it was produced.</summary>
    public RenderResource Resource { get; } = resource;
}
