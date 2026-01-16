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
        var direction = Vector3.Normalize(new Vector3(MathF.Cos(angle), MathF.Sin(angle), 0.5f));
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
        
        // 6 AM (PI) to 7 AM (PI + PI/12) -> Fade in
        // 7 AM to 5:30 PM (2PI - PI/24) -> Full intensity (1.0)
        // 5:30 PM to 6 PM (2PI) -> Fade out
        
        const float sunrise = MathF.PI;
        const float fullDayStart = MathF.PI + (MathF.PI / 12.0f);
        const float fullDayEnd = (MathF.PI * 2.0f) - (MathF.PI / 24.0f);
        const float sunset = MathF.PI * 2.0f;
        
        if (normalizedAngle is >= sunrise and < fullDayStart)
        {
            intensity = (normalizedAngle - sunrise) / (fullDayStart - sunrise);
        }
        else if (normalizedAngle is >= fullDayStart and < fullDayEnd)
        {
            intensity = 1.0f;
        }
        else if (normalizedAngle is >= fullDayEnd and < sunset)
        {
            intensity = 1.0f - (normalizedAngle - fullDayEnd) / (sunset - fullDayEnd);
        }

        lightingSystem.Sun.Intensity = intensity;
    }
}
