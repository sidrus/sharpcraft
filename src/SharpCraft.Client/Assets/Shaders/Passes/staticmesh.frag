#version 450 core
in vec2 TexCoord;
in vec3 Normal;
in vec3 FragPos;

out vec4 FragColor;

uniform sampler2D baseColorTex;
uniform vec3 sunDirection; // direction the sunlight travels
uniform vec3 sunColor;

void main() {
    vec4 tex = texture(baseColorTex, TexCoord);
    if (tex.a < 0.5) discard; // transparent margin around the model

    vec3 albedo = tex.rgb;
    vec3 n = normalize(Normal);

    // Simple sun + ambient lighting for the wooden handle.
    float ndl = max(dot(n, normalize(-sunDirection)), 0.0);
    vec3 ambient = vec3(0.30);
    vec3 lit = albedo * (ambient + sunColor * ndl * 0.8);

    // The burning head (top of the model / high texture-v) is emissive so it
    // glows independent of scene lighting and feeds the HDR bloom pass.
    float glow = smoothstep(0.58, 0.85, TexCoord.y);
    vec3 emission = albedo * glow * 4.0;

    FragColor = vec4(lit + emission, 1.0);
}
