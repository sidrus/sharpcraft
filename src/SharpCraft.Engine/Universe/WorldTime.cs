using SharpCraft.Sdk.Lifecycle;
using SharpCraft.Sdk.Universe;

namespace SharpCraft.Engine.Universe;

/// <summary>
/// Manages the progression of in-game time.
/// </summary>
public class WorldTime : IWorldTime, ILifecycle
{
    private float _time;
    private float _dayDurationInMinutes = 10f;

    public float Time => _time;

    public float DayDurationInMinutes
    {
        get => _dayDurationInMinutes;
        set
        {
            var oldDuration = _dayDurationInMinutes;
            _dayDurationInMinutes = value;
            // Adjust time to maintain the same normalized time/angle
            _time = _time * (_dayDurationInMinutes / oldDuration);
        }
    }

    public WorldTime()
    {
        // Start at 6 AM (PI radians)
        // angle = _time * ((2PI) / (duration * 60))
        // PI = _time * ((2PI) / (duration * 60))
        // 1 = _time * (2 / (duration * 60))
        // _time = (duration * 60) / 2
        _time = (_dayDurationInMinutes * 60f) / 2f;
    }

    public float SunAngle
    {
        get
        {
            var timeScale = (MathF.PI * 2f) / (DayDurationInMinutes * 60f);
            var angle = Time * timeScale;
            var normalizedAngle = angle % (MathF.PI * 2);
            if (normalizedAngle < 0) normalizedAngle += MathF.PI * 2;
            return normalizedAngle;
        }
    }

    public string FormattedTime
    {
        get
        {
            var displayHours = GetDisplayHours();
            var hours = (int)displayHours;
            var minutes = (int)((displayHours - hours) * 60);
            var amPm = hours >= 12 ? "PM" : "AM";
            var hours12 = hours % 12;
            if (hours12 == 0) hours12 = 12;

            return $"{hours12:D2}:{minutes:D2} {amPm}";
        }
    }

    public float NormalizedTime
    {
        get
        {
            var angle = SunAngle;
            var timeShift = (angle - MathF.PI);
            if (timeShift < 0) timeShift += MathF.PI * 2;
            return timeShift / (MathF.PI * 2);
        }
    }

    public void OnUpdate(double deltaTime)
    {
        _time += (float)deltaTime;
    }

    private float GetDisplayHours()
    {
        var totalHours = NormalizedTime * 24.0f;
        return (totalHours + 6.0f) % 24.0f;
    }
}
