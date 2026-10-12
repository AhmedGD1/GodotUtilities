[← back to README](../README.md)

# MathUtil

`GodotUtilities`

General-purpose math helpers: framerate-independent lerping, clamping, and RNG-based utilities, all built on a single shared `RandomNumberGenerator`.

```csharp
// Smoothly approach a target value regardless of frame rate (Good for top-down player movement)
_currentSpeed = MathUtil.ExpoLerp(_currentSpeed, targetSpeed, delta, accel: 8f);

// Random helpers
Vector2 dir = MathUtil.GetRandomDirection();
Vector2 spawn = center + MathUtil.GetRandomPointInCircle(radius: 64f);
bool heads = MathUtil.CoinFlip();
if (MathUtil.Chance(0.25f)) DropRareItem();
var pick = MathUtil.PickRandom("a", "b", "c");
```

## Interpolation and clamping

- `ExpoLerp(a, b, dt, accel)` — exponential interpolation toward a target that behaves consistently regardless of frame rate (unlike a naive `Lerp(a, b, factor)` per frame). Available for `float`, `Vector2`, and `Vector3`; higher `accel` approaches faster.
- `Clamp01(value)` — shorthand for clamping to `[0, 1]` (`float`/`double`).

**How it works:** `ExpoLerp` computes an interpolation factor as `1 - e^(-accel * dt)` and feeds that into `Mathf.Lerp` / `Vector2.Lerp` / `Vector3.Lerp` — because this factor depends on elapsed time rather than being a fixed per-call constant, the approach rate stays consistent whether your game runs at 30 FPS or 144 FPS.

## Random

All of these draw from `MathUtil.RNG`.

| Member | Description |
| --- | --- |
| `GetRandomDirection()` | A random unit `Vector2` (uniformly distributed angle). |
| `GetRandomPointInCircle(radius)` | A random `Vector2` inside a circle of that radius around the origin, uniformly distributed by area. |
| `GetRandomDirection3D()` | A random unit `Vector3`, uniformly distributed over the sphere. |
| `RandomPointInSphere(radius)` | A random `Vector3` inside a sphere of that radius around the origin, uniformly distributed by volume. |
| `CoinFlip()` | 50/50 `bool`. |
| `Chance(probability)` | `true` with the given probability (`0..1`); throws `ArgumentOutOfRangeException` outside that range. |
| `PickRandom(items)` | A random element from a `params T[]` or an `IReadOnlyList<T>` (so `List<T>` and arrays work). Throws on an empty collection. |

## The shared RNG

- `RNG` — the shared `RandomNumberGenerator` instance used by all the random methods above. It is randomized when `MathUtil` is first used.
- `SeedRNG(seed)` — replaces `RNG` with a new generator using that seed, so future random calls are deterministic (e.g. for reproducible runs/tests). Because it's a *new* instance, don't cache `MathUtil.RNG` in a field if you plan to reseed — read it again, or pass your own generator where an API accepts one (see [`WeightedLootTable<T>`](weighted-loot-table.md), which falls back to `MathUtil.RNG` at pick time).

Some Godot-backed helpers elsewhere use Godot's global random functions instead and aren't affected by `SeedRNG` — for example [`Countdown.StartRandom`](countdown.md).

[← back to README](../README.md)