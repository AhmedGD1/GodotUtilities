namespace GodotUtilities;

/// <summary>
/// A lightweight manually-updated countdown timer.
/// Call <see cref="Tick(double)"/> every update to advance the timer.
/// </summary>
/// <param name="duration">The default duration, in seconds.</param>
public struct Countdown(double duration)
{
    private double currentDuration;
    private double remaining;

    public readonly bool IsFinished => remaining <= 0.0;
    public readonly double TimeLeft => Math.Max(0.0, remaining);

    public readonly float Progress => currentDuration != 0.0
        ? (float)(1.0 - (remaining / currentDuration))
        : 1f;

    public void Stop() => remaining = 0.0;
    public void Start() => remaining = currentDuration = duration;

    public void Start(double sec) => remaining = currentDuration = sec;
    public void Tick(double dt) => remaining = Math.Max(0.0, remaining - dt);
}
