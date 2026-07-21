namespace SharpCraft.Engine.Rendering;

/// <summary>
/// One stage of the render pipeline. Owns its own shaders/targets and sets the full GL state it needs
/// on entry (assumes nothing from prior passes). Declares the logical resources it reads and writes so
/// the pipeline can validate ordering at startup.
/// </summary>
public interface IRenderPass : IDisposable
{
    /// <summary>Human-readable pass name, used in ordering-validation diagnostics.</summary>
    string Name { get; }

    /// <summary>Logical resources this pass samples; each must be produced by an earlier pass.</summary>
    IReadOnlyList<RenderResource> Reads { get; }

    /// <summary>Logical resources this pass produces.</summary>
    IReadOnlyList<RenderResource> Writes { get; }

    /// <summary>Whether this pass runs for the given frame; disabled passes are skipped.</summary>
    bool Enabled(RenderContext context);

    /// <summary>Runs the pass, reading from and writing into the shared <paramref name="targets"/>.</summary>
    void Execute(IWorld world, RenderContext context, RenderTargets targets);
}
