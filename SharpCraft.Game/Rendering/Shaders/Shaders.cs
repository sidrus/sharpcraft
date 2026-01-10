namespace SharpCraft.Game.Rendering.Shaders;

public static class Shaders
{
    public static readonly string DefaultVertex = File.ReadAllText("Assets/Shaders/default.vert");
    public static readonly string DefaultFragment = File.ReadAllText("Assets/Shaders/default.frag");
}