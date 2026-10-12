[← back to README](../README.md)

# Countdown

`GodotUtilities`

A minimal, manually-ticked countdown timer struct — a lightweight alternative to a `Timer` node when you don't need signals or scene-tree presence (e.g. per-enemy cooldowns tracked in plain C# state).

```csharp
private Countdown _dodgeCooldown = new(1.5);   // default duration: 1.5 seconds

public override void _PhysicsProcess(double delta)
{
    _dodgeCooldown.Tick(delta);

    if (Input.IsActionJustPressed("dodge") && _dodgeCooldown.IsFinished)
    {
        Dodge();
        _dodgeCooldown.Start(); // restart using the default duration
    }
}
```

A new `Countdown` starts out **stopped** (`IsFinished` is `true`) until you call `Start()` — so the first dodge above is available immediately.

`Tick` also tells you when the countdown expires, which is handy for one-shot effects without polling `IsFinished`:

```csharp
if (_stunTimer.Tick(delta))
    EndStun();
```

## Members

- `new Countdown(baseDuration)` — the default duration, in seconds, used by `Start()`.
- `Start()` — (re)starts using the duration passed to the constructor.
- `Start(seconds)` — (re)starts with a custom duration for this run.
- `StartRandom(min, max)` — (re)starts with a random duration between `min` and `max`.
- `Tick(dt)` — call once per frame/tick to count down. Returns `true` only on the tick where the countdown reaches zero; it returns `false` before that, and on every call after it has finished. Negative `dt` is treated as zero.
- `Stop()` — immediately ends it (`IsFinished` becomes `true`). It doesn't trigger a `true` from `Tick`.
- `IsFinished` — `true` once time has run out, or if it was never started.
- `TimeLeft` — remaining seconds, never below `0`.
- `Progress` — `0..1`, how far through the countdown you are: `0` right after `Start`, `1` when finished or stopped. For a UI cooldown fill that drains, use `1 - Progress`.

## Gotchas

> `Countdown` is a `struct`. Store it in a normal (non-`readonly`) field, as above: `Tick` mutates it, and on a `readonly` field or a copy the call would act on a throwaway copy and the real timer would never advance.

> If you default-construct it (`new Countdown()`, or as an uninitialized field/array element) instead of supplying a duration, its base duration is `0`, so `Progress` always reads `1f` until you call `Start(seconds)` explicitly.


[← back to README](../README.md)