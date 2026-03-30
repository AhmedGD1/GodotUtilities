using System.Collections.Generic;
using System;
using Godot;

namespace Utilities;

public abstract class PooledTweener<T> where T : PooledTweener<T>, new()
{
    private static readonly Stack<T> pool = new();

    public void Release() => pool.Push((T)this);
    public static T Get() => pool.Count > 0 ? pool.Pop() : new();
}

public static class PunchMath
{
    public static float ElasticOut(float x)
    {
        if (x == 0f || x == 1f) return x;

        float period = 0.3f;
        float s      = period / 4f;

        return Mathf.Pow(2f, -10f * x) *
               Mathf.Sin((x - s) * Mathf.Tau / period) + 1f;
    }
}

#region Shake Position & Rotation

public sealed class ShakePositionTweener : PooledTweener<ShakePositionTweener>
{
    private Action<Vector2> setPos;
    private Vector2         originalPos;
    private float           strength;
    private float           frequency;

    public void Setup(Node2D target, float strength, float frequency = 25f)
        => Setup(() => target.Position, v => target.Position = v, strength, frequency);

    public void Setup(Control target, float strength, float frequency = 25f)
        => Setup(() => target.Position, v => target.Position = v, strength, frequency);

    private void Setup(Func<Vector2> get, Action<Vector2> set, float strength, float frequency)
    {
        setPos         = set;
        originalPos    = get();
        this.strength  = strength;
        this.frequency = frequency;
    }

    public void Tick(float t)
    {
        float decay  = 1f - t;
        float noiseT = t * frequency;

        Vector2 shake = new(
            FastNoise(noiseT, 12.34f),
            FastNoise(noiseT, 78.12f)
        );

        setPos(originalPos + shake * strength * decay);

        if (t >= 1f) { setPos(originalPos); Release(); }
    }

    private static float FastNoise(float x, float seed)
    {
        float v = Mathf.Sin(x * 12.9898f + seed * 78.233f) * 43758.5453f;
        return (v - Mathf.Floor(v)) * 2f - 1f;
    }
}

public sealed class ShakeRotationTweener : PooledTweener<ShakeRotationTweener>
{
    private Action<float> setRot;
    private float         originalRot;
    private float         strength;
    private float         frequency;

    public void Setup(Node2D target, float strength, float frequency = 30f)
        => Setup(() => target.Rotation, v => target.Rotation = v, strength, frequency);

    public void Setup(Control target, float strength, float frequency = 30f)
        => Setup(() => target.Rotation, v => target.Rotation = v, strength, frequency);

    private void Setup(Func<float> get, Action<float> set, float strength, float frequency)
    {
        setRot          = set;
        originalRot     = get();
        this.strength   = strength;
        this.frequency  = frequency;
    }

    public void Tick(float t)
    {
        float decay = 1f - t;
        float noise = Mathf.Sin(t * frequency * 5f);

        setRot(originalRot + noise * strength * decay);

        if (t >= 1f) { setRot(originalRot); Release(); }
    }
}

#endregion

#region Punch Position

public sealed class PunchPositionTweener : PooledTweener<PunchPositionTweener>
{
    private Action<Vector2> setPos;
    private Vector2         originalPos;
    private Vector2         punch;

    public void Setup(Node2D target, Vector2 amount)
        => Setup(() => target.Position, v => target.Position = v, amount);

    public void Setup(Control target, Vector2 amount)
        => Setup(() => target.Position, v => target.Position = v, amount);

    private void Setup(Func<Vector2> get, Action<Vector2> set, Vector2 amount)
    {
        setPos      = set;
        originalPos = get();
        punch       = amount;
    }

    public void Tick(float t)
    {
        float p = PunchMath.ElasticOut(1f - t);
        setPos(originalPos + punch * p);

        if (t >= 1f) { setPos(originalPos); Release(); }
    }
}

#endregion

#region Punch Rotation

public sealed class PunchRotationTweener : PooledTweener<PunchRotationTweener>
{
    private Action<float> setRot;
    private float         originalRot;
    private float         punch;

    public void Setup(Node2D target, float degrees)
        => Setup(() => target.Rotation, v => target.Rotation = v, degrees);

    public void Setup(Control target, float degrees)
        => Setup(() => target.Rotation, v => target.Rotation = v, degrees);

    private void Setup(Func<float> get, Action<float> set, float degrees)
    {
        setRot      = set;
        originalRot = get();
        punch       = Mathf.DegToRad(degrees);
    }

    public void Tick(float t)
    {
        float p = PunchMath.ElasticOut(1f - t);
        setRot(originalRot + punch * p);

        if (t >= 1f) { setRot(originalRot); Release(); }
    }
}

#endregion

#region Punch Scale

public sealed class PunchScaleTweener : PooledTweener<PunchScaleTweener>
{
    private Action<Vector2> setScale;
    private Vector2         originalScale;
    private Vector2         punch;

    public void Setup(Node2D target, Vector2 amount)
        => Setup(() => target.Scale, v => target.Scale = v, amount);

    public void Setup(Control target, Vector2 amount)
        => Setup(() => target.Scale, v => target.Scale = v, amount);

    private void Setup(Func<Vector2> get, Action<Vector2> set, Vector2 amount)
    {
        setScale      = set;
        originalScale = get();
        punch         = amount;
    }

    public void Tick(float t)
    {
        float p = PunchMath.ElasticOut(1f - t);
        setScale(originalScale + punch * p);

        if (t >= 1f) { setScale(originalScale); Release(); }
    }
}

#endregion

#region Flicker

public sealed class FlickerTweener : PooledTweener<FlickerTweener>
{
    private CanvasItem target;
    private float      min;
    private float      max;
    private float      threshold;

    public void Setup(CanvasItem target, float min, float max, float threshold)
    {
        this.target    = target;
        this.min       = min;
        this.max       = max;
        this.threshold = threshold;
    }

    public void Tick(float t)
    {
        Color color = target.Modulate with
        {
            A = MathUtil.RandfRange(min, max) > threshold ? 1f : 0f
        };

        target.Modulate = color;

        if (t >= 1f) { target.Modulate = target.Modulate with { A = 1f }; Release(); }
    }
}

#endregion

#region Orbit

public sealed class OrbitTweener : PooledTweener<OrbitTweener>
{
    private Action<Vector2> setPos;
    private Vector2         center;
    private float           radius;
    private float           direction;
    private float           startAngle;

    public void Setup(Node2D target, Vector2 center, float radius, float direction)
        => Setup(v => target.Position = v, target.Position, center, radius, direction);

    public void Setup(Control target, Vector2 center, float radius, float direction)
        => Setup(v => target.Position = v, target.Position, center, radius, direction);

    private void Setup(Action<Vector2> set, Vector2 currentPos, Vector2 center, float radius, float direction)
    {
        this.setPos    = set;
        this.center    = center;
        this.radius    = radius;
        this.direction = direction;
        this.startAngle = (currentPos - center).Angle();
    }

    public void Tick(float t)
    {
        float angle = startAngle + direction * t * Mathf.Tau;
        setPos(center + Vector2.FromAngle(angle) * radius);

        if (t >= 1f) Release();
    }
}

#endregion