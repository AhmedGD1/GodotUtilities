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

Open your Godot project's `.csproj` (same name as your project, in the project root) and add **two** `ProjectReference`s inside an `<ItemGroup>` — one for the library itself, one for its source generator (which powers `[EventHandler]`/`WireEvents()`). The generator has to be referenced separately with `OutputItemType="Analyzer"` because a `ProjectReference` doesn't forward analyzer references transitively:

```xml
<ItemGroup>
  <ProjectReference Include="..\GodotUtilities\GodotUtilities\GodotUtilities.csproj" />

  <ProjectReference Include="..\GodotUtilities\SourceGenerators\SourceGenerators.csproj"
                     OutputItemType="Analyzer"
                     ReferenceOutputAssembly="false" />
</ItemGroup>
```

Adjust the paths to wherever you actually cloned it, relative to your `.csproj`. If you skip the `SourceGenerators` reference, everything except `[EventHandler]`/`WireEvents()` still works — `WireEvents()` just won't be generated.

**3. Build.**

From Godot, build the project as usual (or `dotnet build` from the command line). Godot's C# build picks up both references automatically — no import step needed.

**4. Use it.**

```csharp
using GodotUtilities;         // extension methods + most small utilities (MathUtil, Countdown, etc.)
using GodotUtilities.Events;  // EventBus, [EventHandler]
using GodotUtilities.Logic;   // PhysicsQuery2D, WeightedLootTable<T>
using GodotUtilities.Pooling; // ObjectPool<T>, NodePool<T>, IPoolable
```

> **Updating:** since this is a `ProjectReference` and not a package, updating just means running `git pull` inside wherever you cloned it — no reinstall needed.

## Features

- extension methods on `Node`, `Vector2`, `Tween`, `World Raycast`, and other common Godot types.
- static pub/sub event bus, plus `[EventHandler]` declarative wiring.
- generic object pooling, including scene-tree-aware node pooling and other features you can discover.

MIT — see [LICENSE](LICENSE).
