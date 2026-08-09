[← back to README](../README.md)

# MathUtil

`GodotUtilities`

General-purpose math helpers: framerate-independent lerping, clamping, and RNG-based utilities, all built on a single shared `RandomNumberGenerator`.

```csharp
// Smoothly approach a target value regardless of frame rate
_currentSpeed = MathUtil.ExpoLerp(_currentSpeed, targetSpeed, delta, accel: 8f);

// Random helpers
Vector2 dir = MathUtil.RandomDirection();
bool heads = MathUtil.CoinFlip();
if (MathUtil.Chance(0.25f)) DropRareItem();
var pick = MathUtil.PickRandom("a", "b", "c");
```

- `ExpoLerp(a, b, dt, accel)` — exponential interpolation toward a target that behaves consistently regardless of frame rate (unlike a naive `Lerp(a, b, factor)` per frame). Available for `float` and `Vector2`.
- `Clamp01(value)` — shorthand for clamping to `[0, 1]` (`float`/`double`).
- `RandomDirection()` — a random 2D unit vector.
- `CoinFlip()` — 50/50 `bool`.
- `Chance(probability)` — `true` with the given probability (`0..1`); throws `ArgumentOutOfRangeException` outside that range.
- `PickRandom(items)` — picks a random element from a `params T[]` or `List<T>`. Assumes a non-empty collection.
- `RNG` — the shared `RandomNumberGenerator` instance used by all random methods above; call `SeedRNG(seed)` to make future random calls deterministic (e.g. for reproducible runs/tests).

**How it works:** `ExpoLerp` computes an interpolation factor as `1 - e^(-accel * dt)` and feeds that into `Mathf.Lerp` / `Vector2.Lerp` — because this factor depends on elapsed time rather than being a fixed per-call constant, the approach rate stays consistent whether your game runs at 30 FPS or 144 FPS.

[← back to README](../README.md)
