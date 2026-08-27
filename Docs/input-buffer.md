[← back to README](../README.md)

# InputBuffer

`GodotUtilities`

Tracks short-lived "buffered" inputs so an action pressed slightly too early (e.g. jump pressed a few frames before landing) can still be consumed within a configurable time window, instead of being lost.

```csharp
private readonly InputBuffer _inputBuffer = new();

public override void _PhysicsProcess(double delta)
{
    if (Input.IsActionJustPressed("jump"))
        _inputBuffer.BufferAction("jump", duration: 0.15); // buffer for 150ms

    _inputBuffer.Tick(delta); // advance/expire buffered actions every tick

    if (IsOnFloor() && _inputBuffer.TryConsume("jump"))
        Jump();
}
```

- `BufferAction(name, duration)` — starts (or refreshes) a buffer window for an action name (`StringName`). Throws `ArgumentOutOfRangeException` if `duration <= 0`.
- `TryConsume(name)` — if the action is still within its buffer window, removes and returns `true`; otherwise `false`.
- `Has(name)` — checks validity without consuming.
- `Tick(dt)` — must be called once per frame/physics tick to count down and expire buffered actions.
- `ConsumeAll()` — clears every currently buffered action.

[← back to README](../README.md)
