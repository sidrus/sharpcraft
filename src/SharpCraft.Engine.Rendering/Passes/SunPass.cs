namespace SharpCraft.Engine.Rendering.Passes;

/// <summary>
/// Draws the sun disc at the far plane into the HDR scene after opaque geometry, so it is occluded by
/// nearer terrain (reversed-Z GEqual). Self-culls when the sun is off or below the horizon.
/// </summary>
public sealed class SunPass(GL gl) : IDisposable
{
    private readonly SunRenderer _renderer = new(gl);

    public bool Enabled(RenderContext context)
    {
        return true;
    }

    public void Execute(IWorld world, RenderContext context, RenderTargets targets)
    {
        _renderer.Render(context);
    }

    public void Dispose()
    {
        _renderer.Dispose();
    }
}