# GodotUtilities

A collection of general-purpose C# utilities for Godot 4: extension methods, a type-safe event bus, object pooling, a small state machine, timers, weighted loot tables, and compile-time scene/event wiring, signal shortcuts and singletons powered by Roslyn source generators.

## Requirements

- Godot 4 (.NET build). The library targets `net8.0` and is built against `GodotSharp` 4.7.2.
- `FileSystem` uses `ResourceLoader.ListDirectory`, which needs Godot 4.4 or newer.
- The source generators live in a separate `netstandard2.0` project (Roslyn `Microsoft.CodeAnalysis.CSharp` 4.8.0).

## Setup

You can install GodotUtilities in either of two ways.

### Option 1: NuGet (Recommended)

Install the package directly into your Godot C# project:

```bash
dotnet add package Saku.GodotUtilities
```

Or add the package reference manually to your project's `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Saku.GodotUtilities" Version="3.6.1" />
</ItemGroup>
```

The package includes the Roslyn source generators, so you don't need to reference the `SourceGenerators` project separately.

Build your project as usual. The library and its source generators will be available automatically.

**Updating:** Run `dotnet add package Saku.GodotUtilities` to install the latest version, or update the package version in your `.csproj`.

[View the package on NuGet.org](https://www.nuget.org/packages/Saku.GodotUtilities).

### Option 2: Source Dependency

Use this option if you want to modify the library itself, debug its implementation, or step directly into its source.

**1. Clone the repository outside your project.**

Clone it next to your Godot project (not inside it, to keep Godot's filesystem dock from scanning its contents):

```bash
git clone https://github.com/AhmedGD1/GodotUtilities.git
```

**2. Reference it from your game's `.csproj`.**

Open your Godot project's `.csproj` (same name as your project, in the project root) and add two `ProjectReference`s inside an `<ItemGroup>` — one for the library itself, one for its source generators:

```xml
<ItemGroup>
  <ProjectReference Include="..\GodotUtilities\GodotUtilities\GodotUtilities.csproj" />

  <ProjectReference Include="..\GodotUtilities\SourceGenerators\SourceGenerators.csproj"
                     OutputItemType="Analyzer"
                     ReferenceOutputAssembly="false" />
</ItemGroup>
```

Adjust the paths to wherever you actually cloned it, relative to your `.csproj`. If you skip the `SourceGenerators` reference, everything except `[EventHandler]`/`WireEvents()`, `[Node]`/`WireNodes()`, the `[Signal]` shortcuts and `[Singleton]` still works — those generated methods just won't be emitted.

**3. Build.**

From Godot, build the project as usual (or `dotnet build` from the command line). Godot's C# build picks up both references automatically — no import step needed.

**Updating:** Run `git pull` inside the cloned repository.

## Quick example

```csharp
using Godot;
using GodotUtilities;
using GodotUtilities.Events;

public readonly record struct PlayerDiedEvent(int Score);

public partial class Hud : Control
{
    [Node] private Label scoreLabel;

    public override void _Notification(int what)
    {
        if (what == NotificationSceneInstantiated)
            WireNodes();

        if (what == NotificationEnterTree)
            WireEvents();
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
| [`[Signal]` shortcuts](Docs/signal-shortcuts.md) | generated on your class | `ConnectToX` / `DisconnectFromX` / `IsConnectedToX` / `ToSignalX` for every `[Signal]` delegate: `Action` handlers, connect flags, and `await`-able signals. No `using` needed. |
| [`[Singleton]`](Docs/singleton.md) | `GodotUtilities` | Generates `Instance` / `HasInstance` / `TryGetInstance` for a node: placed in a scene (`Scene`), registered as an autoload (`Autoload`), or created on first access (`Lazy`). |
| [`ObjectPool<T>` / `NodePool<T>`](Docs/pooling.md) | `GodotUtilities.Pooling` | Generic object pooling, and scene-tree-aware node pooling from a `PackedScene`. |
| [`SimpleStateMachine<T>`](Docs/simple-state-machine.md) | `GodotUtilities.Logic` | Enum-keyed state machine with enter/update/exit callbacks and condition-based (including global) transitions. |
| [`WeightedLootTable<T>`](Docs/weighted-loot-table.md) | `GodotUtilities.Logic` | Weighted random picks: single, conditional, and multi-pick with or without duplicates. |
| [`Countdown`](Docs/countdown.md) | `GodotUtilities` | Lightweight, manually-ticked countdown struct for cooldowns. |
| [`InputBuffer`](Docs/input-buffer.md) | `GodotUtilities` | Jump-buffer-style input windows. |
| [`MathUtil`](Docs/math-util.md) | `GodotUtilities` | Framerate-independent exponential lerp, clamping, and seedable RNG helpers (random directions, points in a circle/sphere, picks). |
| [`FileSystem`](Docs/file-system.md) | `GodotUtilities` | Bulk-load resources from a `res://` directory and build snake_case id → path maps. |

## Repository layout

```text
GodotUtilities/
├── GodotUtilities/        # the library (assembly: Saku.GodotUtilities, net8.0)
│   └── src/
│       ├── Events/        # EventBus, [EventHandler]
│       ├── Extension/     # extension methods (+ 2D/ and 3D/)
│       ├── Logic/         # SimpleStateMachine, WeightedLootTable
│       ├── NodeWiring/    # [Node]
│       ├── Pooling/       # ObjectPool, NodePool, IPoolable
│       ├── Singleton/     # [Singleton], SingletonMode
│       └── Util/          # Countdown, InputBuffer, MathUtil, FileSystem, TweenBuilder
├── SourceGenerators/      # Roslyn generators (netstandard2.0)
│   ├── Event/             # [EventHandler] → WireEvents()
│   ├── NodeWiring/       # [Node] → WireNodes()
│   ├── Signal/            # [Signal] → ConnectTo/DisconnectFrom/IsConnectedTo/ToSignal
│   └── Singleton/         # [Singleton] → Instance/HasInstance/TryGetInstance
└── Docs/                  # per-feature documentation
```

## License

MIT — see [LICENSE](LICENSE).
