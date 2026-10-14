[← back to README](../README.md)

# TweenBuilder

`GodotUtilities`

A thin, fluent arrangement layer over Godot's native `Tween`. It adds no behavior of its own: every call maps to the matching `Tween` / `Tweener` call, and the result is a plain Godot `Tween` you can keep using. Think of it as `StringBuilder` for tweens.

```csharp
this.BuildTween()
    .Property(sprite, "scale", Vector2.One * 1.2f, 0.25).Back().EaseOut()
    .Wait(1.0)
    .ModulateAlpha(sprite, 0f, 0.4).Sine().EaseInOut();
```

- Nothing to start: like any Godot tween, it begins on its own, so there is no `Play()` to forget.
- Nothing hidden: [`Tween`](#properties) exposes the native tween for anything the builder doesn't wrap.
- The tween is bound to the node you build it from, and is freed with it.

## When to use it

`TweenBuilder` is not a replacement for Godot's `Tween` or for the [`Tween` extensions](extensions.md#tweens). It doesn't change how tweens work, and it doesn't make simple tweens simpler. It's a tool for **complex sequences**, where the raw API gets hard to read: many steps, groups of parallel and sequential steps, per-step easing and delays, and callbacks in the middle.

| Situation | Reach for |
| --- | --- |
| One property, one tween | `CreateTween()` and the `Tween` extensions |
| A short sequence of two or three steps | Either one |
| Long sequences with parallel groups, such as cutscenes, intro/outro animations or UI transitions | `TweenBuilder` |

With the plain API, the order of a complex sequence is spread across `Parallel()` and `Chain()` calls, and you have to track what each one applies to. The builder lets you read the sequence top to bottom, and `BeginParallel()` / `EndParallel()` show where the groups start and end.

Because it produces a native `Tween`, there's no commitment. You can use the builder for one sequence, plain tweens everywhere else, and move between them as needed.

## Creating a builder

```csharp
var builder = this.BuildTween();             // runs once
var looping = this.BuildTween(loops: 0);     // 0 = loop forever (Godot's rule)
```

`BuildTween(this Node node, int loops = 1)` is an extension on `Node`. It creates the tween with `node.CreateTween()` and sets the loop count.

## Properties

| Member | Description |
| --- | --- |
| `Tween` | The underlying Godot `Tween`. Use it for what the builder doesn't wrap: pause mode, speed scale, `SetParallel`, the `Finished` signal. |
| `IsValid` | `false` if the tween is `null` or has been freed. |
| `IsRunning` | `true` if the tween is valid and running. |

```csharp
var builder = this.BuildTween()
    .Property(this, "position", target, 0.4).Quad().EaseOut();

builder.Tween.SetPauseMode(Tween.TweenPauseMode.Process);
builder.Tween.Finished += OnArrived;
```

## Sequence building

Each method adds one step and returns the builder. Steps run one after another unless you make them [parallel](#parallel-steps).

| Method | Adds | Notes |
| --- | --- | --- |
| `Property(obj, prop, finalVal, duration)` | A property tween | `prop` is a `NodePath`, so sub-properties work: `"modulate:a"`. |
| `Method<T>(action, from, to, duration)` | A typed method tween | Calls `action` with values interpolated from `from` to `to`. |
| `Callback(Action)` / `Callback(Callable)` | A callback | Runs when the sequence reaches the step. |
| `Wait(duration)` | A standalone pause | Use `SetDelay` to offset one tweener instead. |
| `WaitForSignal(signal)` | A step that waits for a signal | The *sequence* waits; this is not C# `await`. |
| `SubTween(Tween)` / `SubTween(TweenBuilder)` | Another tween as a step | See [Sub-tweens](#sub-tweens). |

The step you just added becomes the **current tweener**, which the [configuration](#configuring-the-current-tweener) methods apply to.

## Parallel steps

By default, steps run in order. There are two ways to run steps together.

| Method | Effect |
| --- | --- |
| `JoinPrevious()` | Only the **next** tweener runs alongside the previous one. |
| `BeginParallel()` / `EndParallel()` | Every tweener added between the two calls runs in parallel, as if you'd called `JoinPrevious()` before each. |
| `Chain()` | Godot's `Chain()`: the next tweener starts after the previous step (the default). |

```csharp
this.BuildTween()
    .GlobalPosition(cam, bossPos, 1.5).Cubic().EaseOut()

    .BeginParallel() 
        .Callback(label.Show)
        .ModulateAlpha(label, 1f, 0.2).From(0f)
        .ShaderParam(material, "progress", 1f, 0.3).From(0f)
    .EndParallel()

    .Wait(2.5) 

    .GlobalPosition(cam, home, 1.5).Sine().EaseInOut() 
    .JoinPrevious()
        .Scale(cam, Vector2.One, 0.5).Cubic().EaseOut();
```

> Parallel blocks can't be nested: the first `EndParallel()` ends parallel mode for good. `Chain()` has no visible effect inside a block, because each tweener is joined to the previous one again.

> `BeginParallel()` has the same effect as writing `.JoinPrevious()` before every tweener inside it. The first tweener of the block therefore also joins whatever came before it.

## Configuring the current tweener

These apply to the **last step you added**. If that step doesn't support the call, an `InvalidOperationException` names the call, what it supports, and what the current tweener actually is.

| Method | Works on | Description |
| --- | --- | --- |
| `SetEase(ease)` | Property, Method | Easing for this tweener. |
| `SetTrans(transition)` | Property, Method | Transition for this tweener. |
| `SetDelay(delay)` | Property, Method, Callback, Subtween | Delays this one tweener. |
| `From(value)` | Property | Start value. |
| `FromCurrent()` | Property | Start from the current value. |
| `AsRelative()` | Property | Treat the final value as relative to the start value. |
| `SetCurveInterpolator(curve)` | Property | Drive progress with a `Curve` instead of a built-in easing function. |

```csharp
.Property(sprite, "position", target, 0.4).AsRelative().Quad().EaseOut()
.Wait(0.5).EaseIn();
// InvalidOperationException: SetEase only works on PropertyTweener or MethodTweener,
// but the current tweener is: IntervalTweener.
```

## Shortcuts

Everything below is an extension method on `TweenBuilder`, in the same namespace.

### Transitions and easing

- Transitions: `Linear`, `Sine`, `Quad`, `Cubic`, `Quart`, `Quint`, `Expo`, `Circ`, `Back`, `Bounce`, `Spring`, `Elastic`, each `SetTrans(...)`.
- Easing: `EaseIn`, `EaseOut`, `EaseInOut`, `EaseOutIn`, each `SetEase(...)`.

They apply to the current tweener, so they chain after any step that supports them: `.Property(...).Back().EaseOut()`.

### Property steps

Each is `Property(...)` with the property name filled in, and returns the builder so you can keep configuring.

| Method | Tweens |
| --- | --- |
| `Position(target, to, duration)` | `position` |
| `GlobalPosition(target, to, duration)` | `global_position` |
| `Scale(target, to, duration)` | `scale` |
| `Rotation(target, to, duration)` | `rotation` |
| `RotationDegrees(target, to, duration)` | `rotation_degrees` |
| `Color(target, to, duration)` | `color` |
| `Modulate(item, to, duration)` | `modulate` (`CanvasItem`) |
| `SelfModulate(item, to, duration)` | `self_modulate` (`CanvasItem`) |
| `ModulateAlpha(item, alpha, duration)` | `modulate:a` (`CanvasItem`) |
| `SelfModulateAlpha(item, alpha, duration)` | `self_modulate:a` (`CanvasItem`) |
| `OffsetPosition(control, to, duration)` | `offset_transform_position` (`Control`) |
| `OffsetPositionRatio(control, to, duration)` | `offset_transform_position_ratio` (`Control`) |
| `OffsetScale(control, to, duration)` | `offset_transform_scale` (`Control`) |
| `OffsetRotation(control, to, duration)` | `offset_transform_rotation` (`Control`) |
| `ShaderParam(material, paramName, to, duration)` | `shader_parameter/<paramName>` on a `ShaderMaterial` |

`ShaderParam` builds its `NodePath` on each call; the others reuse cached paths.

## Sub-tweens

`SubTween` runs another tween as one step of this one. Pass either a `Tween` or a `TweenBuilder`.

```csharp
var flash = this.BuildTween()
    .ModulateAlpha(sprite, 0.2f, 0.05)
    .ModulateAlpha(sprite, 1f, 0.05);

this.BuildTween()
    .Position(this, hitPos, 0.2).Quad().EaseOut()
    .SubTween(flash)
    .Callback(OnHit);
```

## Awaiting and cleanup

The builder has no lifecycle methods of its own. Use the tween:

```csharp
var builder = this.BuildTween().Position(this, target, 0.4);

await builder.Tween.WaitToFinish();        // from TweenExtension

builder.Tween.KillIfValid();               // safe on null / freed tweens
```

[← back to README](../README.md)