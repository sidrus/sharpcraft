namespace SharpCraft.Engine.Rendering.IBL;

/// <summary>
/// Contains GLSL shader source code for IBL generation.
/// These shaders implement the split-sum approximation for PBR IBL.
/// Reference: https://learnopengl.com/PBR/IBL/Specular-IBL
/// </summary>
internal static class IBLShaders
{
    #region Equirectangular to Cubemap

    public const string EquirectToCubemapVertex = """
        #version 450 core
        layout (location = 0) in vec3 aPos;

        out vec3 WorldPos;

        uniform mat4 projection;
        uniform mat4 view;

        void main()
        {
            WorldPos = aPos;
            gl_Position = projection * view * vec4(aPos, 1.0);
        }
        """;

    public const string EquirectToCubemapFragment = """
        #version 450 core
        out vec4 FragColor;
        in vec3 WorldPos;

        uniform sampler2D equirectangularMap;

        const vec2 invAtan = vec2(0.1591, 0.3183);

        vec2 SampleSphericalMap(vec3 v)
        {
            vec2 uv = vec2(atan(v.z, v.x), asin(v.y));
            uv *= invAtan;
            uv += 0.5;
            return uv;
        }

        void main()
        {
            vec2 uv = SampleSphericalMap(normalize(WorldPos));
            vec3 color = texture(equirectangularMap, uv).rgb;
            FragColor = vec4(color, 1.0);
        }
        """;

    #endregion

    #region Irradiance Convolution

    public const string IrradianceVertex = """
        #version 450 core
        layout (location = 0) in vec3 aPos;

        out vec3 WorldPos;

        uniform mat4 projection;
        uniform mat4 view;

        void main()
        {
            WorldPos = aPos;
            gl_Position = projection * view * vec4(aPos, 1.0);
        }
        """;

    /// <summary>
    /// Irradiance convolution shader for diffuse IBL.
    /// Integrates incoming radiance over the hemisphere weighted by cosine.
    /// </summary>
    public const string IrradianceFragment = """
        #version 450 core
        out vec4 FragColor;
        in vec3 WorldPos;

        uniform samplerCube environmentMap;

        const float PI = 3.14159265359;

        void main()
        {
            // The world vector acts as the normal of a tangent surface
            // from the origin, aligned to WorldPos. Given this normal, calculate all
            // incoming radiance of the environment. The result of this radiance
            // is the radiance of light coming from -Normal direction, which is what
            // we use in the PBR shader to sample irradiance.
            vec3 N = normalize(WorldPos);

            vec3 irradiance = vec3(0.0);

            // Tangent space calculation from origin point
            vec3 up    = vec3(0.0, 1.0, 0.0);
            vec3 right = normalize(cross(up, N));
            up         = normalize(cross(N, right));

            float sampleDelta = 0.025;
            float nrSamples = 0.0;

            for(float phi = 0.0; phi < 2.0 * PI; phi += sampleDelta)
            {
                for(float theta = 0.0; theta < 0.5 * PI; theta += sampleDelta)
                {
                    // Spherical to cartesian (in tangent space)
                    vec3 tangentSample = vec3(sin(theta) * cos(phi), sin(theta) * sin(phi), cos(theta));
                    // Tangent space to world
                    vec3 sampleVec = tangentSample.x * right + tangentSample.y * up + tangentSample.z * N;

                    irradiance += texture(environmentMap, sampleVec).rgb * cos(theta) * sin(theta);
                    nrSamples++;
                }
            }
            irradiance = PI * irradiance * (1.0 / float(nrSamples));

            FragColor = vec4(irradiance, 1.0);
        }
        """;

    #endregion

    #region Specular Prefilter

    public const string PrefilterVertex = """
        #version 450 core
        layout (location = 0) in vec3 aPos;

        out vec3 WorldPos;

        uniform mat4 projection;
        uniform mat4 view;

        void main()
        {
            WorldPos = aPos;
            gl_Position = projection * view * vec4(aPos, 1.0);
        }
        """;

    /// <summary>
    /// Prefilter shader for specular IBL using importance sampling of GGX.
    /// Each mip level stores the environment convolved with increasing roughness.
    /// Reference: https://google.github.io/filament/Filament.md.html#lighting/imagebasedlights/specularbrdfintegration
    /// </summary>
    public const string PrefilterFragment = """
        #version 450 core
        out vec4 FragColor;
        in vec3 WorldPos;

        uniform samplerCube environmentMap;
        uniform float roughness;
        uniform float envResolution;

        const float PI = 3.14159265359;

        // Van der Corput sequence for low-discrepancy sampling
        float RadicalInverse_VdC(uint bits)
        {
            bits = (bits << 16u) | (bits >> 16u);
            bits = ((bits & 0x55555555u) << 1u) | ((bits & 0xAAAAAAAAu) >> 1u);
            bits = ((bits & 0x33333333u) << 2u) | ((bits & 0xCCCCCCCCu) >> 2u);
            bits = ((bits & 0x0F0F0F0Fu) << 4u) | ((bits & 0xF0F0F0F0u) >> 4u);
            bits = ((bits & 0x00FF00FFu) << 8u) | ((bits & 0xFF00FF00u) >> 8u);
            return float(bits) * 2.3283064365386963e-10; // / 0x100000000
        }

        vec2 Hammersley(uint i, uint N)
        {
            return vec2(float(i)/float(N), RadicalInverse_VdC(i));
        }

        // Importance sample GGX distribution
        vec3 ImportanceSampleGGX(vec2 Xi, vec3 N, float roughness)
        {
            float a = roughness * roughness;

            float phi = 2.0 * PI * Xi.x;
            float cosTheta = sqrt((1.0 - Xi.y) / (1.0 + (a*a - 1.0) * Xi.y));
            float sinTheta = sqrt(1.0 - cosTheta*cosTheta);

            // From spherical coordinates to cartesian coordinates - halfway vector
            vec3 H;
            H.x = cos(phi) * sinTheta;
            H.y = sin(phi) * sinTheta;
            H.z = cosTheta;

            // From tangent-space H vector to world-space sample vector
            vec3 up        = abs(N.z) < 0.999 ? vec3(0.0, 0.0, 1.0) : vec3(1.0, 0.0, 0.0);
            vec3 tangent   = normalize(cross(up, N));
            vec3 bitangent = cross(N, tangent);

            vec3 sampleVec = tangent * H.x + bitangent * H.y + N * H.z;
            return normalize(sampleVec);
        }

        // GGX/Trowbridge-Reitz NDF
        float DistributionGGX(vec3 N, vec3 H, float roughness)
        {
            float a = roughness * roughness;
            float a2 = a * a;
            float NdotH = max(dot(N, H), 0.0);
            float NdotH2 = NdotH * NdotH;

            float nom   = a2;
            float denom = (NdotH2 * (a2 - 1.0) + 1.0);
            denom = PI * denom * denom;

            return nom / max(denom, 0.0000001);
        }

        void main()
        {
            vec3 N = normalize(WorldPos);

            // Make the simplifying assumption that V equals R equals the normal
            vec3 R = N;
            vec3 V = R;

            const uint SAMPLE_COUNT = 1024u;
            vec3 prefilteredColor = vec3(0.0);
            float totalWeight = 0.0;

            for(uint i = 0u; i < SAMPLE_COUNT; ++i)
            {
                // Generate a sample vector that's biased towards the preferred alignment direction (importance sampling)
                vec2 Xi = Hammersley(i, SAMPLE_COUNT);
                vec3 H = ImportanceSampleGGX(Xi, N, roughness);
                vec3 L = normalize(2.0 * dot(V, H) * H - V);

                float NdotL = max(dot(N, L), 0.0);
                if(NdotL > 0.0)
                {
                    // Sample from the environment's mip level based on roughness/pdf
                    float D   = DistributionGGX(N, H, roughness);
                    float NdotH = max(dot(N, H), 0.0);
                    float HdotV = max(dot(H, V), 0.0);
                    float pdf = D * NdotH / (4.0 * HdotV) + 0.0001;

                    float saTexel  = 4.0 * PI / (6.0 * envResolution * envResolution);
                    float saSample = 1.0 / (float(SAMPLE_COUNT) * pdf + 0.0001);

                    float mipLevel = roughness == 0.0 ? 0.0 : 0.5 * log2(saSample / saTexel);

                    prefilteredColor += textureLod(environmentMap, L, mipLevel).rgb * NdotL;
                    totalWeight      += NdotL;
                }
            }

            prefilteredColor = prefilteredColor / totalWeight;

            FragColor = vec4(prefilteredColor, 1.0);
        }
        """;

    #endregion

    #region BRDF Integration LUT

    public const string BrdfVertex = """
        #version 450 core
        layout (location = 0) in vec2 aPos;
        layout (location = 1) in vec2 aTexCoords;

        out vec2 TexCoords;

        void main()
        {
            TexCoords = aTexCoords;
            gl_Position = vec4(aPos, 0.0, 1.0);
        }
        """;

    /// <summary>
    /// BRDF integration LUT shader.
    /// Precomputes the split-sum approximation's second part: the BRDF integration.
    /// Stores scale and bias for the Fresnel term based on roughness and NdotV.
    /// Reference: https://google.github.io/filament/Filament.md.html#lighting/imagebasedlights/specularbrdfintegration
    /// </summary>
    public const string BrdfFragment = """
        #version 450 core
        out vec2 FragColor;
        in vec2 TexCoords;

        const float PI = 3.14159265359;

        // Van der Corput sequence
        float RadicalInverse_VdC(uint bits)
        {
            bits = (bits << 16u) | (bits >> 16u);
            bits = ((bits & 0x55555555u) << 1u) | ((bits & 0xAAAAAAAAu) >> 1u);
            bits = ((bits & 0x33333333u) << 2u) | ((bits & 0xCCCCCCCCu) >> 2u);
            bits = ((bits & 0x0F0F0F0Fu) << 4u) | ((bits & 0xF0F0F0F0u) >> 4u);
            bits = ((bits & 0x00FF00FFu) << 8u) | ((bits & 0xFF00FF00u) >> 8u);
            return float(bits) * 2.3283064365386963e-10;
        }

        vec2 Hammersley(uint i, uint N)
        {
            return vec2(float(i)/float(N), RadicalInverse_VdC(i));
        }

        vec3 ImportanceSampleGGX(vec2 Xi, vec3 N, float roughness)
        {
            float a = roughness * roughness;

            float phi = 2.0 * PI * Xi.x;
            float cosTheta = sqrt((1.0 - Xi.y) / (1.0 + (a*a - 1.0) * Xi.y));
            float sinTheta = sqrt(1.0 - cosTheta*cosTheta);

            vec3 H;
            H.x = cos(phi) * sinTheta;
            H.y = sin(phi) * sinTheta;
            H.z = cosTheta;

            vec3 up        = abs(N.z) < 0.999 ? vec3(0.0, 0.0, 1.0) : vec3(1.0, 0.0, 0.0);
            vec3 tangent   = normalize(cross(up, N));
            vec3 bitangent = cross(N, tangent);

            vec3 sampleVec = tangent * H.x + bitangent * H.y + N * H.z;
            return normalize(sampleVec);
        }

        // Smith's Schlick-GGX geometry function for IBL
        float GeometrySchlickGGX(float NdotV, float roughness)
        {
            // Note: different k for IBL vs direct lighting
            float a = roughness;
            float k = (a * a) / 2.0;

            float nom   = NdotV;
            float denom = NdotV * (1.0 - k) + k;

            return nom / denom;
        }

        float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
        {
            float NdotV = max(dot(N, V), 0.0);
            float NdotL = max(dot(N, L), 0.0);
            float ggx2 = GeometrySchlickGGX(NdotV, roughness);
            float ggx1 = GeometrySchlickGGX(NdotL, roughness);

            return ggx1 * ggx2;
        }

        vec2 IntegrateBRDF(float NdotV, float roughness)
        {
            vec3 V;
            V.x = sqrt(1.0 - NdotV*NdotV);
            V.y = 0.0;
            V.z = NdotV;

            float A = 0.0;
            float B = 0.0;

            vec3 N = vec3(0.0, 0.0, 1.0);

            const uint SAMPLE_COUNT = 1024u;
            for(uint i = 0u; i < SAMPLE_COUNT; ++i)
            {
                vec2 Xi = Hammersley(i, SAMPLE_COUNT);
                vec3 H = ImportanceSampleGGX(Xi, N, roughness);
                vec3 L = normalize(2.0 * dot(V, H) * H - V);

                float NdotL = max(L.z, 0.0);
                float NdotH = max(H.z, 0.0);
                float VdotH = max(dot(V, H), 0.0);

                if(NdotL > 0.0)
                {
                    float G = GeometrySmith(N, V, L, roughness);
                    float G_Vis = (G * VdotH) / (NdotH * NdotV);
                    float Fc = pow(1.0 - VdotH, 5.0);

                    A += (1.0 - Fc) * G_Vis;
                    B += Fc * G_Vis;
                }
            }
            A /= float(SAMPLE_COUNT);
            B /= float(SAMPLE_COUNT);
            return vec2(A, B);
        }

        void main()
        {
            vec2 integratedBRDF = IntegrateBRDF(TexCoords.x, TexCoords.y);
            FragColor = integratedBRDF;
        }
        """;

    #endregion
}
