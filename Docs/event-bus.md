[← back to README](../README.md)

# EventBus

`GodotUtilities.Events`

A static, type-safe, global publish/subscribe event bus. Instead of wiring up Godot signals between distant nodes, you subscribe to a C# type and trigger it from anywhere.

```csharp
public readonly record struct PlayerDied(int Score);

// Subscribe
EventBus.AddListener<PlayerDied>(evt => GD.Print($"Score was {evt.Score}"), owner: SomeNode);

// Trigger from anywhere
EventBus.Trigger(new PlayerDied(Score: 1200));
```

Pass a `Node` as the `owner` parameter and the listener is automatically removed when that node emits `TreeExiting`, so you don't have to unsubscribe manually:

```csharp
EventBus.AddListener<PlayerDied>(OnPlayerDied, owner: this);
```

## API

| Member | Description |
| --- | --- |
| `AddListener<T>(Action<T> listener, Node owner = null)` | Subscribes `listener` to events of type `T`. With `owner`, the listener is removed when that node emits `TreeExiting`. |
| `RemoveListener<T>(Action<T> listener)` | Unsubscribes a listener. Returns `true` if it was registered. |
| `Trigger<T>(T evt)` | Fires every listener registered for `T`. A `null` event pushes an error and does nothing. |
| `Trigger<T>()` | Fires a default-constructed instance — `T` needs a parameterless constructor. |
| `Clear()` / `Clear<T>()` | Removes all listeners, or only those for `T` (handy between game restarts or scene reloads). |

## Behavior worth knowing

- **Exact type match.** Listeners are keyed by `typeof(T)`. Triggering a `Derived` event does not notify listeners of `Base` (or of an interface it implements).
- **Dispatch order** is subscription order.
- **Delegate identity.** A listener is identified by its delegate. Adding the same delegate twice pushes an error and is ignored. To remove a listener later, pass an equal delegate — a method group (`RemoveListener<PlayerDied>(OnPlayerDied)`) works; a fresh lambda never matches the one you added, so keep a reference if you need to remove it.
- **Changes during dispatch.** Removing a listener while an event is being dispatched takes effect once that dispatch finishes, so the removed listener can still receive the in-flight event. A listener added during dispatch also receives the in-flight event.
- **Exceptions** thrown by a listener propagate out of `Trigger` and skip the remaining listeners for that call; the bus stays usable afterwards.
- **Value-type events** (like the `readonly record struct` above) are dispatched without boxing, since each event type has its own generic bucket.
- **Lifetime.** The bus is static: listeners survive scene changes. Use `owner:` (or `[EventHandler]`, below), or call `Clear()` during teardown, so nothing outlives the objects it points at.

> **Threading contract:** every `EventBus` method must be called from the main Godot thread. There's no internal locking — calling from a background thread will silently corrupt state rather than throw.

## EventHandlerAttribute (declarative wiring)

An alternative to calling `EventBus.AddListener` manually: mark methods on a **partial** class deriving from `Godot.Node` with `[EventHandler]`, then call the generated `WireEvents()` method to wire them all up at once. A Roslyn source generator writes `WireEvents()` for you at compile time — no reflection involved — and each subscription is automatically removed when the node exits the tree.

```csharp
public partial class Player : CharacterBody2D
{
    // Recommended since it runs before _EnterTree();
    public override void _Notification(int what)
    {
        if (what == NotificationEnterTree)
            WireEvents();
    }

    [EventHandler]
    private void OnPlayerDied(PlayerDied evt) => GD.Print($"Final score: {evt.Score}");

    // No parameter to use? Specify the event type explicitly.
    [EventHandler(typeof(GamePaused))]
    private void OnGamePaused() => GD.Print("Game paused.");
}
```

The event type is inferred from the method's single parameter, or can be given explicitly via `[EventHandler(typeof(YourEvent))]` 

**When to call `WireEvents()`.** Prefer `_Notification(int what) with NotificationEnterTree` (or just `_EnterTree()`): subscriptions are removed on `TreeExiting`, so wiring on enter-tree restores them if the node is removed and added again (for example when reparenting).

**Once-per-node guard.** Calling `WireEvents()` a second time on the same node while it is still in the tree is a no-op (`EventBus` pushes an error, not an exception, so the game keeps running). The guard resets when the node exits the tree.

### Diagnostics

The generator reports problems at compile time:

| Id | Severity | Meaning |
| --- | --- | --- |
| `GUEVT001` | Error | The method has no parameters and no explicit event type. |
| `GUEVT002` | Error | The explicit event type doesn't match the method's parameter type. |
| `GUEVT003` | Error | The method takes more than one parameter (zero or exactly one is allowed). |
| `GUEVT004` | Error | Two `[EventHandler]` methods in the class handle the same event type. |
| `GUEVT005` | Error | The containing class isn't `partial`. |
| `GUEVT006` | Error | The containing class doesn't derive from `Godot.Node`. |
| `GUEVT007` | Error | The method is `static` — handlers must be instance methods. |
| `GUEVT008` | Error | The class is nested — move it to namespace scope, or wire events manually via `EventBus.AddListener`. |

The package also includes a suppressor (`GUEVTSUPP001`) that silences analyzer warning `CA1822` ("make member static") on valid `[EventHandler]` methods, since they must stay instance methods.


Looking to skip declarative event wiring and go straight to scene-tree lookups instead? See [Node Wiring](node-wiring.md) for the `[Node]`/`WireNodes()` counterpart, which resolves fields from child nodes rather than events.

[← back to README](../README.md)