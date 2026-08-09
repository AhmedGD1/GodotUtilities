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

## Docs

- [Extensions](Docs/extensions.md) — extension methods on `Node`, `Vector2`, `Tween`, and other common Godot types
- [EventBus](Docs/event-bus.md) — static pub/sub event bus, plus `[EventHandler]` declarative wiring
- [PhysicsQuery2D](Docs/physics-query.md) — static 2D physics-space queries (raycasts, shape/circle checks and overlaps)
- [WeightedLootTable\<T\>](Docs/weighted-loot-table.md) — weighted random-selection table
- [ObjectPool\<T\> / NodePool\<T\>](Docs/pooling.md) — generic object pooling, including scene-tree-aware node pooling
- [InputBuffer](Docs/input-buffer.md) — short-lived buffered input windows (e.g. early jump presses)
- [AssetRegistry](Docs/asset-registry.md) — maps friendly ids to resource paths
- [FileSystem](Docs/file-system.md) — discover and bulk-load `Resource` files by type
- [Countdown](Docs/countdown.md) — minimal manually-ticked countdown timer struct
- [MathUtil](Docs/math-util.md) — framerate-independent lerping, clamping, and RNG helpers

## License

MIT — see [LICENSE](LICENSE).
