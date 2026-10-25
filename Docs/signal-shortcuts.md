[← back to README](../README.md)

# Signal Shortcuts (`[Signal]`)

Generated on your own class — no `using` needed.

For every `[Signal]` delegate you declare, a Roslyn source generator adds four small methods to the same class: connect, disconnect, check, and await. Handlers are plain `Action` / `Action<...>` delegates, so you can pass a method group or a lambda and never touch `Callable.From(...)` or `SignalName` yourself.

```csharp
public partial class Enemy : Node3D
{
    [Signal] public delegate void DiedEventHandler();
    [Signal] public delegate void DamagedEventHandler(int amount, Node3D source);
}

// Anywhere that has an Enemy:
enemy.ConnectToDied(OnEnemyDied);
enemy.ConnectToDamaged((amount, source) => GD.Print($"{amount} from {source.Name}"));

enemy.DisconnectFromDied(OnEnemyDied);

await enemy.ToSignalDied();
```

Like `[Node]`/`WireNodes()` and `[EventHandler]`/`WireEvents()`, the methods are written at compile time — no reflection involved.

## Generated members

For a signal declared as `MySignalEventHandler(T0 a, T1 b, ...)`:

| Member | Description |
| --- | --- |
| `Error ConnectToMySignal(Action<T0, T1, ...> handler, ConnectFlags flags = 0)` | Connects `handler` to the signal. Returns the result of `Connect` (`Error.Ok` on success). |
| `bool DisconnectFromMySignal(Action<T0, T1, ...> handler)` | Disconnects `handler`. Returns `true` if it was connected and has been disconnected, `false` otherwise (nothing is logged). |
| `bool IsConnectedToMySignal(Action<T0, T1, ...> handler)` | Whether `handler` is currently connected. |
| `SignalAwaiter ToSignalMySignal()` | Returns an awaiter that completes the next time the signal is emitted. |

A signal with no parameters uses `Action`. The parameter types are copied from your delegate.

The generated methods have the **same accessibility as your delegate**. A `protected` delegate in a `sealed` class is folded to `private` (and the other protected variants likewise), the same way Godot folds its own generated members.

## Connect flags

`flags` takes the usual `GodotObject.ConnectFlags`:

```csharp
enemy.ConnectToDied(OnEnemyDied, GodotObject.ConnectFlags.OneShot);
enemy.ConnectToDamaged(OnDamaged, GodotObject.ConnectFlags.Deferred);
```

## Awaiting a signal

`ToSignalMySignal()` wraps `ToSignal(this, SignalName.MySignal)`. The awaited result holds the signal's arguments as a `Variant[]`:

```csharp
await enemy.ToSignalDied();

var args = await enemy.ToSignalDamaged();
int amount = args[0].AsInt32();
```

## Disconnecting

`DisconnectFromMySignal` and `IsConnectedToMySignal` find the connection by comparing against the handler you pass, so it has to be **the same method or the same delegate instance** that was connected:

```csharp
enemy.ConnectToDied(OnEnemyDied);
enemy.DisconnectFromDied(OnEnemyDied);   // works: same method

enemy.ConnectToDied(() => GD.Print("died"));
// A second, textually identical lambda is a different delegate and won't match.
// Keep the delegate in a variable if you need to disconnect it later:
Action onDied = () => GD.Print("died");
enemy.ConnectToDied(onDied);
enemy.DisconnectFromDied(onDied);
```

The shortcuts don't tie a handler to the lifetime of the node that owns it. If the subscriber can be freed before the emitter, disconnect in `_ExitTree()`.

## Shortcuts or plain C# events?

Godot already generates a C# event for every `[Signal]` (`enemy.Died += OnEnemyDied`), and for simple cases that's perfectly fine. The shortcuts add:

- connect flags (`OneShot`, `Deferred`, ...) without building a `Callable`;
- an `Error` result from connecting;
- `IsConnectedTo`, and a `bool` result from disconnecting;
- `ToSignalMySignal()` for `await`;
- `Action`-typed handlers, so lambdas and method groups work directly.

They don't replace the event, and you can mix both on the same signal.

## Rules the generator follows

- The class — and every type it is nested in — must be declared `partial` and derive from `Godot.GodotObject`. Nested classes **are** supported, as long as every enclosing type is `partial`.
- Problems that Godot already reports itself (missing `partial`, a delegate name without the `EventHandler` suffix, unsupported parameter types, non-`void` return) are **not** reported a second time. The generator just skips that signal.
- Delegates with `ref`/`out`/`in` parameters are skipped.
- Shortcuts are generated only if the class has at least one valid `[Signal]` delegate.

### Diagnostics

| Id | Severity | Meaning |
| --- | --- | --- |
| `GUSIG001` | Warning | The signal has more parameters than `Callable.From` supports (9). No shortcuts were generated for it. |
| `GUSIG002` | Warning | The class already declares a member named `ConnectToMySignal`, `DisconnectFromMySignal`, `IsConnectedToMySignal` or `ToSignalMySignal`. No shortcuts were generated for that signal. Rename the member or the signal. |

## Setup

The generators ship in the `SourceGenerators` project, which has to be referenced separately — see [Setup](../README.md#setup). Without that reference, nothing here is generated.

Looking for scene-tree or event-bus wiring instead? See [Node Wiring](node-wiring.md) and [EventBus](event-bus.md).

[← back to README](../README.md)
