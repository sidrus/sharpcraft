namespace SharpCraft.Sdk.Universe;

/// <summary>
/// Defines the properties and methods for managing in-game time.
/// </summary>
public interface IWorldTime
{
    /// <summary>
    /// Gets the current game time in seconds.
    /// </summary>
    float Time { get; }

    /// <summary>
    /// Gets or sets the duration of one full day in minutes.
    /// </summary>
    float DayDurationInMinutes { get; set; }

    /// <summary>
    /// Gets the current sun angle in radians [0, 2PI].
    /// </summary>
    float SunAngle { get; }

    /// <summary>
    /// Gets the formatted game time string (e.g., "08:30 AM").
    /// </summary>
    string FormattedTime { get; }

    /// <summary>
    /// Gets the normalized time of day [0, 1], where 0 is 6 AM.
    /// </summary>
    float NormalizedTime { get; }
}
