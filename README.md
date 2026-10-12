# GodotUtilities

A collection of general-purpose C# utilities for Godot 4: extension methods, a type-safe event bus, object pooling, a small state machine, timers, weighted loot tables, and compile-time scene/event wiring powered by Roslyn source generators.

## Requirements

- Godot 4 (.NET build). The library targets `net8.0` and is built against `GodotSharp` 4.7.2.
- `FileSystem` uses `ResourceLoader.ListDirectory`, which needs Godot 4.4 or newer.
- The source generators live in a separate `netstandard2.0` project (Roslyn `Microsoft.CodeAnalysis.CSharp` 4.8.0) — see [Setup](#setup).

## Setup

This library isn't published as a NuGet package — you add it to your Godot project as a source dependency (`ProjectReference`s to its `.csproj` files), so it builds alongside your game and you can step into or edit it directly.

**1. Clone it outside your project.**

Clone it next to your Godot project (not inside it, to keep Godot's filesystem dock from scanning its contents):

```bash
git clone https://github.com/AhmedGD1/GodotUtilities.git
```

**2. Reference it from your game's `.csproj`.**

Open your Godot project's `.csproj` (same name as your project, in the project root) and add **two** `ProjectReference`s inside an `<ItemGroup>` — one for the library itself, one for its source generators (which power `[EventHandler]`/`WireEvents()` and `[Node]`/`WireNodes()`). The generators have to be referenced separately with `OutputItemType="Analyzer"` because a `ProjectReference` doesn't forward analyzer references transitively:

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
using GodotUtilities;
using GodotUtilities.Events;  // EventBus, [EventHandler]
using GodotUtilities.Logic;   // WeightedLootTable<T>, SimpleStateMachine<T>
using GodotUtilities.Pooling; // ObjectPool<T>, NodePool<T>, IPoolable
```

> **Updating:** since this is a `ProjectReference` and not a package, updating just means running `git pull` inside wherever you cloned it — no reinstall needed.

## Quick example

```csharp
using Godot;
using GodotUtilities;
using GodotUtilities.Events;

public readonly record struct PlayerDiedEvent(int Score);

public partial class Hud : Control
{
    [Node] private Label scoreLabel;

    public override void _EnterTree() =>    
    public override void _Notification(int what)
    {
        if (what == NotificationSceneInstantiated)
            WireNodes(); // generated from [Node]

        if (what == NotificationEnterTree)
            WireEvents(); // generated from [EventHandler]
    }        

    [EventHandler]
    private void OnPlayerDied(PlayerDiedEvent evt)
    {
        scoreLabel.Text = $"Score: {evt.Score}";
    }
}

// Anywhere else:
EventBus.Trigger(new PlayerDiedEvent(Score: 1200));
```

## Features

Each feature has its own page under [`Docs/`](Docs/).

| Feature | Namespace | What it does |
| --- | --- | --- |
| [Extension methods](Docs/extensions.md) | `GodotUtilities` | Helpers on `Node`, `SceneTree`, `PackedScene`, `Control`, `AnimationPlayer`, `AnimatedSprite2D`, `Tween`/`PropertyTweener` (easing shorthand, property tweens), `CharacterBody2D`/`3D`, `GpuParticles2D`/`3D`, `Camera3D`, `World2D`/`3D` raycasts & shape queries, vectors, `Basis`, `Color`, `bool`, `Resource` and more. |
| [`EventBus`](Docs/event-bus.md) | `GodotUtilities.Events` | Static, type-safe pub/sub, plus `[EventHandler]`/`WireEvents()` declarative wiring generated at compile time. |
| [`[Node]` / `WireNodes()`](Docs/node-wiring.md) | `GodotUtilities` | Declarative scene-tree wiring: annotate fields/properties and let the generated `WireNodes()` resolve them — no `GetNode<T>()` boilerplate. |
| [`ObjectPool<T>` / `NodePool<T>`](Docs/pooling.md) | `GodotUtilities.Pooling` | Generic object pooling, and scene-tree-aware node pooling from a `PackedScene`. |
| [`SimpleStateMachine<T>`](Docs/simple-state-machine.md) | `GodotUtilitie.Logic` | Enum-keyed state machine with enter/update/exit callbacks and condition-based (including global) transitions. |
| [`WeightedLootTable<T>`](Docs/weighted-loot-table.md) | `GodotUtilities.Logic` | Weighted random picks: single, conditional, and multi-pick with or without duplicates. |
| [`Countdown`](Docs/countdown.md) | `GodotUtilities` | Lightweight, manually-ticked countdown struct for cooldowns. |
| [`InputBuffer`](Docs/input-buffer.md) | `GodotUtilities` | Jump-buffer-style input windows. |
| [`MathUtil`](Docs/math-util.md) | `GodotUtilities` | Framerate-independent exponential lerp, clamping, and seedable RNG helpers (random directions, points in a circle/sphere, picks). |
| [`FileSystem`](Docs/file-system.md) | `GodotUtilities` | Bulk-load resources from a `res://` directory and build snake_case id → path maps. |

## Repository layout

```
GodotUtilities/
├── GodotUtilities/        # the library (assembly: Saku.GodotUtilities, net8.0)
│   └── src/
│       ├── Events/        # EventBus, [EventHandler]
│       ├── Extension/     # extension methods (+ 2D/ and 3D/)
│       ├── Logic/         # SimpleStateMachine, WeightedLootTable
│       ├── NodeWiring/    # [Node]
│       ├── Pooling/       # ObjectPool, NodePool, IPoolable
│       └── Util/          # Countdown, InputBuffer, MathUtil, FileSystem
├── SourceGenerators/      # Roslyn generators (netstandard2.0)
│   ├── Event/             # [EventHandler] → WireEvents()  
│   └── NodeWiring/        # [Node] → WireNodes()           
└── Docs/                  # per-feature documentation
```

## License

MIT — see [LICENSE](LICENSE).