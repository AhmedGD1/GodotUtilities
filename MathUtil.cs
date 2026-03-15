using Godot;

namespace Utilities;

public static class MathUtil
{
    private static RandomNumberGenerator RNG { get; } = new();

    static MathUtil()
    {
        RNG.Randomize();
    }

    public static float ExponentialLerp(float from, float to, float dt, float weight)
    {
        float smoothing = 1f - Mathf.Exp(-weight * dt);
        return Mathf.Lerp(from, to, smoothing);
    }

    public static Vector2 ExponentialLerp(Vector2 from, Vector2 to, float dt, float weight)
    {
        float smoothing = 1f - Mathf.Exp(-weight * dt);
        return from.Lerp(to, smoothing);
    }

    public static Vector2 RandomUnit()
    {
        float angle = RNG.Randf() * Mathf.Tau;
        return Vector2.FromAngle(angle);
    }

    public static Vector2 RandomOnCircle(float radius)
    {
        return RandomUnit() * radius;
    }

    public static Vector2 RandomInCircle(float radius)
    {
        float r = Mathf.Sqrt(RNG.Randf()) * radius;
        return RandomUnit() * r;
    }

    public static float Clamp01(float value)
    {
        return Mathf.Clamp(value, 0f, 1f);
    }

    public static bool CoinFlip()
    {
        return (int)GD.Randi() % 2 == 0;
    }

    public static float RandfRange(float min, float max)
    {
        return RNG.RandfRange(min, max);
    }

    public static int RandRange(int min, int max)
    {
        return RNG.RandiRange(min, max);
    }

    public static bool Chance(float probability)
    {
        return RNG.Randf() < probability;
    }

    public static T RandomPick<T>(params T[] items)
    {
        if(items.Length == 0) return default;
        return items[(int)RNG.Randf() % items.Length];
    }

    public static T WeightedPick<T>(params (T item, float weight)[] entries)
    {
        float total = 0;

        foreach (var (_, w) in entries)
            total += w;
        
        float r   = RNG.Randf() * total;
        float acc = 0f;

        foreach (var (item, weight) in entries)
        {
            acc += weight;

            if (r < acc)
                return item;
        }

        return entries[^1].item;
    }

    public static float Progress(float value, float total)
    {
        if (total <= 0f) return 1f;
        return Clamp01(value / total);
    }
}

