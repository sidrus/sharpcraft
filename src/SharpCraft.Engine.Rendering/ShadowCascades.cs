using System.Numerics;

namespace SharpCraft.Engine.Rendering;

/// <summary>
/// Computes per-cascade light-space matrices and split depths for <see cref="CascadedShadowMap"/>
/// (research §8). Uses a practical (logarithmic/uniform blend) split scheme, a bounding-sphere fit
/// per slice for rotation-invariant cascade sizing, and texel-grid snapping to kill shimmer when
/// the camera moves.
/// </summary>
public static class ShadowCascades
{
    public readonly record struct Result(Matrix4x4[] LightSpaceMatrices, float[] SplitDepths);

    /// <param name="view">Main camera view matrix (reversed-Z agnostic).</param>
    /// <param name="projection">Main camera reversed-Z infinite projection (used to recover fov/aspect/near).</param>
    /// <param name="lightDir">Normalised direction the sunlight travels (from sun toward scene).</param>
    /// <param name="shadowDistance">Far clip for the shadowed range; cascades cover [near, shadowDistance].</param>
    /// <param name="shadowMapSize">Per-cascade texture size, for texel snapping.</param>
    /// <param name="cascadeCount">Number of cascades (3–4).</param>
    /// <param name="lambda">Blend between uniform (0) and logarithmic (1) splits.</param>
    public static Result Compute(
        Matrix4x4 view,
        Matrix4x4 projection,
        Vector3 lightDir,
        float shadowDistance,
        uint shadowMapSize,
        int cascadeCount,
        float lambda = 0.6f)
    {
        // Recover the symmetric perspective from the reversed-Z infinite projection:
        //   M11 = f/aspect, M22 = f, M43 = near   (see ReversedZ.InfinitePerspective).
        float f = projection.M22;
        float aspect = projection.M22 / projection.M11;
        float near = projection.M43;
        float far = shadowDistance;

        float tanHalfV = 1.0f / f;
        float tanHalfH = tanHalfV * aspect;

        Matrix4x4.Invert(view, out var invView);
        lightDir = Vector3.Normalize(lightDir);

        // Practical split scheme (Nvidia/GPU Gems): blend log and uniform distributions.
        var splitDistances = new float[cascadeCount];
        float clipRange = far - near;
        float ratio = far / near;
        for (int i = 0; i < cascadeCount; i++)
        {
            float p = (i + 1) / (float)cascadeCount;
            float logSplit = near * MathF.Pow(ratio, p);
            float uniformSplit = near + clipRange * p;
            splitDistances[i] = lambda * logSplit + (1.0f - lambda) * uniformSplit;
        }

        var matrices = new Matrix4x4[cascadeCount];
        var splitDepths = new float[cascadeCount];
        float lastSplit = near;

        // 8 world-space corners of the current frustum slice (reused each cascade).
        Span<Vector3> corners = stackalloc Vector3[8];

        for (int c = 0; c < cascadeCount; c++)
        {
            float splitNear = lastSplit;
            float splitFar = splitDistances[c];

            int idx = 0;
            for (int s2 = 0; s2 < 2; s2++)
            {
                float z = s2 == 0 ? splitNear : splitFar;
                float x = z * tanHalfH;
                float y = z * tanHalfV;
                // Camera looks down -Z in view space.
                corners[idx++] = Vector3.Transform(new Vector3(-x, -y, -z), invView);
                corners[idx++] = Vector3.Transform(new Vector3(x, -y, -z), invView);
                corners[idx++] = Vector3.Transform(new Vector3(x, y, -z), invView);
                corners[idx++] = Vector3.Transform(new Vector3(-x, y, -z), invView);
            }

            // Bounding sphere of the slice → rotation-invariant, stable cascade extent.
            var center = Vector3.Zero;
            for (int i = 0; i < 8; i++) center += corners[i];
            center /= 8.0f;

            float radius = 0.0f;
            for (int i = 0; i < 8; i++) radius = MathF.Max(radius, (corners[i] - center).Length());
            // Quantise the radius so the cascade size doesn't wobble frame to frame.
            radius = MathF.Ceiling(radius * 16.0f) / 16.0f;

            // Pull the light camera back far enough to capture tall casters behind the slice;
            // DEPTH_CLAMP in the shadow pass handles anything still beyond the near plane.
            var eye = center - lightDir * radius * 2.0f;
            var up = MathF.Abs(lightDir.Y) > 0.99f ? Vector3.UnitX : Vector3.UnitY;
            var lightView = Matrix4x4.CreateLookAt(eye, center, up);

            // Conventional ortho (z in [near, far]); generous depth range around the slice.
            var lightProj = Matrix4x4.CreateOrthographicOffCenter(
                -radius, radius, -radius, radius, 0.0f, radius * 4.0f);

            var shadowMatrix = lightView * lightProj;

            // Texel snap: round the projected origin to the shadow-map grid (kills shimmer).
            var shadowOrigin = Vector4.Transform(Vector3.Zero, shadowMatrix);
            shadowOrigin *= shadowMapSize / 2.0f;
            var rounded = new Vector4(MathF.Round(shadowOrigin.X), MathF.Round(shadowOrigin.Y), shadowOrigin.Z, shadowOrigin.W);
            var offset = (rounded - shadowOrigin) * 2.0f / shadowMapSize;
            offset.Z = 0.0f;
            offset.W = 0.0f;

            matrices[c] = shadowMatrix * Matrix4x4.CreateTranslation(offset.X, offset.Y, offset.Z);
            splitDepths[c] = splitFar;
            lastSplit = splitFar;
        }

        return new Result(matrices, splitDepths);
    }
}
