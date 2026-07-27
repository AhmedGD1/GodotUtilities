using Godot;

namespace GodotUtilities;

/// <summary>
/// General-purpose math helpers: framerate-independent lerping, clamping,
/// normalization, and RNG-based utilities built on a shared <see cref="RandomNumberGenerator"/>.
/// </summary>
public static class MathUtil
{
    /// <summary>
    /// The shared random number generator used by the random-utility methods in this class.
    /// Seeded automatically on first use; call <see cref="SeedRNG"/> to use a fixed seed instead.
    /// </summary>
    public static RandomNumberGenerator RNG { get; private set; } = new();

    static MathUtil() => RNG.Randomize();

    /// <summary>
    /// Replaces <see cref="RNG"/> with a new generator seeded with <paramref name="seed"/>,
    /// producing deterministic output from subsequent random calls.
    /// </summary>
    /// <param name="seed">The seed value to use.</param>
    public static void SeedRNG(ulong seed) => RNG = new() { Seed = seed };

    #region Lerp

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

    #endregion

    #region Clamp

    /// <summary>
    /// Clamps <paramref name="value"/> to the range [0, 1].
    /// </summary>
    public static float Clamp01(float value) => Mathf.Clamp(value, 0f, 1f);

    /// <inheritdoc cref="Clamp01(float)"/>
    public static double Clamp01(double value) => Mathf.Clamp(value, 0.0, 1.0);

    #endregion

    #region Normalize

    /// <summary>
    /// Divides <paramref name="value"/> by <paramref name="length"/> and clamps the result to [0, 1].
    /// </summary>
    /// <param name="value">The value to normalize.</param>
    /// <param name="length">The length representing 1.0.</param>
    public static float Normalize(float value, float length)
    {
        return Mathf.Clamp(value / length, 0f, 1f);
    }

    /// <inheritdoc cref="Normalize(float, float)"/>
    public static float Normalize(double value, double length)
    {
        return (float)Mathf.Clamp(value / length, 0.0, 1.0);
    }

    #endregion

    #region Random

    /// <summary>
    /// Returns a random 2D unit direction vector using <see cref="RNG"/>.
    /// </summary>
    public static Vector2 RandomDirection()
    {
        return Vector2.FromAngle(RNG.Randf() * Mathf.Tau);
    }

    /// <summary>
    /// Returns the result of a fair coin flip
    /// </summary>
    /// <returns><c>true</c> or <c>false</c> with equal probability.</returns>
    public static bool CoinFlip() => RNG.Randi() % 2 == 0;

    public static bool Chance(float probability)
    {
        if (probability > 1f || probability < 0f)
            GD.PushWarning("Probability value has to be between [0, 1]");
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
    public static T PickRandom<T>(List<T> items) => items[RNG.RandiRange(0, items.Count - 1)];

    #endregion
}
