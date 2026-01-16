using System.Numerics;
using System.Runtime.InteropServices;

namespace SharpCraft.Client.Rendering.Shaders;

[StructLayout(LayoutKind.Sequential)]
public struct SceneData
{
    public Matrix4x4 ViewProjection;
    public Vector4 ViewPos;
    public Vector4 FogColor;
    public float FogNear;
    public float FogFar;
    public float Exposure;
    public float Gamma;
}

[StructLayout(LayoutKind.Sequential)]
public struct DirLightData
{
    public Vector4 Direction;
    public Vector4 Color;
}

[StructLayout(LayoutKind.Sequential)]
public struct PointLightDataStd140
{
    public Vector4 Position;
    public Vector4 Color;
    public float Intensity;
    public float Constant;
    public float Linear;
    public float Quadratic;
}

[StructLayout(LayoutKind.Sequential)]
public struct LightingData
{
    public Matrix4x4 LightSpaceMatrix;
    public DirLightData DirLight;
    public PointLightDataStd140 PointLight0;
    public PointLightDataStd140 PointLight1;
    public PointLightDataStd140 PointLight2;
    public PointLightDataStd140 PointLight3;
}
