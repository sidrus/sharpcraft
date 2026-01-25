namespace SharpCraft.Engine.Rendering.Shaders;

public static class Shaders
{
    private static readonly ShaderPreprocessor Preprocessor = new(path =>
        File.Exists(path) ? File.ReadAllText(path) : null);

    private static string LoadShader(string path)
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Shaders", path);
        
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Shader file not found: {fullPath}");
        }
        
        var source = File.ReadAllText(fullPath);
        var result = Preprocessor.Process(source, Path.GetDirectoryName(fullPath)!);
        
        // Validate the processed shader
        if (string.IsNullOrWhiteSpace(result))
        {
            throw new InvalidOperationException($"Shader preprocessing produced empty result for: {path}");
        }
        
        if (!result.Contains("#version"))
        {
            throw new InvalidOperationException($"Shader missing #version directive after preprocessing: {path}");
        }
        
        if (!result.Contains("void main()") && !result.Contains("void main(void)"))
        {
            throw new InvalidOperationException($"Shader missing main function after preprocessing: {path}");
        }
        
        return result;
    }

    public static readonly string DefaultVertex = LoadShader("Passes\\gbuffer.vert");
    public static readonly string DefaultFragment = LoadShader("Passes\\gbuffer.frag");

    public static readonly string UnderwaterVertex = LoadShader("Passes\\underwater.vert");
    public static readonly string UnderwaterFragment = LoadShader("Passes\\underwater.frag");

    public static readonly string FXAAFragment = LoadShader("Passes\\fxaa.frag");

    public static readonly string BloomDownsampleFragment = LoadShader("Passes\\bloom_downsample.frag");
    public static readonly string BloomUpsampleFragment = LoadShader("Passes\\bloom_upsample.frag");

    public static readonly string VolumetricLightingFragment = LoadShader("Passes\\volumetric_lighting.frag");

    public static readonly string ShadowVertex = LoadShader("Passes\\shadow.vert");
    public static readonly string ShadowFragment = LoadShader("Passes\\shadow.frag");

    public static readonly string SunVertex = LoadShader("Passes\\sun.vert");
    public static readonly string SunFragment = LoadShader("Passes\\sun.frag");

    public static readonly string SkyboxVertex = LoadShader("Passes\\skybox.vert");
    public static readonly string SkyboxFragment = LoadShader("Passes\\skybox.frag");

    public static readonly string LightingVertex = LoadShader("Passes\\lighting.vert");
    public static readonly string LightingFragment = LoadShader("Passes\\lighting.frag");

    public static readonly string WaterVertex = LoadShader("Passes\\water.vert");
    public static readonly string WaterFragment = LoadShader("Passes\\water.frag");
}