using System.Collections.Generic;
using Godot;

namespace Utilities;

public abstract class PooledTweener<T> where T : PooledTweener<T>, new()
{
    private static readonly Stack<T> pool = new();

    public void Release() => pool.Push((T)this);
    public static T Get() => pool.Count > 0 ? pool.Pop() : new();
}

#region Math

public static class ShakeMath
{
    public static (float x, float y, float z) Calculate(FastNoiseLite noise, float t, float speed, float strength)
    {
        float envelope = 1f - Mathf.Pow(t, 3f);
        float scaled   = t * speed;

        return (
            noise.GetNoise1D(scaled)         * strength * envelope,
            noise.GetNoise1D(scaled + 1000f) * strength * envelope,
            noise.GetNoise1D(scaled + 2000f) * strength * envelope
        );
    }
}

public static class PunchMath
{
    public static float Envelope(float t) => Mathf.Sin(t * Mathf.Pi);
}

#endregion

#region Shake Position

// Note: A sealed class in C# is a class that cannot be inherited.
public sealed class ShakePositionTweener : PooledTweener<ShakePositionTweener>
{
    public Vector2 Origin => origin;

    private FastNoiseLite noise;
    private Node2D        target;
    private Vector2       origin;
    private float         strength;
    private float         speed;

    public void Setup(Node2D target, float strength, float speed)
    {
        this.target   = target;
        this.origin   = target.Position;
        this.strength = strength;
        this.speed    = speed;

        noise ??= new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
            Seed      = (int)(GD.Randi() % 1000)
        };
    }

    public void Tick(float t)
    {
        var (x, y, _) = ShakeMath.Calculate(noise, t, speed, strength);
        target.Position = origin + new Vector2(x, y);

        if (t >= 1f) { target.Position = origin; Release(); }
    }
}

public sealed class ShakePosition3DTweener : PooledTweener<ShakePosition3DTweener>
{
    public Vector3 Origin => origin;

    private FastNoiseLite noise;
    private Node3D        target;
    private Vector3       origin;
    private float         strength;
    private float         speed;

    public void Setup(Node3D target, float strength, float speed)
    {
        this.target   = target;
        this.origin   = target.Position;
        this.strength = strength;
        this.speed    = speed;

        noise ??= new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
            Seed      = (int)(GD.Randi() % 1000)
        };
    }

    public void Tick(float t)
    {
        var (x, y, z) = ShakeMath.Calculate(noise, t, speed, strength);
        target.Position = origin + new Vector3(x, y, z);

        if (t >= 1f) { target.Position = origin; Release(); }
    }
}

#endregion

#region Punch

public sealed class PunchTweener2D : PooledTweener<PunchTweener2D>
{
    private Node2D  target;
    private Vector2 originPosition;
    private Vector2 originScale;
    private Vector2 punchPosition;
    private Vector2 punchScale;

    public void Setup(Node2D target, Vector2 punchPosition, Vector2 punchScale)
    {
        this.target         = target;
        this.punchPosition  = punchPosition;
        this.punchScale     = punchScale;

        originScale         = target.Scale;
        originPosition      = target.Position;
    }

    public void Tick(float t)
    {
        float envelope  = PunchMath.Envelope(t);
        target.Position = originPosition + punchPosition * envelope;
        target.Scale    = originScale    + punchScale    * envelope;

        if (t >= 1f) 
        { 
            target.Position = originPosition; 
            target.Scale    = originScale; 
            Release(); 
        }
    }
}

public sealed class PunchTweener3D : PooledTweener<PunchTweener3D>
{
    private Node3D  target;
    private Vector3 originPosition;
    private Vector3 originScale;
    private Vector3 punchPosition;
    private Vector3 punchScale;

    public void Setup(Node3D target, Vector3 punchPosition, Vector3 punchScale)
    {
        this.target         = target;
        this.punchPosition  = punchPosition;
        this.punchScale     = punchScale;

        originPosition      = target.Position;
        originScale         = target.Scale;
    }

    public void Tick(float t)
    {
        float envelope = PunchMath.Envelope(t);

        target.Position = originPosition + punchPosition * envelope;
        target.Scale    = originScale    + punchScale    * envelope;

        if (t >= 1f) 
        { 
            target.Position = originPosition; 
            target.Scale = originScale; 
            Release(); 
        }
    }
}

#endregion

#region Flicker

public sealed class FlickerTweener : PooledTweener<FlickerTweener>
{
    private CanvasItem target;
    private float min;
    private float max;
    private float threshold;

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
    private Node2D target;
    private Vector2 center;
    private float radius;
    private float direction;

    private float startAngle;

    public void Setup(Node2D target, Vector2 center, float radius, float direction)
    {
        this.target    = target;
        this.center    = center;
        this.radius    = radius;
        this.direction = direction;

        startAngle = (target.Position - center).Angle();
    }

    public void Tick(float t)
    {
        float angle     = startAngle + direction * t * Mathf.Tau;
        target.Position = center + Vector2.FromAngle(angle) * radius;

        if (t >= 1f) Release();
    }
}

#endregion

