namespace SharpCraft.Engine.Rendering.Pipeline;

/// <summary>
/// Runs render passes in their authored order. Culls disabled passes, owns pass lifetime, and validates
/// at construction that every declared read is produced by an earlier pass (RDG-lite — see
/// docs/rendering/render-pass-system-plan.md).
/// </summary>
public sealed class RenderPassPipeline : IDisposable
{
    private readonly IReadOnlyList<IRenderPass> _passes;

    public RenderPassPipeline(IReadOnlyList<IRenderPass> passes, IReadOnlySet<RenderResource>? externalInputs = null)
    {
        Validate(passes, externalInputs);
        _passes = passes;
    }

    /// <summary>Executes each enabled pass in order against the shared targets.</summary>
    public void Execute(IWorld world, RenderContext context, RenderTargets targets)
    {
        foreach (var pass in _passes)
        {
            if (pass.Enabled(context))
            {
                pass.Execute(world, context, targets);
            }
        }
    }

    private static void Validate(IReadOnlyList<IRenderPass> passes, IReadOnlySet<RenderResource>? externalInputs)
    {
        var produced = externalInputs is null ? [] : new HashSet<RenderResource>(externalInputs);
        foreach (var pass in passes)
        {
            foreach (var read in pass.Reads)
            {
                if (!produced.Contains(read))
                {
                    throw new RenderPassDependencyException(pass.Name, read);
                }
            }

            foreach (var write in pass.Writes)
            {
                produced.Add(write);
            }
        }
    }

    public void Dispose()
    {
        foreach (var pass in _passes)
        {
            pass.Dispose();
        }
    }
}