using Godot;

namespace GodotUtilities;

/// <summary>
/// Math and random helpers for Godot, including framerate-independent interpolation and random sampling.
/// </summary>
public static class MathUtil
{
    /// <summary>
    /// The shared random number generator used by all random helpers in this class.
    /// It is randomized on startup and replaced when <see cref="SeedRNG(ulong)"/> is called.
    /// </summary>
    public static RandomNumberGenerator RNG { get; private set; } = new();

    static MathUtil() => RNG.Randomize();

    /// <summary>
    /// Replaces <see cref="RNG"/> with a new generator using the given seed,
    /// making all following random results reproducible.
    /// </summary>
    /// <param name="seed">The seed for the new generator.</param>
    public static void SeedRNG(ulong seed) => RNG = new() { Seed = seed };

    #region Expo Lerp

    /// <summary>
    /// Exponentially interpolates from <paramref name="a"/> toward <paramref name="b"/>,
    /// framerate-independent given a variable <paramref name="dt"/>.
    /// </summary>
    /// <param name="a">The current value.</param>
    /// <param name="b">The target value.</param>
    /// <param name="dt">delta time.</param>
    /// <param name="accel">Controls how quickly the value approaches <paramref name="b"/>; higher is faster.</param>
    /// <returns>The interpolated value.</returns>
    public static float ExpoLerp(float a, float b, double dt, float accel)
    {
        float t = 1f - Mathf.Exp(-accel * (float)dt);
        return Mathf.Lerp(a, b, t);
    }

    /// <inheritdoc cref="ExpoLerp(float, float, double, float)"/>
    public static Vector2 ExpoLerp(Vector2 a, Vector2 b, double dt, float accel)
    {
        float t = 1f - Mathf.Exp(-accel * (float)dt);
        return a.Lerp(b, t);
    }

    /// <inheritdoc cref="ExpoLerp(float, float, double, float)"/>
    public static Vector3 ExpoLerp(Vector3 a, Vector3 b, double dt, float accel)
    {
        float t = 1f - Mathf.Exp(-accel * (float)dt);
        return a.Lerp(b, t);
    }

    #endregion

    #region Clamp

    /// <summary>
    /// Clamps <paramref name="value"/> to the range [0, 1].
    /// </summary>
    public static float Clamp01(float value) => Mathf.Clamp(value, 0f, 1f);

    /// <inheritdoc cref="Clamp01(float)"/>
    public static double Clamp01(double value) => Mathf.Clamp(value, 0.0, 1.0);

    #endregion

    #region Random

    /// <summary>
    /// Returns a random unit-length 2D direction.
    /// </summary>
    /// <returns>A normalized <see cref="Vector2"/> pointing in a random direction.</returns>
    public static Vector2 GetRandomDirection()
    {
        return Vector2.FromAngle(RNG.Randf() * Mathf.Tau);
    }

    /// <summary>
    /// Returns a uniformly distributed random point inside a circle centered on the origin.
    /// </summary>
    /// <param name="radius">The radius of the circle.</param>
    /// <returns>A random point whose distance from the origin is at most <paramref name="radius"/>.</returns>
    public static Vector2 GetRandomPointInCircle(float radius)
    {
        float distance = Mathf.Sqrt(RNG.Randf()) * radius;
        return GetRandomDirection() * distance;
    }

    /// <summary>
    /// Returns a uniformly distributed random unit-length 3D direction.
    /// </summary>
    /// <returns>A normalized <see cref="Vector3"/> pointing in a random direction.</returns>
    public static Vector3 GetRandomDirection3D()
    {
        float phi = RNG.Randf() * Mathf.Tau;
        float z = RNG.RandfRange(-1f, 1f);
        float r = Mathf.Sqrt(1f - z * z);

        return new Vector3(r * Mathf.Cos(phi), r * Mathf.Sin(phi), z);
    }

    /// <summary>
    /// Returns a uniformly distributed random point inside a sphere centered on the origin.
    /// </summary>
    /// <param name="radius">The radius of the sphere.</param>
    /// <returns>A random point whose distance from the origin is at most <paramref name="radius"/>.</returns>
    public static Vector3 RandomPointInSphere(float radius)
    {
        float r = radius * Mathf.Pow(RNG.Randf(), 1f / 3f);
        return GetRandomDirection3D() * r;
    }

    /// <summary>
    /// Returns the result of a fair coin flip
    /// </summary>
    /// <returns><c>true</c> or <c>false</c> with equal probability.</returns>
    public static bool CoinFlip() => RNG.Randi() % 2 == 0;

    /// <summary>
    /// Randomly returns <c>true</c> with the given probability.
    /// </summary>
    /// <param name="probability">The chance of success, from 0 (never) to 1 (always).</param>
    /// <returns><c>true</c> if the roll succeeds; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="probability"/> is outside [0, 1].</exception>
    public static bool Chance(float probability)
    {
        if (probability > 1f || probability < 0f)
            throw new ArgumentOutOfRangeException(nameof(probability), "has to be between [0, 1]");
            
        return RNG.Randf() < probability;
    }

    /// <summary>
    /// Picks a random element from <paramref name="items"/> using <see cref="RNG"/>.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="items">The items to pick from. Must not be empty.</param>
    /// <returns>A randomly selected element from <paramref name="items"/>.</returns>
    public static T PickRandom<T>(params T[] items) => items[RNG.RandiRange(0, items.Length - 1)];

    /// <inheritdoc cref="PickRandom{T}(T[])"/>
    public static T PickRandom<T>(IReadOnlyList<T> items) => items[RNG.RandiRange(0, items.Count - 1)];

    /// <summary>
    /// Shuffles <paramref name="items"/> in place (Fisher-Yates) using <see cref="RNG"/>.
    /// </summary>
    public static void Shuffle<T>(IList<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = RNG.RandiRange(0, i);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }

    #endregion
}

