# Godot Utilities — C# Library

A modular collection of gameplay systems and helpers for **Godot 4** projects written in C#. Covers everything from FSMs and UI management to physics queries, object pooling, and combat components — designed to drop in and stay out of your way.

---

## Modules

| Namespace | What it covers |
|---|---|
| `Utilities` | Core helpers — math, timers, cooldowns, node injection, extensions |
| `Utilities.FSM` | Generic finite state machine with transitions, guards, and events |
| `Utilities.UIManagement` | Stack-based UI manager with transitions and popup support |
| `Utilities.Pooling` | Generic and Node-specific object pools |
| `Utilities.Events` | Type-safe global event bus with attribute-based wiring |
| `Utilities.Logic` | `VelocityComponent`, `Physics2D` raycasting/overlap helpers |
| `Utilities.Combat` | Health, hitbox, hurtbox, and knockback components |

---

## Core Utilities

### MathUtil

Static helpers for randomness, interpolation, and probability. All random calls go through a single seeded `RandomNumberGenerator` instance.

```csharp
// Frame-rate independent smoothing
velocity = MathUtil.ExponentialLerp(velocity, target, delta, weight: 12f);

// Random spatial helpers
Vector2 spawnPoint = MathUtil.RandomInCircle(radius: 80f);
Vector2 direction  = MathUtil.RandomUnit();

// Probability / picks
bool   crit   = MathUtil.Chance(0.15f);   // 15% chance
string rarity = MathUtil.WeightedPick(
    ("Common",    60f),
    ("Rare",      30f),
    ("Legendary", 10f));
```

### TimerUtil

Thin wrappers around Godot's `SceneTreeTimer` that integrate cleanly with `async/await`.

```csharp
// Fire-and-forget
TimerUtil.Wait(1.5f, () => Explode(), this);

// Awaitable — chain multiple delays
await TimerUtil.WaitAsync(0.3f, this);
await TimerUtil.WaitAsync(0.3f, this);
PlaySound();
```

### Cooldown

A value-type cooldown counter meant to live as a field. No node required.

```csharp
private Cooldown attackCooldown = new(0.4f);

public override void _Process(double delta)
{
    attackCooldown.Tick(delta);

    if (Input.IsActionJustPressed("attack") && attackCooldown.IsReady)
    {
        Attack();
        attackCooldown.Start();
    }
}
```

`Cooldown.Progress` returns `[0, 1]` — feed it directly into a UI progress bar.

---

## Finite State Machine (`Utilities.FSM`)

A generic, data-driven FSM. States are defined in code with fluent builder calls; transitions fire on conditions, external events, or timeouts.

```csharp
public enum State { Idle, Run, Jump, Attack }

private StateMachine<State> fsm = new();

public override void _Ready()
{
    fsm.AddState(State.Idle)
        .OnEnter(() => PlayAnim("idle"))
        .MinDuration(0.1f);

    fsm.AddState(State.Run)
        .OnEnter(() => PlayAnim("run"))
        .OnUpdate(dt => Move(inputDir, dt));

    fsm.AddState(State.Attack)
        .OnEnter(() => PlayAnim("attack"))
        .TimeoutAfter(0.5f, to: State.Idle);

    fsm.AddTransition(State.Idle, State.Run)
        .When(() => inputDir != Vector2.Zero);

    fsm.AddTransition(State.Run, State.Idle)
        .When(() => inputDir == Vector2.Zero);

    // Event-driven transition — trigger from anywhere
    fsm.AddTransition(State.Idle, State.Attack)
        .OnEvent("attack_pressed");

    fsm.SetInitialState(State.Idle);
    fsm.Start();
}

public override void _Process(double delta)
{
    fsm.UpdateStates(delta);
}

// Elsewhere:
fsm.TriggerEvent("attack_pressed");
```

**Transition options**

| Method | Effect |
|---|---|
| `.When(Func<bool>)` | Condition checked every frame |
| `.IfOnly(Func<bool>)` | Guard — blocks the transition even if the condition is true |
| `.OnEvent(string)` | Fires only when that event is triggered |
| `.Do(Action)` | Callback run as the transition executes |
| `.OverrideMinDuration(float)` | Minimum time in the *from* state before the transition can fire |
| `.ForceInstant()` | Bypasses the min-duration check |
| `.SetPriority(int)` | Higher priority transitions are evaluated first |

Global transitions (`AddGlobalTransition`) are checked from every state — useful for a universal "died" or "stunned" path.

---

## UI Management (`Utilities.UIManagement`)

A stack-based panel manager. Panels are `UIView` instances registered in a `UIRegistry` resource by string ID. The manager handles instantiation, caching, transitions, and the dimmer overlay for popups.

### UIView

Extend this for each screen or panel:

```csharp
public partial class PauseMenu : UIView
{
    public override void OnInitialize(object payload) { /* setup with payload data */ }
    public override void OnShow()   { /* started becoming visible */ }
    public override void OnHide()   { /* finished hiding */ }
    public override void OnFinalize() { /* cleanup before hide begins */ }
}
```

### UIManager

```csharp
// Show a panel (hides the current one)
await UIManager.Instance.ShowPanel("hud", TransitionType.Fade);

// Show as a popup (keeps the panel underneath; adds a dimmer)
await UIManager.Instance.ShowPanel("settings", TransitionType.ScalePop, isPopup: true);

// Pass arbitrary data to OnInitialize
await UIManager.Instance.ShowPanel("shop", payload: shopData);

// Go back one step
await UIManager.Instance.GoBack();
```

**Available transitions:** `None`, `Fade`, `ScalePop`, `SlideLeft`, `SlideRight`, `SlideUp`, `SlideDown`.

Individual views can override their preferred transition via `SetPreferredTransition()`, which takes priority over whatever the caller passes in.

---

## Object Pooling (`Utilities.Pooling`)

Two pool types: one for plain C# objects, one for Godot `Node` subclasses.

```csharp
// Generic pool
var dataPool = new ObjectPool<Data>(() => new Data(), initialSize: 20);

Data b = dataPool.Get();
dataPool.Release(b);

// Node pool — handles scene instantiation and visibility toggling
var enemyPool = new NodePool<EnemyNode>(enemyScene, parent: this, initialSize: 10);

EnemyNode e = enemyPool.Get();
enemyPool.Release(e);
```

Implement `IPoolable` (`OnGet` / `OnRelease`) on pooled types to hook into retrieval and return events. Both pool types support `Prewarm`, `ReleaseAll`, `Trim`, and exhaustion warnings.

---

## Event Bus (`Utilities.Events`)

A global, type-safe pub/sub system. Events are plain structs or record structs — no string keys, no inheritance required.

```csharp
// Define an event
public record struct PlayerDied(Vector2 Position);

// Subscribe
EventBus.AddListener<PlayerDied>(OnPlayerDied, owner: this);

private void OnPlayerDied(PlayerDied evt)
{
    SpawnBlood(evt.Position);
}

// Publish
EventBus.Trigger(new PlayerDied(GlobalPosition));

// Zero-allocation trigger for parameterless events
public record struct GamePaused();
EventBus.Trigger<GamePaused>();
```

### Attribute wiring

For nodes with many subscriptions, `WireEvents` scans the class for `[EventHandler]` methods and registers them automatically. Subscriptions are cleaned up when the node leaves the tree.

```csharp
public override void _Ready() => this.WireEvents();

[EventHandler]
private void OnPlayerDied(PlayerDied evt) { ... }

[EventHandler(Once = true)]
private void OnFirstKill(EnemyKilled evt) { ... }
```

---

## VelocityComponent (`Utilities.Logic`)

A `Node` child that owns all physics movement for a `CharacterBody2D`. It handles gravity, jumping with coyote time and jump buffering, apex hang, gravity flipping, and explosion knockback — all configurable from the Godot inspector.

```csharp
// In _PhysicsProcess:
velocity.Move(inputDir, delta);

if (Input.IsActionJustPressed("jump"))
    velocity.TryJump();

if (Input.IsActionJustReleased("jump"))
    velocity.CutJump();
```

**Key features**

- Multi-jump (`SetMaxJumps`)
- Coyote time and jump buffering (configurable durations)
- Apex hang — reduced gravity and boosted air control at peak
- Asymmetric gravity: separate fall multiplier for snappier arcs
- Gravity flip (`SwitchGravity`, `SetGravityState`)
- Floating motion mode for top-down or zero-gravity movement
- Explosion impulse with linear / quadratic / inverse-square falloff and upward bias

Lifecycle events are fired through `EventBus`: `Jumped`, `Landed`, `Fell`, `FellOffEdge`, `ApexReached`, `GravitySwitched`, `MotionModeChanged`.

---

## Physics2D (`Utilities.Logic`)

Static helpers for raycasts and circle overlaps that pool their query parameter objects internally.

```csharp
// Raycast — direction + distance
if (Physics2D.Raycast(origin, Vector2.Down, 32f, out RaycastHit hit))
    GD.Print(hit.Collider.Name);

// Point-to-point
Physics2D.Raycast(from, to, out hit, collisionMask: LayerMask.Ground);

// Overlap circle — returns all bodies in radius
var bodies = Physics2D.OverlapCircle(position, radius: 120f);
```

---

## Combat Components (`Utilities.Combat`)

Three components that compose to form a standard damage pipeline:

**HealthComponent** — exported properties for max health, defense (flat reduction), per-type resistances, and immunity. Emits signals for `HealthChanged`, `Damaged`, `Healed`, `Died`, `Revived`, and `DamagePrevented`. Includes an invincibility timer that blocks incoming damage for a configurable window after a hit.

**HitboxComponent** — an `Area2D` that detects `HurtboxComponent` entries and builds a `DamageData` struct automatically, including knockback direction from the contact geometry.

**HurtboxComponent** — routes incoming `DamageData` to the sibling `HealthComponent` and optionally forwards knockback to a `KnockbackComponent`.

**KnockbackComponent** — adds the knockback vector directly to the owner's `CharacterBody2D.Velocity` and emits `KnockbackStarted`.

```csharp
// Typical setup — no code needed beyond inspector wiring.
// To deal damage manually:
var data = new DamageData(source: this, damage: 25f,
    type: HealthComponent.DamageType.Poison,
    knockback: Vector2.Zero);

hurtbox.ReceiveDamage(data);
```

---

## Node Utilities

### NodeInjector

Resolves node references at runtime using a `[NodeRef]` attribute, eliminating `GetNode<T>` calls scattered across `_Ready`.

```csharp
[NodeRef] private Sprite2D sprite;
[NodeRef("HitboxComponent")] private HitboxComponent hitbox;

public override void _Ready() => this.InjectNodes();
```

Without a path argument, `[NodeRef]` performs a depth-first search for the first child matching the field's type.

### Extension methods

| Target | Method | Notes |
|---|---|---|
| `Node` | `GetChildOfType<T>()` | First matching child, any depth |
| `Node` | `GetParentOfType<T>()` | Walks up the tree |
| `Node` | `DeleteChildren()` | Queues all children for deletion |
| `Node` | `AddChildDeferred(child)` | Safe for physics callbacks |
| `Node2D` | `FlipTo(direction)` | Flips X scale based on direction |
| `Node2D` | `SmoothlyLookAt(target, acc, dt)` | Lerped angle tracking |
| `Node2D` | `GetMouseDirection()` | Normalized direction to cursor |
| `CharacterBody2D` | `ApplyGravity(g, dt, max)` | Inline gravity helper |
| `CharacterBody2D` | `Jump(height)` | Physics-correct jump velocity |
| `CharacterBody2D` | `ApplyKnockbackFrom(source, force)` | Directional knockback |
| `AnimatedSprite2D` | `PlayIfNotAlready(name)` | Guards against restart |
| `AnimatedSprite2D` | `PlayIfExist(name)` | Returns false if missing |
| `AnimatedSprite2D` | `WaitToFinish()` | Awaitable finish signal |

---

## Tween Effects

A suite of pooled tweener objects that run effects through a `Tween` callback. All support both `Node2D` and `Control` targets.

| Tweener | Effect |
|---|---|
| `ShakePositionTweener` | Noise-based positional shake with decay |
| `ShakeRotationTweener` | Sine-based rotation shake with decay |
| `PunchPositionTweener` | Elastic-out positional punch |
| `PunchRotationTweener` | Elastic-out rotation punch |
| `PunchScaleTweener` | Elastic-out scale punch |
| `FlickerTweener` | Random alpha flicker (e.g. damage flash) |
| `OrbitTweener` | Circular orbit around a point |

Objects are retrieved from a static pool per type and returned automatically at the end of their duration.

---
