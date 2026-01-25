using System.Numerics;
using SharpCraft.Sdk.UI;

namespace SharpCraft.Client.UI;

public class AtmosphereControlHud : IInteractiveHud
{
    public string Name => "AtmosphereControl";

    private bool _isVisible = true;

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible != value)
            {
                _isVisible = value;
                OnVisibilityChanged?.Invoke();
            }
        }
    }

    public event Action? OnVisibilityChanged;

    public void OnAwake() { }

    public void OnUpdate(double deltaTime) { }

    public void Draw(double deltaTime, IGui gui, IHudContext context)
    {
        if (!_isVisible) return;
        if (context is not HudContext clientContext) return;

        var post = clientContext.PostProcessing;
        var worldTime = clientContext.Lighting?.WorldTime;

        if (worldTime == null || post == null) return;

        gui.SetNextWindowSize(new Vector2(420, 600), GuiCond.FirstUseEver);
        var open = _isVisible;
        if (gui.Begin("Atmosphere & Time Control", ref open))
        {
            IsVisible = open;
            
            // ================================================================
            // TIME OF DAY
            // ================================================================
            if (gui.CollapsingHeader("Time of Day"))
            {
                var time = worldTime.Time;
                var duration = worldTime.DayDurationInMinutes;
                var isPaused = worldTime.IsPaused;
                
                var formattedTime = worldTime.FormattedTime;
                gui.Text($"Current Time: {formattedTime}");
                
                // Twilight phase indicator
                // Sun direction.Y = Sin(angle), but direction points FROM sun TO scene
                // So sun elevation = -Sin(angle): at noon (1.5π), Sin=-1, elevation=1 (high)
                var sunAngle = worldTime.SunAngle;
                var sunElevation = -MathF.Sin(sunAngle);
                var phase = GetTwilightPhase(sunElevation);
                gui.Text($"Phase: {phase}", GetTwilightColor(phase));
                
                gui.Checkbox("Pause Time", ref isPaused);
                worldTime.IsPaused = isPaused;

                if (gui.Button("Sync to 10m Day")) duration = 10f;
                gui.SameLine();
                if (gui.Button("Sync to 1h Day")) duration = 60f;
                
                gui.SliderFloat("Day Duration (min)", ref duration, 1f, 120f);
                if (Math.Abs(duration - worldTime.DayDurationInMinutes) > 0.01f)
                {
                    worldTime.DayDurationInMinutes = duration;
                }

                var maxTime = duration * 60f;
                gui.SliderFloat("Time (seconds)", ref time, 0f, maxTime);
                if (Math.Abs(time - worldTime.Time) > 0.01f)
                {
                    worldTime.Time = time;
                }
                
                gui.Spacing();
                
                // Quick time presets
                gui.Text("Quick Presets:");
                if (gui.Button("Dawn")) SetTimeToHour(worldTime, 5.5f);
                gui.SameLine();
                if (gui.Button("Sunrise")) SetTimeToHour(worldTime, 6.0f);
                gui.SameLine();
                if (gui.Button("Morning")) SetTimeToHour(worldTime, 9.0f);
                gui.SameLine();
                if (gui.Button("Noon")) SetTimeToHour(worldTime, 12.0f);
                
                if (gui.Button("Afternoon")) SetTimeToHour(worldTime, 15.0f);
                gui.SameLine();
                if (gui.Button("Sunset")) SetTimeToHour(worldTime, 18.0f);
                gui.SameLine();
                if (gui.Button("Dusk")) SetTimeToHour(worldTime, 19.0f);
                gui.SameLine();
                if (gui.Button("Night")) SetTimeToHour(worldTime, 22.0f);
            }

            gui.Spacing();

            // ================================================================
            // ATMOSPHERIC SCATTERING
            // ================================================================
            if (gui.CollapsingHeader("Atmospheric Scattering"))
            {
                gui.Text("Scattering Parameters:");
                
                var scatteringG = post.ScatteringG;
                var density = post.DensityMultiplier;
                var extinction = post.ExtinctionMultiplier;

                gui.SliderFloat("Mie Anisotropy (G)", ref scatteringG, 0.0f, 0.99f);
                gui.SliderFloat("Density", ref density, 0.0f, 0.1f);
                gui.SliderFloat("Extinction", ref extinction, 0.0f, 0.05f);

                post.ScatteringG = scatteringG;
                post.DensityMultiplier = density;
                post.ExtinctionMultiplier = extinction;
                
                gui.Spacing();
                gui.Text("Atmosphere Composition:");
                
                var rayleigh = post.RayleighScale;
                var mie = post.MieScale;
                var ozone = post.OzoneScale;
                
                gui.SliderFloat("Rayleigh Scale", ref rayleigh, 0.0f, 3.0f);
                gui.SliderFloat("Mie Scale", ref mie, 0.0f, 3.0f);
                gui.SliderFloat("Ozone Scale", ref ozone, 0.0f, 3.0f);
                
                post.RayleighScale = rayleigh;
                post.MieScale = mie;
                post.OzoneScale = ozone;
                
                gui.Spacing();
                gui.Text("Volumetric Lighting:");
                
                var volumetricIntensity = post.VolumetricIntensity;
                var samples = post.VolumetricSamples;

                gui.SliderFloat("Intensity", ref volumetricIntensity, 0.0f, 3.0f);
                gui.SliderInt("Samples", ref samples, 8, 128);

                post.VolumetricIntensity = volumetricIntensity;
                post.VolumetricSamples = samples;
                
                // Presets
                gui.Spacing();
                gui.Text("Atmosphere Presets:");
                if (gui.Button("Earth"))
                {
                    post.RayleighScale = 1.0f;
                    post.MieScale = 1.0f;
                    post.OzoneScale = 1.0f;
                    post.ScatteringG = 0.8f;
                }
                gui.SameLine();
                if (gui.Button("Mars"))
                {
                    post.RayleighScale = 0.2f;
                    post.MieScale = 2.5f;
                    post.OzoneScale = 0.0f;
                    post.ScatteringG = 0.65f;
                }
                gui.SameLine();
                if (gui.Button("Hazy"))
                {
                    post.RayleighScale = 1.0f;
                    post.MieScale = 3.0f;
                    post.OzoneScale = 0.5f;
                    post.ScatteringG = 0.9f;
                }
                gui.SameLine();
                if (gui.Button("Clear"))
                {
                    post.RayleighScale = 1.2f;
                    post.MieScale = 0.3f;
                    post.OzoneScale = 1.5f;
                    post.ScatteringG = 0.76f;
                }
            }
            
            gui.Spacing();
            
            // ================================================================
            // TONE MAPPING & EXPOSURE
            // ================================================================
            if (gui.CollapsingHeader("Tone Mapping"))
            {
                var toneMapMode = post.ToneMapMode;
                
                gui.Text($"Current: {GetToneMapName(toneMapMode)}");
                
                if (gui.Button("ACES Filmic")) toneMapMode = 0;
                gui.SameLine();
                if (gui.Button("Cinematic")) toneMapMode = 1;
                gui.SameLine();
                if (gui.Button("Reinhard")) toneMapMode = 2;
                
                post.ToneMapMode = toneMapMode;
            }
            
            gui.Spacing();

            // ================================================================
            // BLOOM
            // ================================================================
            if (gui.CollapsingHeader("Bloom"))
            {
                var bloomIntensity = post.BloomIntensity;
                var bloomThreshold = post.BloomThreshold;
                
                gui.SliderFloat("Intensity", ref bloomIntensity, 0.0f, 1.0f);
                gui.SliderFloat("Threshold", ref bloomThreshold, 0.0f, 5.0f);
                
                post.BloomIntensity = bloomIntensity;
                post.BloomThreshold = bloomThreshold;
            }
            
            gui.Spacing();
            
            // ================================================================
            // POST-PROCESSING EFFECTS
            // ================================================================
            if (gui.CollapsingHeader("Post-Processing Effects"))
            {
                var vignette = post.VignetteIntensity;
                var chromatic = post.ChromaticAberration;
                
                gui.SliderFloat("Vignette", ref vignette, 0.0f, 1.0f);
                gui.SliderFloat("Chromatic Aberration", ref chromatic, 0.0f, 0.02f);
                
                post.VignetteIntensity = vignette;
                post.ChromaticAberration = chromatic;
            }

            gui.End();
        }
    }
    
    private static void SetTimeToHour(Sdk.Universe.IWorldTime worldTime, float hour)
    {
        // Convert hour (0-24) to the correct angle-based time
        // The sun angle system uses: 6 AM = π, 12 PM = 1.5π, 6 PM = 2π/0, 12 AM = 0.5π
        // NormalizedTime maps: 6 AM → 0, 12 PM → 0.25, 6 PM → 0.5, 12 AM → 0.75
        var normalizedTime = ((hour - 6f + 24f) % 24f) / 24f;
        var angle = normalizedTime * MathF.PI * 2f + MathF.PI;
        if (angle >= MathF.PI * 2f) angle -= MathF.PI * 2f;
        
        var timeScale = (MathF.PI * 2f) / (worldTime.DayDurationInMinutes * 60f);
        worldTime.Time = angle / timeScale;
    }
    
    private static string GetTwilightPhase(float sunElevation)
    {
        if (sunElevation > 0.0f) return "Day";
        if (sunElevation > -0.105f) return "Civil Twilight";
        if (sunElevation > -0.208f) return "Nautical Twilight";
        if (sunElevation > -0.309f) return "Astronomical Twilight";
        return "Night";
    }
    
    private static Vector4 GetTwilightColor(string phase)
    {
        return phase switch
        {
            "Day" => new Vector4(1.0f, 0.9f, 0.4f, 1.0f),
            "Civil Twilight" => new Vector4(1.0f, 0.6f, 0.3f, 1.0f),
            "Nautical Twilight" => new Vector4(0.6f, 0.4f, 0.7f, 1.0f),
            "Astronomical Twilight" => new Vector4(0.3f, 0.3f, 0.6f, 1.0f),
            "Night" => new Vector4(0.2f, 0.2f, 0.4f, 1.0f),
            _ => new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
        };
    }
    
    private static string GetToneMapName(int mode)
    {
        return mode switch
        {
            0 => "ACES Filmic",
            1 => "Cinematic",
            2 => "Reinhard",
            _ => "Unknown"
        };
    }
}
