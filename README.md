# Godot Utilities — C# Library

A modular C# utility library for Godot 4, covering combat, physics, animation, pooling, tweening, and finite state machines.

---

## Namespaces

| Namespace | Contents |
|---|---|
| `Utilities` | Extensions, Cooldown, MathUtil, TimerUtil, TweenExtensions |
| `Utilities.Combat` | HealthComponent, HitboxComponent, HurtboxComponent, KnockbackComponent |
| `Utilities.Logic` | VelocityComponent, Physics2D |
| `Utilities.FSM` | StateMachine, State, Transition |
| `Utilities.Pooling` | NodePool, ObjectPool |
| `Utilities.InputBuffering` | InputBuffer |

---

## Modules

### Combat

A component-based hit/hurt system. Attach `HealthComponent` and `HurtboxComponent` to a damageable entity, and `HitboxComponent` to an attack hitbox.

```csharp
// Deal damage when hitbox overlaps a hurtbox
// HitboxComponent handles this automatically via AreaEntered

// Manually damage an entity
var data = new DamageData(source, damage: 10f, DamageType.Physical, knockback: Vector2.Zero);
healthComponent.TakeDamage(data);

// Resistances and immunity
healthComponent.SetResistance(DamageType.Ranged, 0.5f); // 50% ranged resistance
healthComponent.AddImmunity(DamageType.Poison);

// Invincibility frames
healthComponent.MakeInvincible(1.5f);

// Signals
healthComponent.Damaged  += (source, amount) => { };
healthComponent.Died     += (source) => { };
healthComponent.Healed   += (amount) => { };
```

---

### Finite State Machine

A generic, enum-keyed FSM with builder-style state and transition setup. Not a Node — instantiate it wherever you need it.

```csharp
public enum PlayerState { Idle, Run, Jump, Attack }

var fsm = new StateMachine<PlayerState>();

fsm.AddState(PlayerState.Idle)
    .OnEnter(() => sprite.Play("idle"))
    .OnUpdate(dt => { if (Input.IsActionPressed("move")) fsm.TryTransitionTo(PlayerState.Run); });

fsm.AddState(PlayerState.Run)
    .OnEnter(() => sprite.Play("run"))
    .MinDuration(0.1f);

fsm.AddState(PlayerState.Attack)
    .OnEnter(() => sprite.Play("attack"))
    .TimeoutAfter(0.6f, PlayerState.Idle);

// Transitions
fsm.AddTransition(PlayerState.Idle, PlayerState.Run)
    .When(() => Input.IsActionPressed("move"));

fsm.AddTransition(PlayerState.Run, PlayerState.Idle)
    .When(() => !Input.IsActionPressed("move"));

// Event-driven transition
fsm.AddTransition(PlayerState.Idle, PlayerState.Attack)
    .OnEvent("attack_pressed");

fsm.SetInitialState(PlayerState.Idle);
fsm.Start();

// In _Process / _PhysicsProcess
fsm.UpdateStates(delta);

// Fire an event
fsm.TriggerEvent("attack_pressed");
```

**Transition options:**

```csharp
fsm.AddTransition(from, to)
    .When(() => condition)           // poll-based condition
    .IfOnly(() => guard)             // secondary guard (blocks transition if false)
    .OnEvent("event_name")          // fire-and-forget event trigger
    .Do(() => callback)             // runs on transition
    .SetPriority(10)                // higher = checked first
    .OverrideMinDuration(0.2f)      // per-transition min time
    .ForceInstant();                // ignore min duration
```

---

### VelocityComponent

A full-featured 2D movement component with coyote time, jump buffering, and configurable gravity. Export `controller` in the inspector, then call from your character script.

```csharp
// In _PhysicsProcess
velocityComponent.Accelerate(direction, dt);        // move at max speed
velocityComponent.AccelerateWithSpeed(dir, dt, 200f);
velocityComponent.Decelerate(dt);

// Jumping
velocityComponent.BufferJump();                     // call on jump input pressed
velocityComponent.TryJump();                        // call in physics update

// Gravity
velocityComponent.SetGravityActive(false);          // disable gravity (floating mode)
velocityComponent.SwitchGravity();                  // flip up direction

// Signals
velocityComponent.Landed  += () => { };
velocityComponent.Fell    += () => { };
velocityComponent.Jumped  += (count) => { };
```

---

### Tween Extensions

Shorthand methods on `Tween` and `PropertyTweener` to reduce boilerplate. Also adds easing/transition helpers for chaining.

```csharp
var tween = CreateTween();

tween.TweenFadeIn(sprite, 0.3f).EaseOut();
tween.TweenShake(camera, duration: 0.4f, strength: 8f);
tween.TweenPunch(sprite, duration: 0.3f, scale: new Vector2(0.2f, 0.2f));
tween.TweenSquish2D(sprite, duration: 0.4f);
tween.TweenWiggle(sprite, degrees: 15f, duration: 0.3f);
tween.TweenBlink(sprite, blinks: 4);
tween.TweenOrbit(node, center, radius: 64f, duration: 2f);
tween.TweenTypewriter(label, duration: 2f);
tween.TweenCounter(label, from: 0f, to: 100f, duration: 1f);
tween.TweenShader(material, "dissolve_amount", 1f, duration: 0.5f);
tween.TweenVolume(audioPlayer, db: -80f, duration: 1f);

// Await completion
await tween.AwaitAsync();

// Safe kill
tween.KillIfValid();

// Callback on finish
tween.OnComplete(() => QueueFree());
```

**TweenVirtual** — drive arbitrary values with a tween, without needing a Godot property path:

```csharp
TweenVirtual.Float(0f, 1f, 0.5f, value => material.SetShaderParameter("alpha", value));
TweenVirtual.Vector2(start, end, 1f, pos => myNode.Position = pos);
```

---

### Pooling

**`NodePool<T>`** — for pooling Godot nodes instantiated from a `PackedScene`.

```csharp
var bulletPool = new NodePool<Bullet>(bulletScene, parent: this, initialSize: 20);

var bullet = bulletPool.Get();
// ... use bullet ...
bulletPool.Release(bullet);
```

Implement `IPoolable` on your node for `OnGet` / `OnRelease` callbacks.

**`ObjectPool<T>`** — for plain C# objects.

```csharp
var pool = new ObjectPool<MyClass>(() => new MyClass(), initialSize: 10);
var obj  = pool.Get();
pool.Release(obj);
```

---

### Physics2D

Static raycast helpers. Call `Physics2D.UpdateSpaceState(GetViewport())` once (e.g. in `_Ready`) before using.

```csharp
Physics2D.UpdateSpaceState(GetViewport());

if (Physics2D.Raycast(GlobalPosition, Vector2.Down, maxDistance: 200f, out var hit, layers: 1))
{
    GD.Print(hit.Collider.Name);
    GD.Print(hit.Distance);
}
```

---

### Cooldown

A lightweight struct timer. No heap allocation.

```csharp
private Cooldown attackCooldown = new Cooldown(0.5f);

// In _Process
attackCooldown.Tick(delta);

if (attackCooldown.IsReady)
{
    attackCooldown.Start();
    // ... attack ...
}
```

---

### InputBuffer

Buffers input actions for a short window so inputs aren't dropped between frames.

```csharp
private InputBuffer buffer = new InputBuffer();

// In _Process
if (Input.IsActionJustPressed("jump"))
    buffer.BufferAction("jump", duration: 0.15f);

buffer.Update((float)delta);

// In _PhysicsProcess
if (buffer.TryConsume("jump"))
    velocityComponent.Jump();
```

---

### MathUtil

Common math helpers with a shared `RandomNumberGenerator` instance.

```csharp
MathUtil.ExponentialLerp(from, to, dt, weight: 10f);
MathUtil.RandomUnit();                          // random direction Vector2
MathUtil.RandomInCircle(radius: 50f);
MathUtil.Chance(0.25f);                         // true 25% of the time
MathUtil.CoinFlip();
MathUtil.WeightedPick(("common", 0.7f), ("rare", 0.25f), ("epic", 0.05f));
MathUtil.Progress(currentHealth, maxHealth);    // 0..1 clamped
```

---

### Extension Methods

**`CharacterBody2DExtensions`**
```csharp
controller.ApplyGravity(gravity, delta, maxFallSpeed: 800f);
controller.Jump(heightInPixels: 64f);
controller.AddImpulse(knockbackForce);
controller.IsFalling();
controller.IsMoving();
```

**`Node2DExtensions`**
```csharp
node.FlipTo(velocity);                              // flips scale.X based on direction
node.SmoothlyLookAt(target, acceleration: 5f, dt);
node.GetMouseDirection();
```

**`NodeExtensions`**
```csharp
node.GetChildOfType<HealthComponent>();
node.GetParentOfType<Player>();
node.DeleteChildren();
```

**`AnimatedSprite2DExtensions`**
```csharp
sprite.PlayIfNotAlready("run");
sprite.OnAnimationFinished(() => fsm.TryTransitionTo(PlayerState.Idle));
await sprite.WaitToFinish();
```

**`Vector2Extensions`**
```csharp
direction.RotatedDegrees(45f);
pos.IsWithinDistance(target, 100f);
```

---
