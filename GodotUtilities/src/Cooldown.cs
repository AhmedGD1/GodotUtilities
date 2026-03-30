using Godot;

namespace Utilities;

public struct Cooldown
{
    private readonly float duration;

    private float currentDuration;
    private float timer;

    public readonly bool  IsReady   => timer <= 0f;
    public readonly float Remaining => Mathf.Max(0f, timer);
    public readonly float Progress  => MathUtil.Progress(currentDuration - timer, currentDuration);

    public Cooldown(float duration)
    {
        this.duration   = duration;
        timer           = 0f;
        currentDuration = duration;
    }

    public void Start(float sec) => timer = currentDuration = sec;
    public void Start()          => timer = currentDuration = duration;
    public void Stop()           => timer = 0f;

    public void Tick(float dt)
    {
        if (timer > 0f)
            timer -= dt;
    }

    public void Tick(double dt)
    {
        if (timer > 0f)
            timer -= (float)dt;
    }
}

