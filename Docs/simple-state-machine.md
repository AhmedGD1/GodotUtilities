[← back to README](../README.md)

# SimpleStateMachine&lt;T&gt;

`GodotUtilities`

A small, allocation-light finite state machine keyed by an `enum`, with `OnEnter`/`OnUpdate`/`OnExit` callbacks per state — a lighter alternative to a full state-machine node tree when you just need clean branching logic for something like a character or an enemy AI.

```csharp
public enum PlayerState { Idle, Run, Jump }

private readonly SimpleStateMachine<PlayerState> _fsm = new();

public override void _Ready()
{
    _fsm.AddState(PlayerState.Idle)
        .OnEnter(() => _sprite.Play("idle"));

    _fsm.AddState(PlayerState.Run)
        .OnEnter(() => _sprite.Play("run"))
        .OnUpdate(delta => Move());

    _fsm.AddState(PlayerState.Jump)
        .OnEnter(() => Velocity = Vector2.Up * jumpForce)
        .OnExit(() => GD.Print("Landed"));

    _fsm.SetInitialState(PlayerState.Idle);
}

public override void _PhysicsProcess(double delta)
{
    _fsm.Update(delta);

    if (Input.IsActionJustPressed("jump"))
        _fsm.ChangeState(PlayerState.Jump);
}
```

- `AddState(id)` — registers a new state under an enum value and returns a `State<T>` builder; throws if `id` is already registered. Chain `.OnEnter(...)`, `.OnUpdate(delta => ...)`, `.OnExit(...)` on the result to attach callbacks (all optional).
- `SetInitialState(id)` — sets the starting state and invokes its `OnEnter`, without going through `ChangeState` (so no `StateChanged` event fires, and there's no "previous" state).
- `ChangeState(id)` — exits the current state (`OnExit`), enters the new one (`OnEnter`), resets `StateElapsed` to `0`, and raises `StateChanged`. Throws `ArgumentException` if `id` isn't registered. No-ops if `Locked` is `true`.
- `Update(delta)` — call once per frame/physics tick; invokes the current state's `OnUpdate` and accumulates `StateElapsed`.
- `Locked` — when `true`, `ChangeState` calls are silently ignored (useful for freezing transitions during a cutscene or hitstop).
- `CurrentState` / `PreviousState` — the `State<T>` instances themselves (or `null`); `GetCurrentId()` / `GetPreviousId()` return just the enum value (`default` if unset).
- `StateElapsed` — seconds since the current state was entered.
- `StateChanged` — event raised on every successful `ChangeState`, with the previous and new state ids. Not raised by `SetInitialState`.

[← back to README](../README.md)
