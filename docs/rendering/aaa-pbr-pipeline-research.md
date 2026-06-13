# Building a AAA HDR PBR Rendering Pipeline — Research & Reference

> Status: **Research document.** This is a from-first-principles study of how modern,
> AAA-quality, HDR + physically-based rendering (PBR) pipelines are built. It deliberately
> ignores SharpCraft's current shaders/pipeline — the goal is to establish the *correct*
> target architecture before we scrap and rebuild.
>
> **API dimension:** §§2–11 establish the API-agnostic theory. **§12 is the OpenGL reality
> check** — what specifically changes when the target is OpenGL 4.5 core (SharpCraft's actual
> backend, via Silk.NET, GLSL `#version 450 core`). OpenGL keeps almost all of the theory
> intact but breaks a handful of load-bearing assumptions (HDR display output, depth-buffer
> layout, async streaming, the cubemap coordinate convention). Where an assumption breaks
> mid-section, a **▶ OpenGL** callout flags it and points at §12.

---

## 0. The one rule that makes it "physically based"

Everything below is downstream of two principles:

1. **Energy conservation** — a surface can never reflect more light than it receives.
   Diffuse + specular response together must integrate to ≤ 1.
2. **Linear light, everywhere** — all lighting math happens in *linear* color space with
   *physically meaningful* (HDR, unbounded) radiance values. sRGB/display encoding is a
   final output step, never an intermediate.

If a pipeline violates either, it is not PBR no matter how many texture maps it has. The
rest of this document is the machinery that upholds these two rules.

---

## 1. The end-to-end pipeline at a glance

A modern AAA frame is a chain of passes feeding HDR linear radiance into a final tonemap +
display-encode. A representative ordering:

```
1.  Depth pre-pass (Z-prepass)         → early-Z, avoids overdraw in main pass
2.  Shadow passes                       → cascaded shadow maps, per-light depth
3.  G-buffer pass (if deferred)         → albedo, normal, roughness, metallic, motion, depth
4.  Screen-space AO (GTAO/SSAO)         → from depth+normal
5.  Lighting / shading                  → analytic lights + IBL, evaluates the BRDF (HDR out)
6.  Screen-space reflections (SSR)      → from G-buffer + previous frame HDR color
7.  Transparent / forward pass          → blended geometry, evaluated forward
8.  Volumetrics                         → fog, light shafts (often froxel-based)
9.  Post: TAA → motion blur → DoF → bloom
10. Auto-exposure (luminance histogram) → adapts key of the scene
11. Tonemap (ACES / AgX / etc.)         → HDR linear → display range
12. Output transform / OETF             → encode to sRGB or HDR10/scRGB for the display
13. UI / FXAA / sharpen                 → composited in display space
```

Sections 2–9 cover each block.

**One gap worth flagging up front: depth-buffer layout.** A 24-bit (or even fp32) depth buffer
with a conventional `[0,1]` mapping wastes almost all its precision just past the near plane,
producing z-fighting at distance — fatal for a voxel game with a long view distance. The fix is
**reversed-Z**: map the near plane to 1.0 and the far plane to 0.0, which pairs the
floating-point depth buffer's dense exponent range near 0 with the perspective projection's
compressed far range, giving near-uniform precision across the whole frustum. This needs a
`GREATER` depth test and a far-plane clear of 0.0. It is essentially free and should be designed
in from day one (it interacts with the projection matrix, the depth pre-pass, and any
depth-reconstruction in screen-space passes). **▶ OpenGL:** reversed-Z is the single biggest
OpenGL gotcha here — see §12.2.

---

## 2. Choosing the shading architecture

Three viable architectures; all are used in shipping AAA titles.

| Architecture | How lights reach surfaces | Strengths | Weaknesses |
|---|---|---|---|
| **Forward** | Loop all lights per pixel in the object shader | Simple; MSAA works; transparency native; low bandwidth | Light count per object limited; overdraw wastes shading |
| **Deferred** | Write material to G-buffer, shade in screen space | Decouples geometry from lighting; many lights cheaply; one shade per pixel | Heavy bandwidth; MSAA hard; transparency needs a separate forward pass; fixed material model |
| **Clustered / Tiled (Forward+ or Clustered Deferred)** | Bin lights into a 3D froxel grid, each pixel reads only its cluster's lights | Scales to thousands of lights; works for *both* forward and deferred; transparency-friendly | Needs compute shaders & structured buffers; cluster build cost |

**Modern default:** **Clustered shading.** Build a camera-frustum-aligned 3D grid of clusters
(e.g. 16×9×24 in screen-XY × view-Z, Z sliced *exponentially*). A compute pass assigns each
light to the clusters its bounding volume touches. Then either:

- **Clustered Forward+** — geometry shader reads its cluster's light list. Keeps MSAA and
  transparency easy; preferred when material variety is high (it is, for a voxel game with
  many block types) and bandwidth matters.
- **Clustered Deferred** — G-buffer first, then shade reading the same cluster lists.

The key insight from the literature: *the cluster light-list build is identical for forward
and deferred*, so a hybrid (opaque deferred, transparent forward, shared light grid) is a
common AAA choice. Pick **clustered forward+** as the baseline unless profiling demands
deferred; it keeps MSAA viable, avoids fat G-buffer bandwidth, and handles high material
variety better than classic deferred. (HDR and wide-gamut are orthogonal to this choice —
both architectures shade into the same fp16 linear targets.)

**The Z-slice equation** (exponential slicing) maps view-space depth to a cluster index so
slices are thin near the camera and grow with distance:
```
slice  = floor( log(−z_view) · (numSlices / log(far/near)) − log(near) · numSlices/log(far/near) )
z(k)   = near · (far/near)^(k / numSlices)            // inverse: slice boundary depths
```
Light assignment runs in a compute pass: each cluster's view-space AABB is tested against every
light's bounding sphere/cone, and surviving light indices are packed into a global index buffer
that the shading pass reads via a per-cluster `(offset,count)`. With reversed-Z the depth→slice
math must account for the flipped mapping (reconstruct linear view-Z first, don't index off the
raw depth value).

**▶ OpenGL:** clustered shading leans entirely on **compute shaders + SSBOs**, both core only in
**GL 4.3**. SharpCraft's 4.5 target is fine, but there is no bindless-texture or descriptor-set
equivalent in core GL, which changes how per-material data is fed to the shading pass — see §12.4.

---

## 3. The PBR material model (metallic–roughness)

The industry-standard parameterization (glTF, UE, Unity HDRP, Filament, Frostbite). Per
surface point you need:

| Parameter | Space | Meaning |
|---|---|---|
| **Base Color** | linear RGB [0..1] | Diffuse albedo for dielectrics; specular F₀ tint for metals |
| **Metallic** | scalar [0..1] | Treat as binary; intermediate values only for transition pixels |
| **Roughness** (perceptual) | scalar [0..1] | Microfacet spread; remapped to `α = roughness²` |
| **Normal** | tangent-space RGB | Per-pixel surface normal |
| **Reflectance** (dielectric F₀) | scalar | Optional; defaults to 0.5 → 4% Fresnel |
| **Ambient Occlusion** | scalar | Baked/SS occlusion of *ambient* (IBL) only |
| **Emissive** | linear RGB (HDR) | Self-illumination, added after shading |

**Derived terms (the canonical remaps):**

```glsl
// Perceptual roughness → BRDF alpha. Clamp the *perceptual* roughness so α² doesn't
// underflow in fp16 (Filament clamps to 0.045, or 0.089 on half-float targets).
float perceptualRoughness = clamp(perceptualRoughnessIn, MIN_PERCEPTUAL_ROUGHNESS, 1.0);
float a   = perceptualRoughness * perceptualRoughness;   // GGX α  (NOT the perceptual value)
float a2  = a * a;                                       // α², as used by D and V in §3.1

// F0 (specular reflectance at normal incidence)
//   dielectrics: a fixed ~4% grey, optionally artist-controlled via 'reflectance'
//   metals:      tinted by baseColor
vec3 f0 = mix(vec3(0.16 * reflectance * reflectance), baseColor, metallic); // dielectric default 0.04
vec3 diffuseColor = (1.0 - metallic) * baseColor;                            // metals have no diffuse
```

Defaults worth memorizing: **dielectric F₀ = 0.04**, polyurethane/clear-coat IOR 1.5 → 0.04,
water ≈ 0.02, gems ≈ 0.05–0.08.

### 3.1 The specular BRDF — Cook-Torrance microfacet

Specular = `D · G · F / (4 · NoV · NoL)`. The modern, energy-correct choices:

**D — Normal Distribution Function: GGX / Trowbridge-Reitz**
```
D_GGX(NoH, a) = a² / ( π · ((NoH)² · (a² − 1) + 1)² )
```
GGX is the industry standard for its long, realistic specular tails.

**G / V — Geometric shadowing-masking: Smith height-correlated, folded into a *visibility* term**
The visibility term `V = G / (4·NoV·NoL)` folds the `4·NoV·NoL` denominator in:
```
V_SmithGGX(NoV, NoL, a) =
   0.5 / ( NoL·√((NoV)²(1−a²)+a²) + NoV·√((NoL)²(1−a²)+a²) )
```
Use the height-correlated Smith form (Heitz 2014) — it's more correct than the separable
Schlick-GGX approximation and barely more expensive.

**F — Fresnel: Schlick approximation**
```
F_Schlick(VoH, f0) = f0 + (1 − f0)·(1 − VoH)⁵
```
(Add an `f90` term for grazing-angle roughness falloff if desired.)

### 3.2 The diffuse BRDF

Lambert (`albedo / π`) is the default and is correct enough for most surfaces. Frostbite and
others offer a *Disney/Burley* diffuse that adds roughness-dependent retro-reflection at
grazing angles — better for cloth/skin but not energy-conserving without care. **Start with
Lambert.**

### 3.3 Multi-scattering energy compensation

Single-scatter Cook-Torrance *loses energy* at high roughness (rough metals look too dark).
Fix with a multiscatter compensation term derived from the DFG LUT (split-sum, §6):
```
energyCompensation = 1.0 + f0 * (1.0 / dfg.y − 1.0);
specular *= energyCompensation;
```
A correct PBR pipeline makes a fully reflective metal indistinguishable from its environment
*regardless of roughness* — that's the test that this term is right.

### 3.4 Optional extended lobes (add later)
- **Clear coat** — second specular lobe, fixed F₀=0.04, Kelemen visibility. Attenuates the
  base *diffuse* by `(1 − Fc)` and the base *specular* by `(1 − Fc)²` — Filament's form is
  `(Fd + Fr·(1 − Fc))·(1 − Fc) + Frc`. (Car paint, lacquer, wet surfaces.)
- **Anisotropy** — split α into `αt = α(1+aniso)`, `αb = α(1−aniso)` (brushed metal, hair).
- **Cloth / sheen** — Charlie NDF + inverted geometry term (fabric).
- **Subsurface** — wrapped diffuse or full SSS (skin, wax, foliage, snow).

### 3.5 Specular anti-aliasing (geometric / normal-map AA)

A gap most "PBR tutorials" skip and a *guaranteed* problem for blocky voxel geometry with sharp
normals and high-frequency detail textures: when a smooth (low-roughness) surface's normal varies
faster than one pixel, the thin specular highlight aliases into shimmering, crawling fireflies
that no MSAA/FXAA can fully fix (it's sub-pixel BRDF aliasing, not edge aliasing). The physically
grounded fix is to **widen roughness to cover the normal variance inside the pixel's footprint**:

```glsl
// Kaplanyan/Tokuyoshi-style geometric specular AA, evaluated per-pixel in the shading pass
vec3  dndx = dFdx(normalWS), dndy = dFdy(normalWS);
float variance   = SIGMA2 * (dot(dndx, dndx) + dot(dndy, dndy));   // SIGMA2 = 0.15915494 (1/2π, per the paper)
float kernelRough = min(2.0 * variance, KAPPA);                    // KAPPA = 0.18 clamp
float a2_aa = saturate(a2 + kernelRough);                          // widen GGX α² (the kernel adds to α², not α)
```

Combine with **mip-mapped normal maps + roughness baked from normal-map variance** (Toksvig or
LEAN/CLEAN mapping) so distant tiles converge to the right roughness instead of flickering. This
is cheaper and more robust than relying on TAA alone, and TAA + specular AA together are the
standard AAA combination.

---

## 4. Lighting: analytic lights + image-based lighting

A surface's final radiance = Σ(analytic lights) + ambient (IBL). Both run through the *same*
BRDF.

### 4.1 Analytic (punctual) lights — physical units
- Use **physical intensity units**: directional lights in lux (illuminance), punctual lights
  in lumens/candela. This makes a single exposure value work across a whole scene.
- **Reference values** (Frostbite) so the numbers mean something:

  | Source | Value |
  |---|---|
  | Full daylight (sun + sky) | ~100,000 lux |
  | Overcast midday | ~20,000 lux |
  | Sun just at horizon | ~400 lux |
  | Full moon, clear night | ~0.05–0.3 lux (≈1 lux only at the extreme) |
  | 40 W incandescent bulb | ~450 lumens |
  | 100 W incandescent bulb | ~1,600 lumens |

  Punctual intensity (candela) relates to luminous power: `I = Φ / (4π)` for an isotropic point
  light, `I = Φ / (2π(1−cosθ))` for a spotlight of half-angle θ.
- **Inverse-square falloff** with a smooth windowing function so the light reaches 0 at its
  radius without a hard clip:
  ```
  attenuation = (1 / max(d², 0.01²)) · saturate(1 − (d/radius)⁴)²
  ```
- Spot lights add an angular falloff (smoothstep between inner/outer cone).
- Each light evaluates `(diffuseBRDF + specularBRDF) · NoL · lightColor · intensity · attenuation · shadow`.

### 4.2 Image-Based Lighting (ambient/environment)
IBL provides the "everything else" lighting — the sky and environment reflected/absorbed. It
splits into two integrals:

**Diffuse irradiance** — precompute an *irradiance map* (small cubemap or SH L2 coefficients)
by convolving the environment with a cosine lobe. Spherical harmonics (9 RGB coeffs) is the
compact AAA choice.

**Specular radiance — the split-sum approximation (Karis/UE4):**
```
specularIBL ≈ prefilteredEnv(R, roughness) · (f0 · DFG.x + DFG.y)
```
- **Prefiltered environment map** — the env cubemap pre-convolved with GGX at several
  roughness levels, stored in mip chain (mip = roughness).
- **DFG / BRDF LUT** — a 2D lookup (`NoV` × `roughness`) of the environment BRDF integral,
  precomputed once, reused for all materials. Also feeds the energy-compensation term (§3.3).

For a game with a dynamic sky, the environment must be (cheaply) re-captured/re-convolved as
the sun moves — or use an *analytic* sky model (Hosek-Wilkie / Preetham) feeding a procedural
irradiance.

### 4.3 Occlusion of ambient
IBL is unshadowed by nature, so it must be attenuated by occlusion:
- **AO map** (baked) × **screen-space AO** (GTAO) for diffuse ambient. Combine multiple AO
  sources with `min()` or multiplicatively, not additively.
- **Specular occlusion** is *not* the same as diffuse AO — a fully occluded cavity can still
  catch a grazing reflection. Derive it from AO, roughness, and `NoV` rather than reusing the AO
  map directly (Lagarde's analytic form):
  ```glsl
  float specularOcclusion(float NoV, float ao, float roughness) {
      return saturate(pow(NoV + ao, exp2(-16.0*roughness - 1.0)) - 1.0 + ao);
  }
  ```
- **SSR** (§7) layers sharp local reflections on top of the (occluded) specular IBL, falling
  back to IBL where rays miss.
- A subtle but important correction: apply a **horizon-occlusion** factor so reflection rays that
  point *into* the surface (below the geometric horizon, common with strong normal maps) are
  faded out — otherwise normal-mapped surfaces leak light from behind them.

---

## 5. HDR, exposure, and tonemapping — the "HDR" in the title

This is what separates a flat renderer from a cinematic one.

### 5.1 Work in HDR linear
- All intermediate render targets are **floating point**: `R16G16B16A16_FLOAT` (or `R11G11B10`
  where alpha isn't needed) so radiance can exceed 1.0 — the sun, emissives, and bright
  speculars carry their true energy.
- Never clamp or sRGB-encode mid-pipeline. The first time values leave linear is the output
  transform (§5.4).

### 5.2 Auto-exposure (eye adaptation)
- Build a **luminance histogram** of the HDR frame in a compute pass.
- Compute average/log-average luminance, pick an exposure (key value) that maps the scene's
  mid-tone to a target, and **adapt over time** (exponential smoothing) so transitions from
  dark→bright feel like an eye/camera.
- Apply exposure as a single multiply *before* tonemapping. Optionally drive it from a
  physical camera model (EV100 from aperture/shutter/ISO) — Frostbite's approach.

### 5.3 Tonemapping operator
Maps unbounded HDR linear → [0,1] display range while preserving contrast and taming
highlights. Options, roughly in order of modern preference:
- **ACES** (Hill's fit / the RRT+ODT approximation) — the long-standing AAA filmic default;
  rich contrast, slightly saturated highlight shift.
- **AgX** — newer, gentler highlight desaturation (no neon clipping); increasingly preferred
  (Blender's default, used in recent titles).
- **Khronos PBR Neutral** — preserves albedo/hue, good for asset-accurate viewers.
- Older: Reinhard (washed out), Uncharted2/Hable (still fine).
  Avoid a bare Reinhard for a AAA look — pick ACES or AgX.

### 5.4 Output transform (OETF / display encode)
- **SDR:** encode linear → sRGB (the ~2.2 gamma OETF) as the very last step. If using an
  sRGB framebuffer the GPU does this for you — don't double-encode.
- **HDR displays:** skip the tonemap-to-SDR; instead map to the display via **PQ (HDR10)** or
  **scRGB**, in **Rec.2020** primaries, scaled to the display's nit range. This is where
  wide-gamut color management lives. **▶ OpenGL:** this is the assumption OpenGL breaks hardest —
  desktop GL has **no standard HDR swapchain at all** (no colorspace signaling), so HDR10
  output in practice requires DXGI/Vulkan interop. See §12.1.

### 5.5 Dithering — the last step before 8-bit

After tonemapping, a smooth HDR gradient quantized to an 8-bit sRGB framebuffer shows visible
**banding** (sky gradients, dark fog, vignettes are the usual offenders). Add a tiny
**triangular-PDF (TPDF) dither** of ±1 LSB, ideally from a blue-noise texture, *after* the OETF
and immediately before quantization:
```glsl
// outColor is already display-encoded (sRGB) in [0,1]
outColor += (blueNoise(gl_FragCoord.xy) - blueNoise(gl_FragCoord.xy + offset)) / 255.0;
```
This trades imperceptible noise for the elimination of contouring and is mandatory for a clean
look at 8-bit. (A true 10-bit/HDR output path mostly removes the need; 10-bit GL output is
possible on modern drivers but the default swapchain is 8-bit — see §12.1 — so for SharpCraft
dithering is not optional.)

### 5.6 Bloom (HDR energy bleed)

Bloom runs on **HDR linear color before tonemapping** (it represents the camera/eye scattering
energy from bright sources), not on the post-tonemap image. The modern recipe is the
**Call of Duty / Jimenez progressive dual-filter**, not a fixed-radius Gaussian:
- **Downsample** the HDR buffer through a mip pyramid with a 13-tap filter. On the *first*
  downsample apply a **Karis average** (weight each tap by `1/(1+luma)`) to stop single bright
  pixels from becoming firefly bloom flares.
- **Upsample** back up the pyramid with a 3×3 tent filter, *additively* accumulating each level.
- **Composite** by lerping the original HDR scene toward the bloom pyramid by a small factor
  (e.g. 0.04) — energy-preserving, resolution-independent, and naturally wide.

Avoid a hard "bright-pass threshold then blur": it clips energy, is resolution-dependent, and
flickers. A soft knee threshold (or none, relying on the lerp factor) is the AAA choice.

---

## 6. Precomputation / lookup tables (do these offline or at load)
- **BRDF/DFG LUT** — 2D, `NoV × roughness`, independent of the scene; bake once, ship it.
- **Irradiance SH or map** — per environment; recompute when the sky changes.
- **Prefiltered specular env mip chain** — per environment; recompute when the sky changes.
- **Blue-noise textures** — for dithering, TAA jitter, and SSAO/SSR sampling.

---

## 7. Screen-space effects (the polish layer)

| Effect | Inputs | Notes |
|---|---|---|
| **GTAO** (Ground-Truth AO) | depth + normal | Best modern SSAO; horizon-search, physically grounded; multiply into ambient diffuse |
| **SSR** (screen-space reflections) | depth + normal + roughness + prev HDR color | Ray-march depth buffer; fall back to IBL where rays miss/leave screen; roughness-aware blur |
| **Contact shadows** | depth | Short screen-space ray from each pixel toward the light; fills the gap CSMs miss at contact points |
| **SSGI** (optional) | depth + normal + prev color | One-bounce diffuse GI in screen space |

All of these are *approximations that fail off-screen* — they augment, never replace, IBL and
shadow maps.

---

## 8. Shadows

- **Directional (sun): Cascaded Shadow Maps (CSM).** Split the view frustum into 3–4 cascades
  along Z; render a depth map per cascade from the light. Near cascades get high effective
  resolution, far ones cover more area. Stabilize cascades (texel-snapping) to kill shimmer.
- **Filtering:** PCF (multi-tap) for soft edges; **PCSS / contact-hardening** for
  distance-varying penumbra (sharp at contact, soft far away).
- **Punctual lights:** cube/dual-paraboloid shadow maps; cull aggressively via the cluster grid.
- **Contact shadows** (§7) patch the small-scale leak CSMs can't resolve.
- **State of the art:** UE5 **Virtual Shadow Maps** (one huge virtual page-cached map) and
  distance-field/ray-traced shadows — aspirational, not a starting requirement.

---

## 9. Anti-aliasing

- **TAA is the AAA default.** Jitter the projection matrix sub-pixel each frame; reproject the
  previous frame using a **motion-vector buffer**; blend with neighborhood **variance
  clipping** (in YCoCg) to reject ghosting; bicubic history sampling; apply a **negative mip
  bias** so textures aren't over-blurred, then a light sharpen pass.
  TAA *requires* motion vectors and sub-pixel jitter to be designed in from the start — it is
  not a bolt-on. This is the single most architecture-invasive AA choice. **▶ OpenGL:** the
  jitter is applied by perturbing the projection matrix in clip space, which on GL must respect
  the `[-1,1]` (or clip-control-adjusted) NDC convention and the bottom-left origin when sampling
  the history buffer — see §12.2/§12.3.
- **MSAA** — clean but expensive and awkward with deferred; viable with forward+.
- **FXAA / SMAA** — cheap spatial fallback, often applied to UI or as a TAA complement.
- **Upscalers** (DLSS/FSR/XeSS/TSR) are temporal-AA supersets and the current frontier; they
  reuse the same motion-vector + jitter infrastructure.

---

## 10. Color management throughout
- Author **albedo/base-color and emissive textures as sRGB** (decode to linear on sample).
- Author **data textures (normal, roughness, metallic, AO) as linear** — never sRGB-decode them.
- Do all math in linear, ideally tracking working-space primaries (sRGB/Rec.709 linear for
  SDR; Rec.2020 for HDR output).
- Only the final OETF/output transform converts to display encoding.

**▶ OpenGL:** GL gives you sRGB conversion *for free in hardware* and it's easy to
double-apply by accident — see §12.3. Sample albedo from `GL_SRGB8_ALPHA8` textures (auto-decode
to linear), keep normal/roughness/metallic as `GL_RGBA8`/`GL_RG8` (linear), and let
`GL_FRAMEBUFFER_SRGB` do the final encode — but then your tonemap shader must output *linear*, not
sRGB, or you gamma the image twice.

---

## 11. Minimum viable AAA pipeline — recommended build order

A pragmatic sequence to stand this up without boiling the ocean:

0. **Device & depth foundation** — fp16 render targets, **reversed-Z + `glClipControl` (§12.2)**,
   `GL_FRAMEBUFFER_SRGB` policy decided (§12.3), DSA + a KHR_debug callback wired up (§12.5).
   These are cheap to set up first and *very* expensive to retrofit.
1. **Linear HDR core** — sRGB texture sampling, a tonemap (ACES/AgX) + sRGB output, dither
   before quantization (§5.5). *Get linear-light correctness before anything else.*
2. **Core metallic-roughness BRDF** — GGX D + Smith-correlated V + Schlick F + Lambert diffuse,
   one directional (sun) light in physical-ish units.
3. **IBL** — DFG LUT + irradiance (SH) + prefiltered specular env; split-sum; energy
   compensation. This is the biggest single jump in visual quality.
4. **Shadows** — CSM for the sun, PCF filtering, stabilized cascades.
5. **Clustered light culling** — froxel grid + compute light assignment; add punctual lights.
6. **Auto-exposure** — luminance histogram + temporal adaptation.
7. **TAA** — jitter + motion vectors + reprojection (design motion vectors in from step 1).
8. **Screen-space polish** — GTAO, then SSR, then contact shadows.
9. **Post stack** — bloom (threshold-free dual-filter down/upsample with Karis average, §5.6),
   then DoF/motion blur.
10. **Volumetrics & extended material lobes** — as needed.

Each step is independently shippable and visually validates the one before it.

---

## 12. The OpenGL dimension — how it changes the assumptions

§§2–11 are written API-agnostically, the way the reference literature (Filament, Frostbite, the
SIGGRAPH courses) is. Almost all of it ports to OpenGL unchanged: the BRDF math, IBL, clustered
culling, TAA, tonemapping, and color management are GLSL-portable and GPU-feature-driven, not
API-driven. SharpCraft targets **OpenGL 4.5 core** (Silk.NET, GLSL `#version 450 core`), which is
a *good* GL baseline — it has compute shaders, SSBOs, image load/store, Direct State Access, and
clip control all in core. The pieces below are the assumptions that **do** change, ordered by how
much they bite.

### 12.1 HDR display output — the assumption that genuinely breaks (§5.4)

This is the one place the theory simply does not hold on OpenGL. The whole "§5.4 → PQ/HDR10 or
scRGB in Rec.2020" path **does not exist in desktop OpenGL**: there is no standard HDR swapchain
and no `DXGI_COLOR_SPACE` equivalent — no way to tell the OS/compositor the framebuffer is PQ or
scRGB. (Plain **10-bit** `R10G10B10A2` output is no longer the blocker it once was: it was
historically Quadro/FirePro-only, but NVIDIA enabled 30-bit OpenGL on GeForce/TITAN with the
431.70 Studio driver in July 2019 and AMD followed on Radeon. Extra buffer depth without
colorspace signaling is still SDR, though — not HDR.) Practical consequences:

- **SDR is the only first-class GL output.** Tonemap to `[0,1]`, encode sRGB (let
  `GL_FRAMEBUFFER_SRGB` do it), dither to 8-bit (§5.5). Everything *upstream* (fp16 linear HDR
  pipeline, exposure, tonemap) is unchanged and worth building — only the final display hop is
  capped.
- **To actually light up an HDR monitor** you must hand the final fp16 image to a different API
  that owns the swapchain: **GL↔D3D interop** (`WGL_NV_DX_interop2` — share a DXGI flip-model
  scRGB/HDR10 backbuffer) or render in GL and present via Vulkan. This is a real,
  ship-it-later option, not a fantasy, but it is a separate subsystem.
- **Decision for SharpCraft:** treat HDR10 output as out of scope for the core rebuild. Build the
  full HDR-linear interior pipeline (it's the 95% that gives the look), output SDR sRGB+dither,
  and leave a clean seam at the present step for a future interop path.

### 12.2 Depth: NDC range & reversed-Z (§1 depth, §7 SSR/SSAO, §9 TAA)

OpenGL's default clip space is **`z ∈ [-1, 1]`** (D3D/Vulkan use `[0,1]`). Naively flipping to
reversed-Z on top of `[-1,1]` *gains nothing* — the precision you want near the far plane is
destroyed when depth is mapped through `-1`. You must switch the convention:

```c
glClipControl(GL_LOWER_LEFT, GL_ZERO_TO_ONE);   // core in GL 4.5 (ARB_clip_control)
// then: reversed projection (near→1, far→0), glDepthFunc(GL_GREATER), glClearDepth(0.0)
```

Knock-on effects that touch the rest of the pipeline:
- The projection matrix changes (custom reversed-Z perspective, infinite-far variant is popular).
- Every screen-space pass that **reconstructs view/world position from depth** (SSAO, SSR,
  volumetrics, contact shadows) must use the reversed mapping.
- `GL_LOWER_LEFT` keeps GL's native bottom-left framebuffer origin (see §12.3); changing only the
  Z range and not the origin is the usual intent.
- Depth clamping (`GL_DEPTH_CLAMP`) is still useful to keep shadow casters from clipping the near
  plane.

For a voxel world with a multi-hundred-block view distance, this is not optional polish — it's the
difference between clean distant geometry and z-fighting.

### 12.3 Coordinate-origin & sRGB conventions (§10, IBL §4.2, §9 TAA)

- **Texture origin is bottom-left** in GL (D3D is top-left). This flips: UV `v` on loaded images,
  reads of the framebuffer/G-buffer in screen-space passes, and TAA history sampling. Pick one
  convention and apply it consistently; most engines flip images at load and treat `(0,0)` as
  bottom-left everywhere.
- **`GL_FRAMEBUFFER_SRGB`** makes the GPU encode linear→sRGB on write to an sRGB-format default
  framebuffer/attachment. Free and correct — but the #1 source of "everything looks washed out /
  too dark" bugs is **double gamma**: tonemap shader outputting sRGB *and* the framebuffer
  encoding again. Rule: if `GL_FRAMEBUFFER_SRGB` is on and the target is sRGB-format, your shader
  writes **linear**.
- **Cubemap face convention.** GL cubemaps follow the legacy RenderMan **left-handed** convention,
  so the six faces and especially `+Y/−Y` are oriented differently than you'd expect from a
  right-handed world. This is the classic "my IBL irradiance/prefilter map is flipped or rotated"
  bug. Get the sampling/face-render directions right once in the IBL bake (§4.2) and verify
  against a known HDRI.

### 12.4 No bindless / no descriptor sets — feeding materials (§2 clustered, §3 materials)

Core GL 4.5 has **no bindless textures** (it's the `ARB_bindless_texture` extension, well
supported on desktop NVIDIA/AMD but not guaranteed) and no Vulkan-style descriptor sets. For a
voxel game with many block materials this shapes the data path:
- **Texture arrays / array-of-2D (`GL_TEXTURE_2D_ARRAY`)** are the portable answer: pack all block
  albedo/normal/MRA into a few arrays and index by layer — perfect for uniform-size voxel tiles.
- Per-draw/per-material constants go in **UBOs** (small, fast, `std140`) or **SSBOs** (large,
  flexible, `std430`); clustered light lists live in SSBOs.
- **Mind `std140` packing:** `vec3` aligns to 16 bytes and array elements pad to `vec4` — the most
  common GL uniform bug. Prefer `std430` (SSBO) or hand-pack to `vec4` to avoid surprises. (This is
  a likely culprit in any "uniform data is garbage" issue.)
- **AZDO** (Approaching Zero Driver Overhead): persistent-mapped buffers (`GL_MAP_PERSISTENT_BIT`),
  multi-draw-indirect, and instancing are how GL closes the draw-call gap with modern APIs — the
  relevant pattern for pushing many chunks.

### 12.5 Threading, state, and tooling

- **Single-threaded context.** GL commands must run on the thread owning the context. Background
  chunk meshing/streaming can build CPU-side vertex data on worker threads, but the actual buffer
  uploads must be marshaled to the GL thread (or done through a **shared context** with its own
  caveats). This matches SharpCraft's existing "delete GL resources on the GL thread" notes and is
  a real constraint on the streaming design — Vulkan/D3D12's free-threaded resource creation
  doesn't exist here.
- **Use Direct State Access (DSA, core 4.5):** `glCreateTextures`/`glTextureStorage2D`/
  `glNamedBufferData` etc. avoid the bind-to-edit state-machine dance, fewer bugs, cleaner code.
- **Explicit binding points:** prefer `layout(binding=N)` in GLSL 4.2+ for samplers/UBOs/SSBOs so
  binding is declared in-shader, not chased in C#.
- **Debugging:** wire up a **`KHR_debug`** callback for synchronous, human-readable driver errors
  (vastly better than polling `glGetError`), and use **RenderDoc** for frame capture. GLSL has no
  offline bytecode at the 4.5 target (SPIR-V ingestion only entered core with GL 4.6 /
  `ARB_gl_spirv`), so shaders compile per-driver — expect
  **driver-specific GLSL quirks** (NVIDIA is lenient, Mesa/AMD stricter) and test across vendors.
- **Precision qualifiers** (`highp` etc.) are no-ops on desktop GL — ignore them unless a GLES
  path is ever added.

### 12.6 What does *not* change (so you don't over-worry)

The BRDF (§3), IBL split-sum + DFG LUT (§4.2/§6), analytic light units (§4.1), clustered culling
math (§2), HDR-linear interior + exposure + tonemap (§5.1–5.3), GTAO/SSR (§7), CSM (§8), and TAA
neighborhood-clipping (§9) are all **GPU-capability features, not API features** — they're as
available in GL 4.5 compute/fragment shaders as in D3D12. The GL-specific work is concentrated in
*setup and plumbing* (depth convention, sRGB/origin, output, resource binding, threading), not in
the shading algorithms themselves.

---

## Sources

- [Physically Based Rendering in Filament (google.github.io)](https://google.github.io/filament/Filament.md.html) — BRDF formulas, remaps, energy compensation, clear coat/aniso/cloth, defaults
- [Filament — Physically Based Rendering Theory](https://www.mintlify.com/google/filament/concepts/pbr-theory)
- [LearnOpenGL — PBR Theory](https://learnopengl.com/PBR/Theory) and [Deferred Shading](https://learnopengl.com/Advanced-Lighting/Deferred-Shading)
- [Moving Frostbite to Physically Based Rendering v3 (Lagarde & de Rousiers, SIGGRAPH 2014)](https://seblagarde.files.wordpress.com/2015/07/course_notes_moving_frostbite_to_pbr_v32.pdf) — physical light units, energy conservation, camera/exposure
- [Crash Course in BRDF Implementation — Jakub Boksansky](https://boksajak.github.io/files/CrashCourseBRDF.pdf)
- [Designing a Linear HDR Rendering Pipeline — kosmonaut's blog](https://kosmonautblog.wordpress.com/2017/03/26/designing-a-linear-hdr-pipeline/)
- [Unity URP — G-buffer layout (Deferred / Deferred+)](https://docs.unity3d.com/6000.4/Documentation/Manual/urp/rendering/g-buffer-layout.html)
- [Forward vs Deferred Shading — Unreal Art Optimization](https://unrealartoptimization.github.io/book/pipelines/forward-vs-deferred/)
- [Temporal AA and the Quest for the Holy Trail — The Code Corsair](https://www.elopezr.com/temporal-aa-and-the-quest-for-the-holy-trail/) and [Intel GameTechDev TAA](https://github.com/GameTechDev/TAA)
- [LearnOpenGL — Cascaded Shadow Maps](https://learnopengl.com/Guest-Articles/2021/CSM) and [MJP — A Sampling of Shadow Techniques](https://therealmjp.github.io/posts/shadow-maps/)
- [Cascaded Shadow Maps with Soft Shadows — Alex Tardif](https://alextardif.com/shadowmapping.html)
- [Virtual Shadow Maps in Unreal Engine 5](https://dev.epicgames.com/documentation/en-us/unreal-engine/virtual-shadow-maps-in-unreal-engine)

**OpenGL dimension (§12) & gap-fills:**
- [Reversed-Z in OpenGL — nlguillemot](https://nlguillemot.wordpress.com/2016/12/07/reversed-z-in-opengl/) and [Reverse Z (and why it's so awesome) — Tom Hulton-Harrop](https://tomhultonharrop.com/posts/reverse-z/) — `glClipControl(GL_LOWER_LEFT, GL_ZERO_TO_ONE)`, why `[-1,1]` NDC breaks naive reversed-Z
- [How to render to HDR displays on Windows 10 — pyromuffin](https://www.pyromuffin.com/2018/07/how-to-render-to-hdr-displays-on.html) and [Programming HDR monitor support in Direct3D — asawicki](https://www.asawicki.info/news_1703_programming_hdr_monitor_support_in_direct3d) — scRGB/HDR10 swapchains live in DXGI; desktop GL has no colorspace-signaling equivalent
- [WGL_NV_DX_interop2 spec](https://registry.khronos.org/OpenGL/extensions/NV/WGL_NV_DX_interop2.txt) — share a DXGI HDR backbuffer with a GL renderer (the GL→HDR-output escape hatch)
- [Khronos OpenGL Wiki — Direct State Access](https://www.khronos.org/opengl/wiki/Direct_State_Access), [Interface Block / std140 & std430 layout](https://www.khronos.org/opengl/wiki/Interface_Block_(GLSL)#Memory_layout), [KHR_debug](https://www.khronos.org/opengl/wiki/Debug_Output)
- [Approaching Zero Driver Overhead (AZDO) — Cass Everitt et al., GDC 2014](https://www.khronos.org/assets/uploads/developers/library/2014-gdc/Khronos-OpenGL-Efficiency-GDC-Mar14.pdf) — persistent-mapped buffers, MDI, instancing
- [Improved Geometric Specular Antialiasing — Tokuyoshi & Kaplanyan (2019)](https://www.activision.com/cdn/research/Improved_Geometric_Specular_AA_2019.pdf) and [Filament §4.10 specular AA](https://google.github.io/filament/Filament.md.html#materialsystem/specularantialiasing)
- [Next Generation Post Processing in Call of Duty: Advanced Warfare — Jorge Jimenez, SIGGRAPH 2014](https://www.iryoku.com/next-generation-post-processing-in-call-of-duty-advanced-warfare/) — dual-filter bloom + Karis average firefly suppression
- [Moving Frostbite to PBR (Lagarde) — specular occlusion & physical light unit reference values](https://seblagarde.files.wordpress.com/2015/07/course_notes_moving_frostbite_to_pbr_v32.pdf)
- [Clustered Shading — Olsson, Billeter & Assarsson (2012)](https://efficientshading.com/wp-content/uploads/clustered_shading_preprint.pdf) and [A Primer on Clustered Forward/Deferred — Angel Ortiz](https://www.aortiz.me/2018/12/21/CG.html) — exponential Z-slice equation, compute light assignment
