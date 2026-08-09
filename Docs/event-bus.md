[← back to README](../README.md)

# EventBus

`GodotUtilities.Events`

A static, type-safe, global publish/subscribe event bus. Instead of wiring up Godot signals between distant nodes, you subscribe to a C# type and trigger it from anywhere.

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

Other members: `RemoveListener<T>`, `Trigger<T>()` (fires a default-constructed instance — `T` needs a parameterless constructor), and `Clear()` / `Clear<T>()` to wipe all or per-type listeners (handy between game restarts or scene reloads).

> **Threading contract:** every `EventBus` method must be called from the main Godot thread. There's no internal locking — calling from a background thread will silently corrupt state rather than throw.

## EventHandlerAttribute (declarative wiring)

An alternative to calling `EventBus.AddListener` manually: mark methods on a **partial** class deriving from `Godot.Node` with `[EventHandler]`, then call the generated `WireEvents()` method once (typically in `_Ready`) to wire them all up at once. A Roslyn source generator writes `WireEvents()` for you at compile time — no reflection involved — and each subscription is automatically removed when the node exits the tree.

```csharp
public partial class Player : CharacterBody2D
{
    public override void _Ready() => WireEvents();

    [EventHandler]
    private void OnPlayerDied(PlayerDied evt) => GD.Print($"Final score: {evt.Score}");

    // No parameter? Specify the event type explicitly.
    [EventHandler(typeof(GamePaused))]
    private void OnGamePaused() => GD.Print("Game paused.");
}
```

The event type is inferred from the method's single parameter, or can be given explicitly via `[EventHandler(typeof(YourEvent))]` for parameterless handlers.

A few rules the generator enforces, each with its own compiler diagnostic if broken:
- The containing class must be declared `partial` and must derive from `Godot.Node`.
- `[EventHandler]` methods must be instance methods (not `static`), and must take zero parameters or exactly one (the event).
- Only one `[EventHandler]` method per event type is allowed per class.
- Nested classes aren't supported — move the class to namespace scope, or wire events manually via `EventBus.AddListener`.
- Calling `WireEvents()` twice on the same node instance is a no-op past the first call (a warning is pushed, not an exception).

> `[EventHandler]`/`WireEvents()` requires the `SourceGenerators` project to be referenced as an analyzer — see the [setup guide](../README.md#setup). Without it, `EventBus.AddListener`/`RemoveListener` still work fine on their own.

[← back to README](../README.md)
