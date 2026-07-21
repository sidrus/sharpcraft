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

    // Forward-lit terrain (opaque voxels).
    public static readonly string DefaultVertex = LoadShader("Passes\\terrain.vert");
    public static readonly string DefaultFragment = LoadShader("Passes\\terrain.frag");

    // Fullscreen quad VS, shared by the final output pass.
    public static readonly string UnderwaterVertex = LoadShader("Passes\\underwater.vert");

    // Final output transform: tonemap -> FXAA -> sRGB -> dither.
    public static readonly string FxaaFragment = LoadShader("Passes\\fxaa.frag");

    public static readonly string ShadowVertex = LoadShader("Passes\\shadow.vert");
    public static readonly string ShadowFragment = LoadShader("Passes\\shadow.frag");

    public static readonly string SunVertex = LoadShader("Passes\\sun.vert");
    public static readonly string SunFragment = LoadShader("Passes\\sun.frag");

    // Placed static meshes (textured, sun-lit, emissive where the texture is bright).
    public static readonly string StaticMeshVertex = LoadShader("Passes\\staticmesh.vert");
    public static readonly string StaticMeshFragment = LoadShader("Passes\\staticmesh.frag");

    public static readonly string SkyboxVertex = LoadShader("Passes\\skybox.vert");
    public static readonly string SkyboxFragment = LoadShader("Passes\\skybox.frag");

    public static readonly string WaterVertex = LoadShader("Passes\\water.vert");
    public static readonly string WaterFragment = LoadShader("Passes\\water.frag");

    // Clustered forward+ light culling (research §2): compute passes over SSBOs.
    public static readonly string ClusterBuildAabbCompute = LoadShader("Passes\\cluster_build_aabb.comp");
    public static readonly string ClusterCullLightsCompute = LoadShader("Passes\\cluster_cull_lights.comp");

    // Auto-exposure (research §5.2): luminance histogram + temporal adaptation.
    public static readonly string HistogramBuildCompute = LoadShader("Passes\\histogram_build.comp");
    public static readonly string HistogramAverageCompute = LoadShader("Passes\\histogram_average.comp");

    // Temporal anti-aliasing resolve (research §9).
    public static readonly string TaaResolveFragment = LoadShader("Passes\\taa_resolve.frag");

    // Screen-space ambient occlusion (research §7).
    public static readonly string GtaoFragment = LoadShader("Passes\\gtao.frag");

    // Dual-filter HDR bloom (research §5.6).
    public static readonly string BloomDownFragment = LoadShader("Passes\\bloom_down.frag");
    public static readonly string BloomUpFragment = LoadShader("Passes\\bloom_up.frag");

    // Volumetric fog + sun light shafts (research §11 step 10): half-res march + composite.
    public static readonly string VolumetricMarchFragment = LoadShader("Passes\\volumetric_march.frag");
    public static readonly string VolumetricCompositeFragment = LoadShader("Passes\\volumetric_composite.frag");

    // Image-based lighting bakes (research §4.2/§6).
    public static readonly string CubemapCaptureVertex = LoadShader("Passes\\cubemap_capture.vert");
    public static readonly string IblSkyCaptureFragment = LoadShader("Passes\\ibl_sky_capture.frag");
    public static readonly string IblIrradianceFragment = LoadShader("Passes\\ibl_irradiance.frag");
    public static readonly string IblPrefilterFragment = LoadShader("Passes\\ibl_prefilter.frag");
    public static readonly string IblBrdfLutFragment = LoadShader("Passes\\ibl_brdf_lut.frag");
}