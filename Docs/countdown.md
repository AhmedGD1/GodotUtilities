[← back to README](../README.md)

# Countdown

`GodotUtilities`

A minimal, manually-ticked countdown timer struct — a lightweight alternative to a `Timer` node when you don't need signals or scene-tree presence (e.g. per-enemy cooldowns tracked in plain C# state).

```csharp
private Countdown _dodgeCooldown = new(duration: 1.5);

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

- `Start()` — (re)starts using the duration passed to the constructor.
- `Start(seconds)` — (re)starts with a custom duration for this run.
- `Tick(dt)` — call once per frame/tick to count down; clamps at zero.
- `Stop()` — immediately ends it (`IsFinished` becomes `true`).
- `IsFinished` — `true` once time has run out.
- `TimeLeft` — remaining seconds.
- `Progress` — `0..1`, how far through the countdown you are (useful for UI cooldown fills).

> `Countdown` is a `struct`. If you default-construct it (`new Countdown()`, or as an uninitialized field/array element) instead of supplying a duration, `Progress` will always read `1f` until you call `Start(seconds)` explicitly.

[← back to README](../README.md)
