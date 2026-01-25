using System.Numerics;
using SharpCraft.Sdk.Lifecycle;
using SharpCraft.Sdk.Rendering;
using SharpCraft.Sdk.Universe;

namespace SharpCraft.Client.Rendering.Lighting;

/// <summary>
/// Updates the sun's direction and intensity based on the world time.
/// </summary>
public class Sun(IWorldTime worldTime, ILightingSystem lightingSystem) : ILifecycle
{
    public void OnUpdate(double deltaTime)
    {
        var angle = worldTime.SunAngle;
        
        // Update sun direction
        // Normalize vector to ensure it's a unit vector
        // angle: 6 AM = PI, 12 PM = 1.5 PI, 6 PM = 2 PI
        // We want:
        // 6 AM: Rising in East (+X) -> Direction from Sun is (-1, 0, 0)
        // 12 PM: At zenith (+Y) -> Direction from Sun is (0, -1, 0)
        // 6 PM: Setting in West (-X) -> Direction from Sun is (1, 0, 0)
        
        // Z axis is North (-Z) / South (+Z).
        // Since sun rises in East (+X) and sets in West (-X), Z remains 0.
        
        // At angle = PI (6 AM), Cos(PI) = -1, Sin(PI) = 0. direction = (-1, 0, 0). Correct.
        // At angle = 1.5 PI (12 PM), Cos(1.5 PI) = 0, Sin(1.5 PI) = -1. direction = (0, -1, 0). Correct.
        // At angle = 2 PI (6 PM), Cos(2 PI) = 1, Sin(2 PI) = 0. direction = (1, 0, 0). Correct.
        var direction = Vector3.Normalize(new Vector3(MathF.Cos(angle), MathF.Sin(angle), 0.0f));
        lightingSystem.Sun.Direction = direction;

        // Calculate intensity based on time of day
        // 6 AM is sunrise (angle = PI), 6 PM is sunset (angle = 2PI)
        // 7 AM is angle = PI + (PI/12)
        // 5:30 PM is angle = 2PI - (PI/24)
        
        // We can use normalized angle to make it easier [0, 2PI] where PI is 6AM
        var normalizedAngle = angle % (MathF.PI * 2);
        if (normalizedAngle < 0) normalizedAngle += MathF.PI * 2;
        
        // Shift so 12 AM (midnight) is 0
        // PI is 6 AM, so shift by PI to get 6 AM at PI, then subtract 0.5PI to get 12 AM at 0?
        // Actually, current implementation says:
        // PI = 6 AM, 1.5 PI = 12 PM, 2 PI = 6 PM, 0.5 PI = 12 AM
        
        var intensity = 0.0f;
        
        // 5:30 AM is sunrise start (PI - PI/12)
        // 6 AM is sun breaches horizon (PI)
        // 7 AM is full intensity (PI + PI/12)
        // 5 PM is fade start (2PI - PI/6)
        // 6 PM is sun dips below horizon (2PI)
        // 6:30 PM is darkness (2PI + PI/24)
        
        const float twilightStart = MathF.PI - (MathF.PI / 8.0f); // ~4:30 AM
        const float sunrise = MathF.PI;
        const float fullDayStart = MathF.PI + (MathF.PI / 12.0f);
        const float fullDayEnd = (MathF.PI * 2.0f) - (MathF.PI / 12.0f);
        const float sunset = MathF.PI * 2.0f;
        const float twilightEnd = (MathF.PI * 2.0f) + (MathF.PI / 8.0f); // ~7:30 PM
        
        if (normalizedAngle >= twilightStart && normalizedAngle < fullDayStart)
        {
            // Start at 0 at twilightStart, reach 1.0 at fullDayStart
            intensity = (normalizedAngle - twilightStart) / (fullDayStart - twilightStart);
            // Apply a curve to keep it brighter longer/start earlier
            intensity = MathF.Pow(intensity, 0.7f);
        }
        else if (normalizedAngle is >= fullDayStart and < fullDayEnd)
        {
            intensity = 1.0f;
        }
        else if (normalizedAngle is >= fullDayEnd and < twilightEnd)
        {
            intensity = 1.0f - (normalizedAngle - fullDayEnd) / (twilightEnd - fullDayEnd);
            intensity = MathF.Pow(intensity, 0.7f);
        }

        lightingSystem.Sun.Intensity = intensity;
    }
}
