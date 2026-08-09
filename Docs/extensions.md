[← back to README](../README.md)

# Extensions

`GodotUtilities`

Extension methods on common Godot types — `Node`, `Node2D`, `SceneTree`, `Tween`/`Tweener`/`PropertyTweener`, `Vector2`, `CharacterBody2D`, `AnimatedSprite2D`, `AnimationPlayer`, `GpuParticles2D`, `GodotObject`, and `bool`. They're all under the plain `GodotUtilities` namespace, so a single `using GodotUtilities;` covers all of them.

## Node

- `TryGetChildOfType<T>(out result, recursive)` / `GetChildOfType<T>(recursive)` — find the first direct (or, recursively, any descendant) child of type `T`.
- `GetChildrenOfType<T>()` — all direct children of type `T`.
- `QueueFreeChildren()` — queues every direct child for deletion.

## Node2D

- `GetMouseDirection()` — unit direction from this node toward the current mouse position.

## SceneTree

- `GetFirstNodeInGroup<T>(group)` / `GetNodesInGroup<T>(group)` — typed variants of Godot's group lookups.
- `Wait(duration)` — awaitable `SignalAwaiter` that completes after `duration` seconds (via a one-shot `SceneTreeTimer`).
- `NextIdle()` — awaitable that completes on the next process frame.

## Tween / Tweener / PropertyTweener

- Transition/ease shorthand: `Linear()`, `Sine()`, `Back()`, `Bounce()`, `Circ()`, `Spring()`, `Quad()`, `Quart()`, `Expo()`, `Quint()`, `Elastic()`, `Cubic()`, `EaseIn()`, `EaseOut()`, `EaseOutIn()`, `EaseInOut()` — available on both `Tween` (sets the default for the whole tween) and `PropertyTweener` (sets it for one step).
- `WaitToFinish()` — awaitable that completes when the tween/tweener finishes.
- `KillIfValid()` — kills the tween only if it's still a valid instance.
- `SetCurveInterpolator(curve)` — drives a `PropertyTweener` from a `Curve` resource instead of a transition/ease pair.
- `TweenAction(action)` — shorthand for `TweenCallback(Callable.From(action))`.
- `TweenMethod<T>(action, from, to, duration)` — typed shorthand for `TweenMethod`.
- `TweenShader(material, paramName, value, duration)` — tweens a shader parameter directly.
- `TweenPosition` / `TweenGlobalPosition` / `TweenScale` — shorthand for tweening the corresponding property by name.

## Vector2

- `RotatedDeg(deg)` — `Rotated`, but in degrees.
- `IsWithinDistanceSquared(other, distance)` — distance check using squared distance (avoids a `Sqrt`).

## CharacterBody2D

- `ApplyGravity(dt, gravity, maxFallSpeed)` — applies gravity along `-UpDirection` when not on the floor, clamped to `maxFallSpeed`.
- `GetHorizontalSpeed()` — velocity magnitude perpendicular to `UpDirection`.
- `GetVerticalSpeed()` — velocity magnitude along `UpDirection`.

## AnimatedSprite2D

- `WaitToFinish()` — awaitable that completes on `AnimationFinished`.
- `TryPlay(animName)` — plays the animation only if it exists on the current `SpriteFrames`; returns whether it did.
- `PlayFrames(animName, frames)` — swaps `SpriteFrames` (only if different) and plays in one call.

## AnimationPlayer

- `ResetAndPlay(animation, customBlend, customSpeed, fromEnd)` — plays the `"RESET"` animation, seeks to 0, then plays `animation`.
- `PlayIfExist(animation, customBlend, customSpeed, fromEnd)` — plays only if the animation exists; returns whether it did.

## GpuParticles2D

- `SetDirection(direction)` — sets a `ParticleProcessMaterial`'s `Direction` from a `Vector2` (pushes a warning if the process material isn't a `ParticleProcessMaterial`).
- `EmitTimeout(duration)` — restarts emission and turns it off again after `duration` seconds (no-op with a warning if `OneShot` is set).
- `EmitFresh()` — restarts emission immediately.

## GodotObject

- `IsNullOrInvalid()` — `true` if the reference is `null` or no longer a valid instance.

## bool

- `ToSingle()` — `1f` / `0f`.
- `ToSign()` — `1f` / `-1f`.

[← back to README](../README.md)
