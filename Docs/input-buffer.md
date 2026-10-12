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

- `BufferAction(name, duration)` — starts a buffer window of `duration` seconds for an action name (`StringName`). If the action is already buffered, its remaining time is reset to `duration`. Throws `ArgumentOutOfRangeException` if `duration <= 0`.
- `TryConsume(name)` — if the action is still within its buffer window, removes it and returns `true`; otherwise `false`.
- `Has(name)` — checks validity without consuming.
- `Tick(dt)` — must be called once per frame/physics tick to count down and expire buffered actions (an action expires when its remaining time reaches zero).
- `ConsumeAll()` — clears every currently buffered action, valid or not.

Any number of different actions can be buffered at once. `InputBuffer` is a class, so share one instance across everything that should see the same buffer, or give each character its own.

[← back to README](../README.md)