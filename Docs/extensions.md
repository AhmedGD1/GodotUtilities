[← back to README](../README.md)

# Extension Methods

`GodotUtilities`

Extension methods for Godot types. All of them live in the root `GodotUtilities` namespace, so a single `using GodotUtilities;` brings them in.

- [Nodes and scene tree](#nodes-and-scene-tree) — `Node`, `SceneTree`, `PackedScene`, `ResourcePreloader`, `Resource`, `GodotObject`
- [UI and animation](#ui-and-animation) — `Control`, `AnimationPlayer`, `AnimatedSprite2D`
- [Tweens](#tweens) — `Tween`, `PropertyTweener`
- [2D](#2d) — `Vector2`, `Node2D`, `CharacterBody2D`, `GpuParticles2D`
- [3D](#3d) — `Vector3`, `Basis`, `Camera3D`, `CharacterBody3D`, `GpuParticles3D`
- [Physics queries](#physics-queries) — `World2D` / `World3D` raycasts and shape checks
- [Small value helpers](#small-value-helpers) — `bool`, `Color`

## Nodes and scene tree

### Node

Several of these look children up **by type name**: the node's name has to equal `typeof(T).Name`.

| Method | Description |
| --- | --- |
| `AddToGroup()` | Adds the node to a group named after its own type (`node.GetType().Name`). Pairs with the type-named `SceneTree` group lookups below. |
| `HasNode<T>()` | `HasNode(typeof(T).Name)`. |
| `GetNode<T>()` / `GetNodeOrNull<T>()` | `GetNode<T>(typeof(T).Name)` — finds a child path named exactly like the type. |
| `GetAutoload<T>()` | `GetNode<T>("/root/" + typeof(T).Name)` — fetches an autoload registered under its class name. |
| `GetSibling<T>(index)` | The parent's child at `index`, cast to `T` (throws `InvalidCastException` on a type mismatch). |
| `GetLastChild<T>()` | The last child, as `T`. |
| `GetChildrenOfType<T>()` | `IEnumerable<T>` of the direct children that are `T`. |
| `GetChildOfType<T>(recursive = false)` | First child that is `T`, or `null`. With `recursive: true` it searches depth-first into each child before moving to its next sibling. |
| `TryGetChildOfType<T>(out result, recursive = false)` | Same, returning `false` instead of `null`. |
| `GetDescendants(recursive = true, owned = true)` | All descendants as `Node[]` (via `FindChildren("*")`). With `owned: true`, only nodes that have an `Owner` are returned — nodes added from code and never given an owner are skipped; pass `owned: false` to include them. |
| `QueueFreeChildren()` | Calls `QueueFree()` on every direct child. |
| `AddChildDeferred(child)` | Adds a child via `CallDeferred`, for use from physics callbacks and other places where changing the tree immediately is unsafe. |

```csharp
public partial class Spawner : Node2D
{
    public override void _Ready()
    {
        this.AddToGroup<Spawner>();                                   // group "Spawner"
        var timer = this.GetChildOfType<Timer>();       // first Timer child, or null
        var hud = this.GetAutoload<Hud>();              // /root/Hud
    }
}
```

### SceneTree

| Method | Description |
| --- | --- |
| `GetFirstNodeInGroup<T>(group)` | First node in the group as `T`. |
| `GetFirstNodeInGroup<T>()` | Same, using the group named after `T`. |
| `GetNodesInGroup<T>(group)` / `GetNodesInGroup<T>()` | Nodes in the group (by name, or by type name) that are `T`, as `IEnumerable<T>`. |
| `Wait(duration, ignoreTimeScale = false, processAlways = true)` | `async Task` that completes after `duration` seconds, using a `SceneTreeTimer`. |
| `NextIdle()` | `async Task` that completes on the next `ProcessFrame`. |

```csharp
await GetTree().Wait(0.5);        // runs even while the tree is paused (processAlways defaults to true)
await GetTree().NextIdle();

var player = GetTree().GetFirstNodeInGroup<Player>();
```

> `Wait` and `NextIdle` return a `Task`, so the code after `await` runs even if the node that called them was freed in the meantime. Check `IsNullOrInvalid()` before touching `this` afterwards if that can happen.

### PackedScene

| Method | Description |
| --- | --- |
| `InstantiateOrFree<T>()` | Instantiates the scene and returns it as `T` (`T` can be a class or interface). If the root isn't a `T`, the instance is `QueueFree`d, a warning is pushed, and `null` is returned. |
| `Instantiate<T>(parent, globalPos = null)` | For `T : Node2D`: instantiates, optionally sets `GlobalPosition`, adds the instance to `parent`, and returns it. |

> `Instantiate<T>(parent, globalPos)` applies `GlobalPosition` **before** the node is added to `parent`. Outside the tree there's no parent transform to account for, so if `parent` is moved, rotated or scaled the final global position won't equal `globalPos`. Add the node first and set `GlobalPosition` yourself when that matters.

### ResourcePreloader

| Method | Description |
| --- | --- |
| `InstantiateSceneOrNull<T>(name)` | Instantiates the `PackedScene` stored under `name` as `T`. Pushes an error and returns `null` if the name doesn't exist or isn't a `PackedScene`. |
| `InstantiateSceneOrNull<T>()` | Looks the scene up under `typeof(T).Name`, then under that name in snake_case (`EnemyBoss` → `enemy_boss`). If only the snake_case name exists. |

### Resource

| Method | Description |
| --- | --- |
| `Duplicate<T>(deep = false)` | `Resource.Duplicate` with the cast to `T` done for you. |
| `DuplicateDeep<T>(mode = Internal)` | `Resource.DuplicateDeep` cast to `T`; `mode` is a `Resource.DeepDuplicateMode`. |

### GodotObject

| Method | Description |
| --- | --- |
| `IsNullOrInvalid()` | `true` if the reference is `null` **or** the underlying Godot instance has been freed. |

## UI and animation

### Control

| Method | Description |
| --- | --- |
| `CenterPivotOffset()` | Sets `PivotOffset` to half the control's `Size`. |
| `GetMouseDirection(fromCenter = true)` | Unit vector from the control (its center, or its top-left corner with `fromCenter: false`) toward the global mouse position. |

### AnimationPlayer

| Method | Description |
| --- | --- |
| `PlayIfExist(animation, customBlend = -1, customSpeed = 1, fromEnd = false)` | Plays the animation if it exists; returns whether it did. |
| `PlayReset(customBlend = -1, customSpeed = 1, fromEnd = false)` | Plays the `RESET` animation and seeks to its start (with an update), restoring the rest pose. The `RESET` animation must exist. |
| `ResetAndPlay(animation, ...)` | `PlayReset()` followed by `Play(animation, ...)`. |
| `WaitToFinish()` | Returns a `SignalAwaiter` for `AnimationFinished`: `await player.WaitToFinish();`. |

### AnimatedSprite2D

| Method | Description |
| --- | --- |
| `TryPlay(animName)` | Plays the animation if the sprite's `SpriteFrames` has it; returns whether it did. |
| `PlayFrames(animName, frames)` | Swaps `SpriteFrames` to `frames` (only if different), then plays `animName`. |
| `WaitToFinish()` | Returns a `SignalAwaiter` for `AnimationFinished`. |

## Tweens

Shorthand for easing and for the most common property tweens.

### Transition and ease shorthand

`Tween` and `PropertyTweener` each get the same set of one-word methods:

- Transitions: `Linear`, `Sine`, `Quad`, `Cubic`, `Quart`, `Quint`, `Expo`, `Circ`, `Back`, `Bounce`, `Spring`, `Elastic` — each is `SetTrans(Tween.TransitionType.X)`.
- Easing: `EaseIn`, `EaseOut`, `EaseInOut`, `EaseOutIn` — each is `SetEase(Tween.EaseType.X)`.

On a `Tween` they set the default for tweeners created afterwards; on a `PropertyTweener` they apply to that tweener only. Both return what they were called on, so they chain.

```csharp
var tween = CreateTween().SetParallel();

tween.TweenModulateAlpha(sprite, 0f, 0.3).Sine().EaseOut();
tween.TweenScale(sprite, Vector2.One * 1.2f, 0.3).Back().EaseOut();

await tween.WaitToFinish();
```

`PropertyTweener` also has `SetCurveInterpolator(Curve curve)`, which drives the tween's progress through a `Curve` resource (`SampleBaked`) instead of a built-in easing function.

### Property tweens

All of these return the `PropertyTweener` so you can chain easing onto them.

| Method | Tweens |
| --- | --- |
| `TweenPosition(target, to, duration)` | `position` |
| `TweenGlobalPosition(target, to, duration)` | `global_position` |
| `TweenScale(target, value, duration)` | `scale` |
| `TweenRotation(target, value, duration)` | `rotation` |
| `TweenRotationDegrees(target, value, duration)` | `rotation_degrees` |
| `TweenColor(target, value, duration)` | `color` |
| `TweenModulate(item, color, duration)` | `modulate` (`CanvasItem`) |
| `TweenSelfModulate(item, color, duration)` | `self_modulate` (`CanvasItem`) |
| `TweenModulateAlpha(item, alpha, duration)` | `modulate:a` (`CanvasItem`) |
| `TweenSelfModulateAlpha(item, alpha, duration)` | `self_modulate:a` (`CanvasItem`) |
| `TweenOffsetPosition(control, value, duration)` | `offset_transform_position` (`Control`) |
| `TweenOffsetPositionRatio(control, value, duration)` | `offset_transform_position_ratio` (`Control`) |
| `TweenOffsetScale(control, value, duration)` | `offset_transform_scale` (`Control`) |
| `TweenOffsetRotation(control, value, duration)` | `offset_transform_rotation` (`Control`) |
| `TweenShader(material, paramName, value, duration)` | `shader_parameter/<paramName>` on a `ShaderMaterial` |

The `target`/`to` parameters of the first group are `GodotObject` and `Variant`, so they work on any object that has the property.

### Callbacks, methods and awaiting

| Method | Description |
| --- | --- |
| `TweenAction(Action action)` | `TweenCallback` taking a plain C# `Action`. |
| `TweenMethod<T>(Action<T> action, from, to, duration)` | `TweenMethod` taking a typed C# delegate. |
| `WaitToFinish()` (on `Tween` or `Tweener`) | Returns a `SignalAwaiter` for the `Finished` signal. |
| `KillIfValid()` | Kills the tween unless it's `null` or already freed — safe to call on a stored tween before starting a new one. |

```csharp
_tween.KillIfValid();
_tween = CreateTween();
_tween.TweenPosition(this, target, 0.4).Quad().EaseOut();
_tween.TweenAction(OnArrived);
```

## 2D

### Vector2

| Method | Description |
| --- | --- |
| `IsWithinDistanceSquared(other, distance)` | `true` if `other` is within `distance` — compares squared distances, so you pass the plain distance (it is squared internally). |
| `AngleDegrees()` | `Angle()` in degrees. |
| `RotatedDegrees(deg)` | `Rotated()` taking degrees. |
| `ToVector2I()` | Rounds each component to the nearest integer. |
| `ToVector3()` | `(x, y, 0)`. |
| `ToVector3XZ()` | `(x, 0, y)` — a 2D point on the ground plane. |
| `ToVector3YZ()` | `(0, x, y)`. |

### Node2D

| Method | Description |
| --- | --- |
| `GetMouseDirection()` | Unit vector from the node's `GlobalPosition` toward the global mouse position. |

### CharacterBody2D

| Method | Description |
| --- | --- |
| `ApplyGravity(dt, gravity = 980, maxFallSpeed = ∞)` | Adds gravity to `Velocity` along `-UpDirection` unless the body is on the floor, caps the fall speed, and returns the current fall speed (`0` on the floor). |
| `GetHorizontalSpeed()` | Length of the velocity component perpendicular to `UpDirection`. |
| `GetVerticalSpeed()` | Velocity along `UpDirection` — positive means moving up. |

`ApplyGravity` only changes `Velocity`; you still call `MoveAndSlide()` yourself.

### GpuParticles2D

| Method | Description |
| --- | --- |
| `SetDirection(direction)` | Sets the `ParticleProcessMaterial`'s direction (pushes a warning if the particles have no process material). |
| `EmitFresh()` | `Emitting = true` and `Restart()`. |
| `EmitTimeout(duration)` | `async Task`: emits fresh, waits `duration` seconds, then stops emitting (if the particles still exist). Not for `OneShot` particles — those push a warning and return. |

## 3D

### Vector3

| Method | Description |
| --- | --- |
| `RotatedDegrees(axis, degrees)` | `Rotated()` taking degrees. |
| `IsWithinDistanceSquared(other, distance)` | As for `Vector2`: pass the plain distance. |
| `ToVector2()` | `(x, y)` — drops `z`. |

### Basis

`Forward()` (`-Z`), `Back()` (`Z`), `Right()` (`X`), `Left()` (`-X`), `Up()` (`Y`), `Down()` (`-Y`).

### Camera3D

| Method | Description |
| --- | --- |
| `GetMouseRayOrigin()` | Ray origin for the current mouse position (`ProjectRayOrigin`). |
| `GetMouseRayDirection()` | Ray direction for the current mouse position (`ProjectRayNormal`). |

### CharacterBody3D

Same three methods as [`CharacterBody2D`](#characterbody2d), with a default `gravity` of `9.8`.

### GpuParticles3D

`EmitFresh()` and `EmitTimeout(duration)`, behaving as for `GpuParticles2D`.

## Physics queries

`World2D` and `World3D` get raycast and shape-query helpers that hide the query-parameter boilerplate. They behave the same in 2D and 3D (shown here in 2D; the 3D versions use `Vector3`, `Shape3D`, `CollisionObject3D` and `RaycastHit3D`).

```csharp
var world = GetWorld2D();

// Ray from a point to a point
if (world.Raycast(from, to, out RaycastHit2D hit))
    GD.Print($"Hit {hit.Collider?.Name} at {hit.Position}, normal {hit.Normal}");

// Ray from an origin along a direction
if (world.Raycast(GlobalPosition, Vector2.Down, 32f, out var ground, mask: 1u << 0))
    SnapTo(ground.Position);

// Is anything inside this shape?
bool blocked = world.CheckShape(_probeShape, GlobalPosition + offset);

// Everything inside this shape
CollisionObject2D[] overlaps = world.OverlapShape(_probeShape, GlobalPosition, maxResults: 16);
```

| Method | Description |
| --- | --- |
| `Raycast(from, to, out hit, mask, collideWithBodies, collideWithAreas)` | Casts a ray between two points. Returns `false` (and a default `hit`) if nothing was hit. |
| `Raycast(origin, direction, distance, out hit, ...)` | Casts from `origin` to `origin + direction * distance`. `direction` is **not** normalized for you, so pass a unit vector. |
| `CheckShape(shape, position, mask, ...)` | `true` if the shape at `position` overlaps anything. |
| `CheckShape(shape, position, out collider, mask, ...)` | Same, also returning the first overlapping collider. |
| `OverlapShape(shape, position, mask, maxResults = 8, ...)` | Colliders overlapping the shape at `position`, up to `maxResults` (empty array if none). |

Common parameters: `mask` defaults to `uint.MaxValue` (all layers), `collideWithBodies` to `true`, `collideWithAreas` to `false`. The shape is placed at `position` with no rotation (2D) / identity basis (3D).

`RaycastHit2D` / `RaycastHit3D` are `readonly struct`s with:

| Field | Description |
| --- | --- |
| `Position`, `Normal` | World-space hit point and surface normal. |
| `Collider` | The hit object cast to `CollisionObject2D`/`3D` — `null` if the hit object isn't one. |
| `ColliderId`, `ColliderRid`, `Shape` | The collider's instance id, its physics `Rid`, and the index of the shape that was hit (always populated). |

## Small value helpers

| Type | Method | Description |
| --- | --- | --- |
| `bool` | `ToSingle()` | `1f` / `0f`. |
| `bool` | `ToInt()` | `1` / `0`. |
| `bool` | `ToSign()` | `1` / `-1`. |
| `Color` | `ToVector4()` | `(R, G, B, A)` as a `Vector4`. |

```csharp
float rotationDir = isFacingRight.ToSign();
float dir = MathUtil.CoinFlip().ToSingle();
```

[← back to README](../README.md)