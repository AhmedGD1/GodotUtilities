[← back to README](../README.md)

# SimpleStateMachine&lt;T&gt;

`GodotUtilities`

A small, light finite state machine keyed by an `enum`, with `OnEnter`/`OnUpdate`/`OnExit` callbacks per state, optional condition-based transitions, and simple timing rules (minimum duration and re-entry cooldown). It's a lighter alternative to a full state-machine node tree when you just need clean branching logic for something like a character or an enemy AI.

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
        _fsm.TryChangeState(PlayerState.Jump);
}
```

## States

- `AddState(id)` — registers a new state under an enum value and returns a `State<T>` builder; throws `ArgumentException` if `id` is already registered. Chain `.OnEnter(...)`, `.OnUpdate(delta => ...)`, `.OnExit(...)` on the result to attach callbacks (all optional). Setting a callback again replaces the previous one.
- `SetInitialState(id)` — sets the starting state and invokes its `OnEnter`, without raising `StateChanged` (there's no "previous" state). **Call it exactly once, before `Update`/`ChangeState`**: a second call throws `InvalidOperationException`, and throws `ArgumentException` if `id` isn't registered.
- `TryChangeState(id)` — the conditional change: it only switches if [`CanChangeState(id)`](#can-the-state-change) allows it, then exits the current state (`OnExit`), enters the new one (`OnEnter`), resets `StateElapsed` to `0`, and raises `StateChanged`. Returns `true` if the state changed and `false` if the change was refused (locked machine, minimum duration not reached, cooldown running, `id` not registered, `id` already the current state, or no initial state yet). It never throws. Use this for gameplay input and anything that should respect the state's timing rules.
- `ChangeState(id)` — the forced change: switches **unconditionally**, ignoring `Locked`, the current state's minimum duration and the target's cooldown. Throws `InvalidOperationException` if no initial state has been set and `ArgumentException` if `id` isn't registered. Use it for things that must win (death, stun, a cutscene reset).
- **Restarting a state.** `ChangeState(current)` restarts the state: `OnExit`, reset `StateElapsed`, `OnEnter`, and `StateChanged` fires with identical previous and current ids. `TryChangeState(current)` returns `false`, and automatic transitions never target the state you're already in, so a global transition like `AddGlobalTransition(Dead, () => Health <= 0)` fires once instead of restarting `Dead` every frame.
- `Update(delta)` — call once per frame/physics tick. Adds `delta` to `StateElapsed` and `TotalElapsed`, invokes the current state's `OnUpdate`, then evaluates transitions. Does nothing until an initial state is set.
- `GetStateOrNull(id)` — returns the registered `State<T>` for `id`, or `null`.

## Timing: minimum duration and cooldown

Both are set on the `State<T>` builder and default to `0` (no restriction):

```csharp
_fsm.AddState(PlayerState.Dash)
    .SetMinDuration(0.2)   // can't leave Dash until it has been active for 0.2s
    .SetCooldown(1.0);     // can't re-enter Dash until 1s after leaving it
```

- `SetMinDuration(seconds)` — the state must have been active for at least this long before the machine can leave it. It gates automatic transitions and `TryChangeState`; a forced `ChangeState` ignores it.
- `SetCooldown(seconds)` — after leaving the state, it can't be entered again until this much time has passed. The cooldown is measured from the moment the state is exited, using `TotalElapsed`, so it only advances while `Update` runs. A state that has never been left is always enterable.
- A forced `ChangeState` ignores cooldowns, but leaving a state that way still starts its cooldown, so automatic transitions can't re-enter it until the cooldown has passed.

## Transitions

Instead of calling `TryChangeState` yourself, register conditions and let `Update` do it:

```csharp
public enum EnemyState { Idle, Chase, Attack, Stunned, Dead }

_fsm.AddState(EnemyState.Idle);
_fsm.AddState(EnemyState.Chase).OnUpdate(delta => MoveToward(_target));
_fsm.AddState(EnemyState.Attack).OnEnter(StartAttack);
_fsm.AddState(EnemyState.Stunned);
_fsm.AddState(EnemyState.Dead).OnEnter(PlayDeathAnimation);

_fsm.AddTransition(EnemyState.Idle, EnemyState.Chase,  () => CanSeePlayer());
_fsm.AddTransition(EnemyState.Chase, EnemyState.Attack, () => InAttackRange());
_fsm.AddTransition(EnemyState.Attack, EnemyState.Chase,  () => !InAttackRange());
_fsm.AddTimeoutTransition(EnemyState.Stunned, EnemyState.Idle, 1.5);   // after 1.5s in Stunned
_fsm.AddGlobalTransition(EnemyState.Dead, () => Health <= 0);          // from any state

_fsm.SetInitialState(EnemyState.Idle);
```

- `AddTransition(from, to, condition)` — moves from `from` to `to` when `condition()` returns `true` while `from` is the current state.
- `AddGlobalTransition(to, condition)` — same, but checked in every state.
- `AddTimeoutTransition(from, to, duration)` — moves from `from` to `to` once `StateElapsed` reaches `duration` seconds. If `from` has a `MinDuration` longer than `duration`, it waits for the minimum duration.
- All three validate their arguments when called: the involved states must already be registered (`ArgumentException`), so add your states first. `AddTransition` and `AddGlobalTransition` also throw `ArgumentNullException` if `condition` is `null`.
- **Evaluation:** transitions are checked in registration order after the current state's `OnUpdate`, once per `Update`. For each transition that applies to the current state, the machine first checks whether the change is allowed (`CanChangeState`); if not, the transition is skipped **without calling its condition** and evaluation moves on to the next one. The first allowed transition whose condition is `true` fires, and evaluation stops for that tick.

## Can the state change?

`CanChangeState(to)` returns `true` only when all of these hold:

1. The machine isn't `Locked`.
2. An initial state has been set.
3. The current state's `MinDuration` has passed.
4. `to` is a registered state.
5. `to`'s `Cooldown` has elapsed.
6. `to` isn't already the current state.

`TryChangeState` and transition evaluation both use it (a forced `ChangeState` skips it), so you can call it yourself to check ahead of time (for example to grey out a button or avoid starting an animation that would be refused).

## Queries and state

- `Locked` — when `true`, `TryChangeState` returns `false` and transitions aren't evaluated, though a forced `ChangeState` still works (useful for freezing transitions during a cutscene or hitstop). `Update` still runs the current `OnUpdate` and accumulates `StateElapsed` and `TotalElapsed`.
- `CurrentState` / `PreviousState` — the `State<T>` instances themselves (or `null`); `GetCurrentId()` / `GetPreviousId()` return just the enum value (`default` if unset, which for an enum looks like its first member, so check `CurrentState`/`PreviousState` for `null` if that distinction matters).
- `IsInState(id)` — `true` if `id` is the current state.
- `StateElapsed` — seconds since the current state was entered. It's reset *after* `OnExit` runs and *before* the new `OnEnter`, so inside `OnExit` it still holds the time spent in the state being left.
- `TotalElapsed` — total seconds the machine has been updated since its initial state was set. It's what cooldowns are measured against.
- `StateChanged(previous, current)` — raised after every state change.

[← back to README](../README.md)