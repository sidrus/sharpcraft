namespace SharpCraft.Client.Rendering.Shaders;

public static class Shaders
{
    private static string LoadShader(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Assets", "Shaders", name));

    public static readonly string DefaultVertex = LoadShader("default.vert");
    public static readonly string DefaultFragment = LoadShader("default.frag");

    public static readonly string UnderwaterVertex = LoadShader("underwater.vert");
    public static readonly string UnderwaterFragment = LoadShader("underwater.frag");
}