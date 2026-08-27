# GodotUtilities

A collection of general-purpose C# utilities for Godot 4.

## Setup

This library isn't published as a NuGet package — you add it to your Godot project as a source dependency (`ProjectReference`s to its `.csproj` files), so it builds alongside your game and you can step into or edit it directly.

**1. Clone it outside your project.**

Clone it next to your Godot project (not inside it, to keep Godot's filesystem dock from scanning its contents):

```bash
git clone https://github.com/AhmedGD1/GodotUtilities.git
```

**2. Reference it from your game's `.csproj`.**

Open your Godot project's `.csproj` (same name as your project, in the project root) and add **two** `ProjectReference`s inside an `<ItemGroup>` — one for the library itself, one for its source generator (which powers `[EventHandler]`/`WireEvents()` and `[Node]`/`WireNodes()`). The generator has to be referenced separately with `OutputItemType="Analyzer"` because a `ProjectReference` doesn't forward analyzer references transitively:

```xml
<ItemGroup>
  <ProjectReference Include="..\GodotUtilities\GodotUtilities\GodotUtilities.csproj" />

  <ProjectReference Include="..\GodotUtilities\SourceGenerators\SourceGenerators.csproj"
                     OutputItemType="Analyzer"
                     ReferenceOutputAssembly="false" />
</ItemGroup>
```

Adjust the paths to wherever you actually cloned it, relative to your `.csproj`. If you skip the `SourceGenerators` reference, everything except `[EventHandler]`/`WireEvents()` and `[Node]`/`WireNodes()` still works — those generated methods just won't be emitted.

**3. Build.**

From Godot, build the project as usual (or `dotnet build` from the command line). Godot's C# build picks up both references automatically — no import step needed.

**4. Use it.**

```csharp
using GodotUtilities;         // extension methods + most small utilities (MathUtil, Countdown, FileSystem, AssetRegistry, [Node], etc.)
using GodotUtilities.Events;  // EventBus, [EventHandler]
using GodotUtilities.Logic;   // SimpleStateMachine<T>, WeightedLootTable<T>
using GodotUtilities.Pooling; // ObjectPool<T>, NodePool<T>, IPoolable
```

> **Updating:** since this is a `ProjectReference` and not a package, updating just means running `git pull` inside wherever you cloned it — no reinstall needed.

## Features

- **Extension methods** on `Node`, `Control`, `AnimationPlayer`, `World`, `PackedScene`, `SceneTree`, `Tween`/`PropertyTweener` (full easing-curve shorthand), `GodotObject`, `bool`, and 2D/3D-specific types (`Vector2`/`Vector3`, `Node2D`, `CharacterBody2D`/`3D`, `GPUParticles2D`/`3D`, `Camera3D`, `Basis`, `World2D`/`3D` raycasts & shape queries).
- **`EventBus`** — a static pub/sub event bus, plus `[EventHandler]`/`WireEvents()` declarative wiring generated at compile time.
- **`[Node]`/`WireNodes()`** — declarative scene-tree wiring: annotate fields/properties with `[Node]` and call the generated `WireNodes()` to resolve them from the scene tree, no manual `GetNode<T>()` boilerplate.
- **Generic object pooling** — `ObjectPool<T>` for any type, `NodePool<T>` for scene-tree-aware node pooling.
- **`AssetRegistry`** — id-based resource lookup, with folder scanning.
- **`FileSystem`** — helpers for bulk-loading resources and instantiating scenes from a `res://` directory.
- **`SimpleStateMachine<T>`** — a small enum-keyed state machine with enter/update/exit callbacks.
- **`WeightedLootTable<T>`** — weighted random item/drop selection.
- **`Countdown`** / **`InputBuffer`** — lightweight manually-ticked timers for cooldowns and jump-buffer-style input windows.
- **`MathUtil`** — framerate-independent exponential lerp, clamping, and RNG helpers.

MIT — see [LICENSE](LICENSE).
