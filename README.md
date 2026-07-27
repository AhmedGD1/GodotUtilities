# GodotUtilities

A collection of general-purpose C# utilities for Godot 4 

Targets **.NET 8** and **Godot 4**

## Table of Contents

- [Installation](#installation)
- [Extensions](#extensions)
- [Utilities](#utilities)
  - [EventBus](#eventbus)
  - [EventHandlerAttribute + EventSubscriber](#eventhandlerattribute--eventsubscriber-declarative-wiring)
  - [PhysicsQuery2D](#physicsquery2d)
  - [WeightedLootTable\<T\>](#weightedloottablet)
  - [ObjectPool\<T\> / NodePool\<T\>](#objectpoolt--nodepoolt)
  - [InputBuffer](#inputbuffer)
  - [AssetRegistry](#assetregistry)
  - [FileSystem](#filesystem)
  - [Countdown](#countdown)
  - [MathUtil](#mathutil)
- [License](#license)

## Installation

This library is distributed as source — you reference its `.csproj` directly from your Godot game's `.csproj`, rather than as a compiled NuGet package. This keeps it easy to step into and modify.

1. Clone this repository somewhere on your machine, either inside your Godot project or next to it:

   ```bash
   git clone https://github.com/AhmedGD1/GodotUtilities.git
   ```

2. Open your Godot project's `.csproj` file

3. Add a `ProjectReference` pointing at `GodotUtilities.csproj`, inside an `<ItemGroup>`. The path is relative to your project's `.csproj`. For example, if you cloned the repo into a `libs/` folder next to your project:

   ```xml
   <ItemGroup>
     <ProjectReference Include="path of cloned library" />
   </ItemGroup>
   ```

4. Save the file and rebuild (Godot will pick up the reference automatically the next time it builds.

5. You can now use any type from the library via its namespace, e.g.:

   ```csharp
   using GodotUtilities.Events;
   using GodotUtilities.Logic;
   using GodotUtilities.Pooling;
   ```

   Most extension methods and small utilities live directly under the `GodotUtilities` namespace, so a plain `using GodotUtilities;` covers those.

> **Note:** Since this is a `ProjectReference` and not a NuGet package, updating the library just means pulling the latest changes (`git pull`) in wherever you cloned it — no reinstall needed.

**[⬆ back to top](#table-of-contents)**

## Extensions

The library also ships a set of extension methods on common Godot types (`Node`, `Node2D`, `Vector2`, `Tween`, `CharacterBody2D`, `AnimatedSprite2D`, `GpuParticles2D`, `SceneTree`, `GodotObject`, `bool`) covering things like child lookups, tween shorthand, gravity application, and null/validity checks. They're all under the `GodotUtilities` namespace and available automatically once you add a `using GodotUtilities;` — explore them in `src/Extension/` as needed.

**[⬆ back to top](#table-of-contents)**

## Utilities

Jump to: [EventBus](#eventbus) · [EventSubscriber](#eventhandlerattribute--eventsubscriber-declarative-wiring) · [PhysicsQuery2D](#physicsquery2d) · [WeightedLootTable](#weightedloottablet) · [Pooling](#objectpoolt--nodepoolt) · [InputBuffer](#inputbuffer) · [AssetRegistry](#assetregistry) · [FileSystem](#filesystem) · [Countdown](#countdown) · [MathUtil](#mathutil)

### EventBus

`GodotUtilities.Events`

A static, type-safe, global publish/subscribe event bus. Instead of wiring up Godot signals between distant nodes, you subscribe to a C# type and trigger it from anywhere.

**Basic usage:**

```csharp
public readonly record struct PlayerDied(int Score);

// Subscribe
EventBus.AddListener<PlayerDied>(evt => GD.Print($"Score was {evt.Score}"));

// Trigger from anywhere
EventBus.Trigger(new PlayerDied(Score: 1200));
```

Pass a `Node` as the `owner` parameter and the listener is automatically removed when that node leaves the scene tree, so you don't have to unsubscribe manually:

```csharp
EventBus.AddListener<PlayerDied>(OnPlayerDied, owner: this);
```

Other methods: `AddListenerOnce<T>` (fires once then auto-removes), `RemoveListener<T>`, `Trigger<T>()` (fires a default-constructed instance), and `Clear()` / `Clear<T>()` to wipe all or per-type listeners (handy between game restarts or scene reloads).

**[⬆ back to top](#table-of-contents)**

### EventHandlerAttribute + EventSubscriber (declarative wiring)

`GodotUtilities.Events`

An alternative to calling `EventBus.AddListener` manually: mark methods on a `Node` with `[EventHandler]`, then call `node.WireEvents()` once (typically in `_Ready`) to wire them all up at once, auto-removed when the node exits the tree.

```csharp
public partial class Player : CharacterBody2D
{
    public override void _Ready() => this.WireEvents();

    [EventHandler]
    private void OnPlayerDied(PlayerDied evt) => GD.Print($"Final score: {evt.Score}");

    // No parameter? Specify the event type explicitly.
    [EventHandler(typeof(GamePaused), Once = true)]
    private void OnGamePaused() => GD.Print("Paused once, then auto-unsubscribed.");
}
```

The event type is inferred from the method's first parameter; if the method takes no parameter, pass the type explicitly via `[EventHandler(typeof(YourEvent))]`. Setting `Once = true` makes it behave like `AddListenerOnce`.

**[⬆ back to top](#table-of-contents)**

### PhysicsQuery2D

`GodotUtilities.Logic`

Static helpers for 2D physics-space queries — raycasts, circle checks, and circle overlaps — without needing to hold a reference to a specific node's physics space.

```csharp
if (PhysicsQuery2D.Raycast(from, to, out RaycastHit hit, collisionMask: 1))
    GD.Print($"Hit {hit.Collider.GetType().Name} at {hit.Position}");

if (PhysicsQuery2D.CheckCircle(position, radius: 32f, out GodotObject collider))
    GD.Print($"Something is here: {collider}");

// Uses a cached circle shape instead of creating new one on each call
if (PhysicsQuery2D.CheckCircle(cachedCircleShape, position, radius: 32f)
    GD.Print($"Something is here");

GodotObject[] nearby = PhysicsQuery2D.OverlapCircle(position, radius: 64f, maxResults: 8);
```

- `Raycast` casts a ray and returns the closest hit (including `Area2D`s) as a `RaycastHit` struct (`Position`, `Normal`, `Collider`, `ColliderRid`).
- `CheckCircle` tests whether a circle at a position overlaps anything and returns the first collider found.
- `OverlapCircle` returns every collider overlapping a circle, up to a max result count.
- `IntersectShape` is the lower-level primitive behind the circle methods, for when you need more than just the collider (e.g. shape index or RID) or want to query with a different `Shape2D`.

**How it works:** All queries run through `PhysicsDirectSpaceState2D`. By default the space is resolved lazily from the main scene tree's root viewport (via `Engine.GetMainLoop()`), so you can call these methods from anywhere without passing a node around. If you're working inside a `SubViewport` with its own physics space, or want to skip the main-loop lookup, call `PhysicsQuery2D.SetWorldSpace(state)` once to override which space subsequent queries run against.

**[⬆ back to top](#table-of-contents)**

### WeightedLootTable&lt;T&gt;

`GodotUtilities.Logic`

A weighted random-selection table — register items with an integer weight, then draw one or many at random, with the probability of each item proportional to its weight.

```csharp
var table = new WeightedLootTable<string>();
table.AddItem("Common Sword", weight: 70);
table.AddItem("Rare Sword", weight: 25);
table.AddItem("Legendary Sword", weight: 5);

string drop = table.PickItem(); // weighted random pick

// Pick 3 distinct items without repeats:
List<string> loot = table.PickItems(3, allowDuplicates: false);

// Only pick among items matching a condition:
string commonOnly = table.PickItem(item => item.StartsWith("Common"));
```

Other members: `RemoveItem`, `Clear`, `Contains`, `GetItemWeight`, `SetItemWeight`, `ModifyItemWeight` (adjust a weight up/down, e.g. for pity systems), `GetAllItems`/`GetItems(condition)`, and `TotalWeight`/`ItemCount`/`IsEmpty` for introspection. You can supply your own `RandomNumberGenerator` in the constructor (or via `SetRandom`) for seeded/deterministic drops; otherwise it shares `MathUtil.RNG`.

**[⬆ back to top](#table-of-contents)**

### ObjectPool&lt;T&gt; / NodePool&lt;T&gt;

`GodotUtilities.Pooling`

Two generic object pools for reusing instances instead of repeatedly allocating/instantiating and freeing them — useful for bullets, particles, enemies, or any object you create and destroy frequently.

- **`ObjectPool<T>`** pools *any* type via a factory function. Good for plain C# objects or Godot objects that don't need to live in the scene tree.
- **`NodePool<T>`** specifically pools `Node`-derived instances created from a `PackedScene`, parented under a shared root. It additionally manages scene-tree concerns for you: toggling `ProcessMode` and `Visible` when a node is checked out/returned, and deferred-freeing nodes when the pool is cleared or trimmed.

```csharp
// ObjectPool: pools plain objects via a factory
var bulletDataPool = new ObjectPool<BulletData>(factory: () => new BulletData());
bulletDataPool.Prewarm(50);
var data = bulletDataPool.Get();
// ... use it ...
bulletDataPool.Release(data);

// NodePool: pools scene instances parented under `bulletContainer`
var bulletPool = new NodePool<Bullet>(bulletScene, bulletContainer) { Extendable = true };
bulletPool.Prewarm(20);

Bullet bullet = bulletPool.Get(); // enabled + visible, ready to use
// ... later ...
bulletPool.Release(bullet); // disabled + hidden, returned to the pool
```

Both pools share the same shape:
- `Get()` / `TryGet(out value)` — retrieve an instance; `TryGet` returns `false` on exhaustion instead of pushing a warning.
- `Release(obj)` / `TryRelease(obj)` — return an instance; `TryRelease` returns `false` if it wasn't tracked as active.
- `Prewarm(count)` — eagerly create instances up front to avoid allocation spikes mid-game.
- `ReleaseAll()` — return every active instance to the pool at once (e.g. on wave clear).
- `Trim(targetSize, onTrim?)` — shrink the free (unused) pool down to a target size, discarding the rest.
- `Extendable` — if `true`, the pool creates a new instance when exhausted instead of failing.
- `ActiveCount` / `FreeCount` / `TotalCount` — introspection.
- `NodePool<T>` also exposes `Clear()`, which deferred-frees every node it tracks (active and free) and empties the pool — useful when tearing down a level.

If your pooled type implements `IPoolable` (`OnGet()` / `OnRelease()`), both pools call it automatically on checkout/return — put your "reset state" logic there instead of scattering it at call sites.

**[⬆ back to top](#table-of-contents)**

### InputBuffer

Tracks short-lived "buffered" inputs so an action pressed slightly too early (e.g. jump pressed a few frames before landing) can still be consumed within a configurable time window, instead of being lost.

```csharp
private readonly InputBuffer _inputBuffer = new();

public override void _PhysicsProcess(double delta)
{
    if (Input.IsActionJustPressed("jump"))
        _inputBuffer.BufferAction("jump", duration: 0.15); // buffer for 150ms

    _inputBuffer.Tick(delta); // advance/expire buffered actions every tick

    if (IsOnFloor() && _inputBuffer.TryConsume("jump"))
        Jump();
}
```

- `BufferAction(name, duration)` — starts (or refreshes) a buffer window for an action name.
- `TryConsume(name)` — if the action is still within its buffer window, removes and returns `true`; otherwise `false`.
- `Has(name)` — checks validity without consuming.
- `Tick(dt)` — must be called once per frame/physics tick to count down and expire buffered actions.
- `ConsumeAll()` — clears every currently buffered action.

**[⬆ back to top](#table-of-contents)**

### AssetRegistry

Maps human-friendly string ids to resource paths, so the rest of your code can load assets by name (`"player_idle"`) instead of hardcoding `res://` paths everywhere.

```csharp
var registry = new AssetRegistry();

// Register a whole folder — each file gets an id auto-derived from its filename (snake_case)
registry.RegisterFolder("res://assets/textures", recursive: true);

// Or register individual entries manually
registry.Register("player_icon", "res://ui/icons/player.png");

// Load by id later, anywhere in your code
Texture2D icon = registry.Load<Texture2D>("player_icon");

// Or the non-throwing version
if (registry.TryLoad<Texture2D>("enemy_icon", out var enemyIcon))
    sprite.Texture = enemyIcon;
```

Also exposes `TryRegister`/`TryRegisterAuto` (non-overwriting variants), `Unregister`, `Contains`, `Clear`, and introspection via `GetIds()`, `GetPaths()`, `GetMap()`, `GetPath`/`TryGetPath`.

**[⬆ back to top](#table-of-contents)**

### FileSystem

Static helpers for discovering and bulk-loading `Resource` files directly from the Godot filesystem (`res://`, `user://`) by type, without needing to register them first.

```csharp
// Load every SpriteFrames resource under a folder
List<SpriteFrames> allFrames = FileSystem.LoadResourcesInPath<SpriteFrames>("res://sprites", recursive: true);

// Or just get the paths without loading, e.g. to lazy-load later
List<string> paths = FileSystem.ScanFolder<PackedScene>("res://levels");
```

- `LoadResourcesInPath<T>(path, recursive)` — loads and returns every resource of type `T` found in a folder.
- `ScanFolder<T>(path, recursive)` — same scan, but returns matching resource *paths* only, without loading them.

**[⬆ back to top](#table-of-contents)**

### Countdown

A minimal, manually-ticked countdown timer struct — a lightweight alternative to a `Timer` node when you don't need signals or scene-tree presence (e.g. per-enemy cooldowns tracked in plain C# state).

```csharp
private Countdown _dodgeCooldown = new(duration: 1.5);

public override void _PhysicsProcess(double delta)
{
    _dodgeCooldown.Tick(delta);

    if (Input.IsActionJustPressed("dodge") && _dodgeCooldown.IsFinished)
    {
        Dodge();
        _dodgeCooldown.Start(); // restart using the default duration
    }
}
```

- `Start()` — (re)starts using the duration passed to the constructor.
- `Start(seconds)` — (re)starts with a custom duration for this run.
- `Tick(dt)` — call once per frame/tick to count down; clamps at zero.
- `Stop()` — immediately ends it (`IsFinished` becomes `true`).
- `IsFinished` — `true` once time has run out.
- `TimeLeft` — remaining seconds.
- `Progress` — `0..1`, how far through the countdown you are (useful for UI cooldown fills).

**[⬆ back to top](#table-of-contents)**

### MathUtil

`GodotUtilities` (static class)

General-purpose math helpers: framerate-independent lerping, clamping, normalization, and RNG-based utilities, all built on a single shared `RandomNumberGenerator`.

```csharp
// Smoothly approach a target value regardless of frame rate
_currentSpeed = MathUtil.ExpoLerp(_currentSpeed, targetSpeed, delta, accel: 8f);

// Random helpers
Vector2 dir = MathUtil.RandomDirection();
bool heads = MathUtil.CoinFlip();
if (MathUtil.Chance(0.25f)) DropRareItem();
var pick = MathUtil.PickRandom("a", "b", "c");

// Normalize a value into [0, 1] given a max length
float t = MathUtil.Normalize(currentHealth, maxHealth);
```

- `ExpoLerp(a, b, dt, accel)` — exponential interpolation toward a target that behaves consistently regardless of frame rate (unlike a naive `Lerp(a, b, factor)` per frame). Available for `float` and `Vector2`.
- `Clamp01(value)` — shorthand for clamping to `[0, 1]` (`float`/`double`).
- `Normalize(value, length)` — divides by `length` and clamps to `[0, 1]`.
- `RandomDirection()` — a random 2D unit vector.
- `CoinFlip()` — 50/50 `bool`.
- `Chance(probability)` — `true` with the given probability (`0..1`).
- `PickRandom(items)` — picks a random element from a `params T[]` or `List<T>`.
- `RNG` — the shared `RandomNumberGenerator` instance used by all random methods above; call `SeedRNG(seed)` to make future random calls deterministic (e.g. for reproducible runs/tests).

**How it works:** `ExpoLerp` computes an interpolation factor as `1 - e^(-accel * dt)` and feeds that into `Mathf.Lerp`/`Vector2.Lerp` — because this factor depends on elapsed time rather than being a fixed per-call constant, the approach rate stays consistent whether your game runs at 30 FPS or 144 FPS.

**[⬆ back to top](#table-of-contents)**

## License

MIT — see [LICENSE](LICENSE).
