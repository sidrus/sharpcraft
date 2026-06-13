#ifndef CLUSTERS_GLSL
#define CLUSTERS_GLSL

// Clustered forward+ shared definitions (research §2). A camera-frustum-aligned 3D froxel grid:
// screen tiles in XY, exponential slices in view-space Z. The cluster light-list build is done in
// compute (cluster_build_aabb.comp + cluster_cull_lights.comp); the shading pass reads the
// per-cluster (offset,count) and walks only that cluster's lights.

// View-space AABB of one cluster (xyz used; w padding for std430 vec4 alignment).
struct Cluster {
    vec4 minPoint;
    vec4 maxPoint;
};

// A punctual (point) light. positionRange.w is the cull/attenuation radius in world units.
struct Light {
    vec4 positionRange; // xyz = world position, w = radius
    vec4 color;         // rgb = color,         w = intensity
    vec4 atten;         // x = constant, y = linear, z = quadratic, w = unused
};

// Sphere (view space) vs cluster AABB (view space).
bool sphereIntersectsAABB(vec3 center, float radius, vec3 aabbMin, vec3 aabbMax) {
    vec3 closest = clamp(center, aabbMin, aabbMax);
    vec3 d = closest - center;
    return dot(d, d) <= radius * radius;
}

// Exponential Z-slice for a (negative) view-space depth. Slices are thin near the camera and
// grow with distance (research §2 Z-slice equation). Reversed-Z agnostic: the caller passes a
// reconstructed linear view-Z, never a raw depth-buffer value.
uint clusterZSlice(float viewZ, float zNear, float zFar, uint sliceCount) {
    float depth = max(-viewZ, zNear);
    float slice = log(depth / zNear) / log(zFar / zNear) * float(sliceCount);
    return uint(clamp(slice, 0.0, float(sliceCount - 1u)));
}

// Linear cluster index from fragment screen coords (gl_FragCoord.xy) and the Z-slice.
uint clusterIndex(vec2 fragCoord, float viewZ, vec2 screenSize, uvec3 gridSize, float zNear, float zFar) {
    uint zSlice = clusterZSlice(viewZ, zNear, zFar, gridSize.z);
    vec2 tileSize = screenSize / vec2(gridSize.xy);
    uvec2 tile = uvec2(fragCoord / tileSize);
    tile = min(tile, gridSize.xy - 1u);
    return tile.x + tile.y * gridSize.x + zSlice * gridSize.x * gridSize.y;
}

#endif
