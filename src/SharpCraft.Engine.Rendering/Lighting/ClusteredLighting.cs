using SharpCraft.Engine.Rendering.Lighting;
using SharpCraft.Engine.Rendering.Shaders;
using System.Numerics;
using System.Runtime.InteropServices;

namespace SharpCraft.Engine.Rendering.Lighting;

/// <summary>
/// Clustered forward+ light culling (research §2). Builds a camera-frustum-aligned froxel grid,
/// assigns punctual lights to clusters in two compute passes (AABB build + sphere/AABB cull), and
/// exposes the resulting SSBOs to the forward shading pass. The AABB build re-runs only when the
/// projection or resolution changes; the cull runs every frame.
/// </summary>
public sealed class ClusteredLighting : IDisposable
{
    public const uint GridX = 16;
    public const uint GridY = 9;
    public const uint GridZ = 24;
    public const uint ClusterCount = GridX * GridY * GridZ;
    public const int MaxLights = 256;
    public const float ZNear = 0.1f;
    public const float ZFar = 400.0f;

    [StructLayout(LayoutKind.Sequential)]
    private struct GpuLight
    {
        public Vector4 PositionRange; // xyz world pos, w radius
        public Vector4 Color;         // rgb color, w intensity
        public Vector4 Atten;         // constant, linear, quadratic, _
    }

    private readonly GL _gl;
    private readonly ComputeProgram _buildAabb;
    private readonly ComputeProgram _cullLights;

    // SSBO bindings match the layout(binding=N) in the cluster shaders + terrain.frag.
    private readonly ShaderStorageBuffer _clusters;     // 0: view-space AABBs
    private readonly ShaderStorageBuffer _lights;       // 1: light list
    private readonly ShaderStorageBuffer _lightGrid;    // 2: per-cluster (offset, count)
    private readonly ShaderStorageBuffer _lightIndex;   // 3: global light index list
    private readonly ShaderStorageBuffer _globalCount;  // 4: atomic index counter

    private readonly GpuLight[] _lightScratch = new GpuLight[MaxLights];

    private int _lastWidth;
    private int _lastHeight;
    private float _lastTanH;
    private float _lastTanV;
    private bool _disposed;

    public ClusteredLighting(GL gl)
    {
        _gl = gl;
        _buildAabb = new ComputeProgram(gl, Shaders.Shaders.ClusterBuildAabbCompute);
        _cullLights = new ComputeProgram(gl, Shaders.Shaders.ClusterCullLightsCompute);

        _clusters = new ShaderStorageBuffer(gl, 0);
        _lights = new ShaderStorageBuffer(gl, 1);
        _lightGrid = new ShaderStorageBuffer(gl, 2);
        _lightIndex = new ShaderStorageBuffer(gl, 3);
        _globalCount = new ShaderStorageBuffer(gl, 4);

        const int lightStride = 48;           // sizeof(GpuLight)
        const int maxLightsPerCluster = 100;  // must match cluster_cull_lights.comp
        _clusters.Allocate(ClusterCount * 32);
        _lights.Allocate(MaxLights * lightStride);
        _lightGrid.Allocate(ClusterCount * 8); // uvec2
        _lightIndex.Allocate(ClusterCount * maxLightsPerCluster * sizeof(uint));
        _globalCount.Allocate(sizeof(uint));
    }

    /// <summary>
    /// Run the two compute passes for this frame. <paramref name="projection"/> is the main
    /// reversed-Z infinite projection; fov/aspect are recovered from it for the (reversed-Z
    /// agnostic) tile rays.
    /// </summary>
    public void Update(Matrix4x4 view, Matrix4x4 projection, int width, int height, ReadOnlySpan<PointLightData> lights)
    {
        float tanH = 1.0f / projection.M11;
        float tanV = 1.0f / projection.M22;

        // Rebuild cluster AABBs only when the projection / resolution changes.
        if (width != _lastWidth || height != _lastHeight ||
            MathF.Abs(tanH - _lastTanH) > 1e-6f || MathF.Abs(tanV - _lastTanV) > 1e-6f)
        {
            BuildAabbs(width, height, tanH, tanV);
            _lastWidth = width;
            _lastHeight = height;
            _lastTanH = tanH;
            _lastTanV = tanV;
        }

        UploadLights(lights, out var lightCount);
        CullLights(view, lightCount);
    }

    private void BuildAabbs(int width, int height, float tanH, float tanV)
    {
        _clusters.BindBase();
        _buildAabb.Use();
        _buildAabb.SetUniform("gridSize", new Vector3(GridX, GridY, GridZ)); // uvec3 via float-cast OK
        _buildAabb.SetUniform("screenDimensions", new Vector2(width, height));
        _buildAabb.SetUniform("tanHalfFov", new Vector2(tanH, tanV));
        _buildAabb.SetUniform("zNear", ZNear);
        _buildAabb.SetUniform("zFar", ZFar);
        _buildAabb.Dispatch(GridX, GridY, GridZ);
    }

    private void UploadLights(ReadOnlySpan<PointLightData> lights, out uint count)
    {
        int n = Math.Min(lights.Length, MaxLights);
        for (int i = 0; i < n; i++)
        {
            var l = lights[i];
            _lightScratch[i] = new GpuLight
            {
                PositionRange = new Vector4(l.Position, l.Range),
                Color = new Vector4(l.Color, l.Intensity),
                Atten = new Vector4(l.Constant, l.Linear, l.Quadratic, 0f)
            };
        }
        _lights.Update(_lightScratch.AsSpan(0, Math.Max(n, 1)));
        count = (uint)n;
    }

    private void CullLights(Matrix4x4 view, uint lightCount)
    {
        _globalCount.SetUInt(0);

        _clusters.BindBase();
        _lights.BindBase();
        _lightGrid.BindBase();
        _lightIndex.BindBase();
        _globalCount.BindBase();

        _cullLights.Use();
        _cullLights.SetUniform("view", view);
        _cullLights.SetUniform("lightCount", lightCount);
        _cullLights.SetUniform("clusterCount", ClusterCount);

        uint groups = (ClusterCount + 127) / 128;
        _cullLights.Dispatch(groups, 1, 1);
    }

    /// <summary>Bind the buffers the forward shading pass reads (lights, grid, index list).</summary>
    public void BindForShading()
    {
        _lights.BindBase();
        _lightGrid.BindBase();
        _lightIndex.BindBase();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _buildAabb.Dispose();
        _cullLights.Dispose();
        _clusters.Dispose();
        _lights.Dispose();
        _lightGrid.Dispose();
        _lightIndex.Dispose();
        _globalCount.Dispose();
        _disposed = true;
    }
}