namespace SharpCraft.Client.Rendering.Shaders;

public static class Shaders
{
    private static string LoadShader(string path)
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Shaders", path);
        var source = File.ReadAllText(fullPath);
        return ProcessIncludes(source, Path.GetDirectoryName(fullPath)!);
    }

    private static string ProcessIncludes(string source, string currentDir)
    {
        var lines = source.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.StartsWith("#include \""))
            {
                var includePath = line.Substring(10, line.Length - 11);
                var fullIncludePath = Path.Combine(currentDir, includePath);
                if (File.Exists(fullIncludePath))
                {
                    lines[i] = ProcessIncludes(File.ReadAllText(fullIncludePath), Path.GetDirectoryName(fullIncludePath)!);
                }
            }
        }
        return string.Join('\n', lines);
    }

    public static readonly string DefaultVertex = LoadShader("Passes\\gbuffer.vert");
    public static readonly string DefaultFragment = LoadShader("Passes\\gbuffer.frag");

    public static readonly string UnderwaterVertex = LoadShader("Passes\\underwater.vert");
    public static readonly string UnderwaterFragment = LoadShader("Passes\\underwater.frag");
}