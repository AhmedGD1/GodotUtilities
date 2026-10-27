[← back to README](../README.md)

# Singletons (`[Singleton]`)

Generated on your own class — no `using` needed beyond `GodotUtilities`.

Mark a `partial` class with `[Singleton]` and a Roslyn source generator writes the static `Instance` (and the lookup/registration code behind it) for you. Which code it writes depends on how the instance comes to exist.

```csharp
[Singleton]                                   // Scene (default)
public partial class LevelManager : Node { }

[Singleton(SingletonMode.Autoload)]           // registered in Project Settings > Globals > Autoload
public partial class GameState : Node { }

[Singleton(SingletonMode.Lazy)]               // created on first access
public partial class AudioManager : Node { }

// Anywhere:
LevelManager.Instance.LoadNext();
```

Like the other generators, it is written at compile time — no reflection involved.

## Modes

| Mode | Who creates the instance | How `Instance` is found |
| --- | --- | --- |
| `Scene` (default) | You, by placing the node in a scene | Found in the scene tree by type on first access, then cached |
| `Autoload` | Godot, from Project Settings | Looked up under the scene tree root by name (the class name, or `Name = "..."`) |
| `Lazy` | The generated code, on the first `Instance` access | Created (and kept) by `Instance` |

### Scene

The generator can't create the node: **you place it in a scene**, and it is only available once that scene has loaded. So `Scene` mode is built around "it might not be there yet":

```csharp
LevelManager.Instance;                        // throws InvalidOperationException if there is none
LevelManager.HasInstance;                     // bool
LevelManager.TryGetInstance(out var level);   // bool, no exception
LevelManager.WhenReady(level => level.Start());
```

- `Instance` throws with a message that says what to do, instead of a `NullReferenceException` somewhere unrelated.
- `WhenReady(callback)` runs the callback right away if the instance is in the tree, otherwise once, after a node of the class is added (deferred, so it is ready by then). Use it when you may run before the scene has loaded.
- **Nothing is overridden.** The first access walks the scene tree for a node of the class and caches it, so your class keeps its own `_EnterTree` and `_ExitTree`. The lookup is by type, not name, so renaming the node in the editor is fine, and it works even from the `_EnterTree` of a node that comes earlier in the scene.
- A cached node that was freed, queued for freeing or removed from the tree no longer counts. The next access searches again, so scene changes and reloads pick up the new node.
- If two nodes of the class are in the tree, the first one found wins. There is no duplicate warning.
- **Cost:** while no instance exists, each `Instance`, `HasInstance` or `TryGetInstance` call walks the tree. Once an instance is cached the check is a few native calls. If you poll `TryGetInstance` every frame in a place where the singleton may be absent (a main menu), prefer `WhenReady`.

### Autoload

```csharp
[Singleton(SingletonMode.Autoload)]                 // looks for /root/GameState
public partial class GameState : Node { }

[Singleton(SingletonMode.Autoload, Name = "Game")]  // autoload is named "Game" in Project Settings
public partial class GameState : Node { }
```

`Instance` looks the node up under the root and caches it. If it isn't there it throws, telling you to add the autoload with that exact name. `HasInstance` and `TryGetInstance` are available as in `Scene` mode.

Godot adds autoloads to the tree one after another, in the order listed in Project Settings. An autoload can't see one listed *after* it from its own `_EnterTree`, but everything is there by `_Ready`.

### Lazy

For a singleton you want to behave like an autoload without registering it by hand: the first `Instance` access creates it.

```csharp
[Singleton(SingletonMode.Lazy)]
public partial class AudioManager : Node { }

// A singleton that needs child nodes: instantiate a scene instead of `new`.
[Singleton(SingletonMode.Lazy, ScenePath = "res://managers/pool.tscn", Name = "Pool")]
public partial class PoolManager : Node { }
```

- A node is added to the scene tree root, **deferred** (adding to the root while it is still setting up its children — at startup, or from an autoload's `_Ready` — fails). So on the first access the node exists and is returned, but it is only in the tree and `_Ready` once the current frame's deferred calls run.
- `Name` is the name given to the node (default: the class name). If a node of that class with that name is already under the root, it is reused.
- `ScenePath` instantiates that scene; its root node must be this class.
- A `Lazy` singleton doesn't have to be a node. Any class with a parameterless constructor works (`RefCounted`, `Resource`, or a plain C# class), and is created with `new`.
- `HasInstance` and `TryGetInstance` report whether it has already been created. They never create it.

## Generated members

| Member | Modes | Description |
| --- | --- | --- |
| `static T Instance` | all | The instance (see the mode sections for what happens when there isn't one). |
| `static bool HasInstance` | all | Whether the instance exists (`Lazy`: has been created). |
| `static bool TryGetInstance(out T instance)` | all | The instance, without throwing (`Lazy`: without creating). |
| `static void WhenReady(Action<T> callback)` | `Scene` | Run now, or once a node of the class is added to the tree. |

The generated class members are `public` (`Instance`, `HasInstance`, `TryGetInstance`, `WhenReady`) regardless of the class's own accessibility. Nested classes are supported; the containing types just need to be `partial` too.

## Diagnostics

| Id | Severity | Meaning |
| --- | --- | --- |
| `GUSNG001` | Error | The class (or a type around it) isn't `partial`. |
| `GUSNG002` | Error | The class is abstract, static or generic (or nested in a generic type). |
| `GUSNG003` | Error | `Scene`/`Autoload` mode on a class that doesn't derive from `Node`. |
| `GUSNG004` | Error | `Lazy` on a non-node class with no parameterless constructor. |
| `GUSNG005` | Error | The class already declares a member the generator needs (`Instance`, `HasInstance`, `TryGetInstance`, `WhenReady`, ...). |
| `GUSNG006` | Warning | `Name` or `ScenePath` was given where it has no effect. |

When an error is reported for a class, nothing is generated for it.

## Limits

- Singletons are meant for the main thread, like the rest of the scene tree. The generated code does no locking.
- In `[Tool]` scripts running in the editor, the scene tree root is the *editor's* root, not the scene being edited, so `Scene`'s fallback search and `Autoload`/`Lazy` lookups don't see the edited scene.
