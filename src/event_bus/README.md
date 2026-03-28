# EventBus

A lightweight, decoupled event system for Godot 4 (C#).  
Namespace: `Utilities.Events`

Lets nodes communicate without holding direct references to each other — no autoload singletons with exported fields, no `GetNode` chains, no tight coupling.

---

## Files

| File | Purpose |
|---|---|
| `EventBus.cs` | Core dispatch — add listeners, trigger events, remove listeners |
| `EventSubscriber.cs` | Reflection-based auto-wiring via `WireEvents()` |
| `EventAttribute.cs` | `[EventHandler]` attribute for marking subscriber methods |

---

## Setup

Add `using Utilities.Events;` to any file that uses the system.

```csharp
using Utilities.Events;
```

---

## Defining Events

Events are plain types. `record struct` is the recommended default — immutable, stack-allocated, and clean to declare:

```csharp
public record struct PlayerDied(int Score, float TimeAlive);
public record struct EnemySpawned(int EnemyId, Vector2 Position);
public record struct GameStarted();
```

Use `record` (class) only when the payload is large or contains reference types.

---

## Usage

There are two ways to subscribe to events. They can be mixed freely in the same node.

### Option A — Manual (typed)

Register listeners explicitly in `_Ready`. Best for high-frequency events or nodes where you need fine-grained control over lifetime.

```csharp
using Utilities.Events;

public partial class HUD : Node
{
    public override void _Ready()
    {
        // Listener is auto-removed when this node exits the tree
        EventBus.AddListener<PlayerDied>(OnPlayerDied, owner: this);
        EventBus.AddListener<GameStarted>(OnGameStarted, owner: this);
    }

    private void OnPlayerDied(PlayerDied e)
        => GD.Print($"Player died with score {e.Score}");

    private void OnGameStarted(GameStarted e)
        => GD.Print("Game started");
}
```

### Option B — Attribute-based (auto-wired)

Mark methods with `[EventHandler]` and call `this.WireEvents()` in `_Ready`. The event type is inferred from the method parameter — no extra configuration needed for most cases.

```csharp
using Utilities.Events;

public partial class HUD : Node
{
    public override void _Ready() => this.WireEvents();

    // Event type inferred from parameter
    [EventHandler]
    private void OnPlayerDied(PlayerDied e)
        => GD.Print($"Player died with score {e.Score}");

    // Explicit type — required when the method has no parameter
    [EventHandler(typeof(GameStarted))]
    private void OnGameStarted()
        => GD.Print("Game started");

    // Fires once then auto-removes itself
    [EventHandler(Once = true)]
    private void OnFirstKill(EnemySpawned e)
        => GD.Print("First kill!");
}
```

> `WireEvents()` must be called once per node. A second call on the same node logs a warning and is ignored.

---

## Triggering Events

Triggering is always manual — any node or system can fire an event at any time.

```csharp
// With data
EventBus.Trigger(new PlayerDied(Score: 1500, TimeAlive: 42.3f));

// Parameterless — instance is cached, no allocation
EventBus.Trigger<GameStarted>();
```

---

## API Reference

### `EventBus`

```csharp
// Register a typed listener — auto-removed when owner exits the tree
EventBus.AddListener<T>(Action<T> listener, Node owner = null)

// Register a parameterless listener — returns the wrapper delegate for manual removal
EventBus.AddListener<T>(Action listener, Node owner = null)

// Register a listener that fires once then removes itself
EventBus.AddListenerOnce<T>(Action<T> listener, Node owner = null)
EventBus.AddListenerOnce<T>(Action listener, Node owner = null)

// Remove a listener manually
EventBus.RemoveListener<T>(Action<T> listener)

// Trigger an event with data
EventBus.Trigger<T>(T evt)

// Trigger a parameterless event — T must have a default constructor
EventBus.Trigger<T>()

// Clear all listeners globally, or for a specific event type
EventBus.Clear()
EventBus.Clear<T>()
```

### `[EventHandler]`

```csharp
[EventHandler]                               // infer type from first method parameter
[EventHandler(typeof(GameStarted))]          // explicit type — required for parameterless methods
[EventHandler(Once = true)]                  // infer type, fire once then auto-remove
[EventHandler(typeof(GameStarted), Once = true)]
```

---

## Rules

**Do** register listeners in `_Ready` or initialization.  
**Do** pass `owner: this` to `AddListener` — it wires up automatic cleanup on tree exit.  
**Do not** call `AddListener` inside `_Process` or `_PhysicsProcess` — listeners accumulate every frame.  
**Do not** use `[EventHandler]` for per-frame events — use `AddListener<T>` manually for those.  
**Do not** call `WireEvents()` more than once on the same node.

---

## When to Use Each Option

| Scenario | Recommended approach |
|---|---|
| Per-frame / high-frequency events | `AddListener<T>` manually |
| Standard gameplay events (death, spawn, UI) | `[EventHandler]` + `WireEvents()` |
| One-shot events (tutorial trigger, first kill) | `[EventHandler(Once = true)]` |
| Listening from a non-node class | `AddListener<T>` manually, manage lifetime yourself |

---

## How It Works

**`WireEvents()`** scans the node's methods via reflection once at `_Ready` time. Each `[EventHandler]` method is wrapped in an `Action<object>` and registered with the bus. The reflection cost is one-time — it has no impact on runtime dispatch. A node can only be wired once; subsequent calls are ignored with a warning.

**`Trigger<T>`** iterates listeners backwards so `Once` handlers can safely remove themselves mid-loop without shifting unvisited indices. Each listener invocation is wrapped in a try/catch — one failing listener logs an error and dispatch continues to the remaining listeners.

**Lifetime management** is handled automatically when `owner` is provided. The owner's `TreeExiting` signal removes the listener from the bus — no manual cleanup needed in most cases.
