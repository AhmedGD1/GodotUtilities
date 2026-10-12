[← back to README](../README.md)

# SimpleStateMachine&lt;T&gt;

`GodotUtilities`

A small, light finite state machine keyed by an `enum`, with `OnEnter`/`OnUpdate`/`OnExit` callbacks per state and optional condition-based transitions — a lighter alternative to a full state-machine node tree when you just need clean branching logic for something like a character or an enemy AI.

> Need something more advanced? Check out [TakobiAI](https://github.com/AhmedGD1/takobi_ai).

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

## States

- `AddState(id)` — registers a new state under an enum value and returns a `State<T>` builder; throws `ArgumentException` if `id` is already registered. Chain `.OnEnter(...)`, `.OnUpdate(delta => ...)`, `.OnExit(...)` on the result to attach callbacks (all optional).
- `SetInitialState(id)` — sets the starting state and invokes its `OnEnter`, without raising `StateChanged` (there's no "previous" state). **Call it exactly once, before `Update`/`ChangeState`**: a second call throws `InvalidOperationException`, and `ChangeState` before it throws a `NullReferenceException`.
- `ChangeState(id)` — exits the current state (`OnExit`), enters the new one (`OnEnter`), resets `StateElapsed` to `0`, and raises `StateChanged`. Throws `ArgumentException` if `id` isn't registered. It's a no-op if `id` is already the current state (no exit/enter) or if `Locked` is `true`.
- `Update(delta)` — call once per frame/physics tick. Adds `delta` to `StateElapsed`, invokes the current state's `OnUpdate`, then evaluates transitions. Does nothing until an initial state is set.

## Transitions

Instead of calling `ChangeState` yourself, register conditions and let `Update` do it:

```csharp
public enum EnemyState { Idle, Chase, Attack, Dead }

_fsm.AddState(EnemyState.Idle);
_fsm.AddState(EnemyState.Chase).OnUpdate(delta => MoveToward(_target));
_fsm.AddState(EnemyState.Attack).OnEnter(StartAttack);
_fsm.AddState(EnemyState.Dead).OnEnter(PlayDeathAnimation);

_fsm.AddTransition(EnemyState.Idle, EnemyState.Chase,  () => CanSeePlayer());
_fsm.AddTransition(EnemyState.Chase, EnemyState.Attack, () => InAttackRange());
_fsm.AddTransition(EnemyState.Attack, EnemyState.Chase,  () => !InAttackRange());
_fsm.AddGlobalTransition(EnemyState.Dead, () => Health <= 0);   // from any state

_fsm.SetInitialState(EnemyState.Idle);
```

- `AddTransition(from, to, condition)` — moves from `from` to `to` when `condition()` returns `true` while `from` is the current state.
- `AddGlobalTransition(to, condition)` — same, but checked in every state.
- Both validate their arguments when called: `condition` can't be `null`, and the involved states must already be registered (`ArgumentException`), so add your states first.
- **Global transitions and the enum's default value.** A global transition is stored with the enum's `default` value as its `from`, and registration checks that this value is a registered state. In practice the enum's first (zero) member must be registered before you call `AddGlobalTransition` — it is in the example above (`Idle`).
- **Evaluation:** transitions are checked in registration order after the current state's `OnUpdate`, once per `Update`. The first one whose condition is `true` fires and evaluation stops for that tick — including a global transition that targets the state you're already in (which then does nothing).
- Transitions are skipped while `Locked`.
- `Transitions` and `States` expose the registered `Transition<T>` records and `State<T>` objects read-only.

## Queries and state

- `Locked` — when `true`, `ChangeState` calls are silently ignored and transitions aren't evaluated (useful for freezing transitions during a cutscene or hitstop). `Update` still runs the current `OnUpdate` and accumulates `StateElapsed`.
- `CurrentState` / `PreviousState` — the `State<T>` instances themselves (or `null`); `GetCurrentId()` / `GetPreviousId()` return just the enum value (`default` if unset).
- `IsInState(id)` — `true` if `id` is the current state.
- `StateElapsed` — seconds since the current state was entered. It's reset *after* `OnExit` runs and *before* the new `OnEnter`, so inside `OnExit` it still holds the time spent in the state being left.

[← back to README](../README.md)