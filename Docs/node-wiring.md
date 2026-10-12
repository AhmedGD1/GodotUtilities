[← back to README](../README.md)

# Node Wiring (`[Node]` / `WireNodes()`)

`GodotUtilities`

Declarative scene-tree wiring: mark a field or property with `[Node]`, then call the generated `WireNodes()` once to resolve every annotated member from the scene tree — no more repetitive `GetNode<T>("Path/To/Thing")` calls scattered through `_Ready`.

```csharp
public partial class Player : CharacterBody2D
{
    [Node] private AnimationPlayer AnimationPlayer;
    [Node] private Sprite2D Sprite;

    // Explicit path when the name doesn't match the node's own name
    [Node("UI/HealthBar")] private ProgressBar HealthBar;

    public override void _Notification(int what)
    {
        if (what == NotificationSceneInstantiated)
            WireNodes();
    }
}
```

Like `[EventHandler]`/`WireEvents()`, `WireNodes()` is written for you at compile time by a Roslyn source generator — no reflection involved.

## Resolution order

For each `[Node]` member, `WireNodes()` tries the following candidates in order until one succeeds:

1. The explicit path passed to the attribute (`[Node("UI/HealthBar")]`), if given.
2. The member name converted to **PascalCase**, then the **member name as written** (leading underscores removed), then **snake_case**, then **camelCase** — duplicates are skipped.

Every candidate is tried as a normal node path, and (when it contains no `/`) also as a [unique name](https://docs.godotengine.org/en/stable/tutorials/scripting/scene_unique_nodes.html) (`%Name`).

| Member | Names tried |
| --- | --- |
| `AnimationPlayer` | `AnimationPlayer`, `animation_player`, `animationPlayer` |
| `_healthBar` | `HealthBar`, `healthBar`, `health_bar` |
| `[Node("UI/HealthBar")] HealthBar` | `UI/HealthBar`, `HealthBar`, `health_bar`, `healthBar` |


## Rules the generator enforces

Each of these has its own compiler diagnostic. A broken member is skipped (the others are still wired); a broken containing type skips the whole class.

- The containing type — and every type it is nested in — must be declared `partial`, and it must derive from `Godot.Node`. Nested classes **are** supported here (unlike `[EventHandler]`) as long as every enclosing type is `partial`.
- The `[Node]` member's type must itself derive from `Godot.Node`.
- `[Node]` members must be instance members — not `static`.
- `[Node]` fields can't be `readonly`; `[Node]` properties need an accessible, non-`init` setter and can't be `required` — `WireNodes()` assigns after construction, so the member has to still be writable at that point.
- Two `[Node]` members resolving to the same path in the same class is a warning, not an error — but almost always a mistake.
- An empty (or whitespace-only) explicit path (`[Node("")]`) is a warning; omit the argument instead to fall back to name-based resolution.

`WireNodes()` is only generated if the class has at least one valid `[Node]` member.

### Diagnostics

| Id | Severity | Meaning |
| --- | --- | --- |
| `GUNOD001` | Error | The containing type (or an enclosing type) isn't `partial`. |
| `GUNOD002` | Error | The containing type doesn't derive from `Godot.Node`. |
| `GUNOD003` | Error | The member's type doesn't derive from `Godot.Node`. |
| `GUNOD004` | Error | The member is `static`. |
| `GUNOD005` | Error | The property has no accessible `set` accessor. |
| `GUNOD006` | Warning | Two members in the type resolve to the same node path. |
| `GUNOD007` | Warning | The explicit path is empty. |
| `GUNOD008` | Error | The property's setter is `init`-only. |
| `GUNOD009` | Error | The field is `readonly`. |
| `GUNOD010` | Error | The property is `required`. |

Looking for event-driven wiring instead of scene-tree wiring? See [EventBus](event-bus.md) for `[EventHandler]`/`WireEvents()`.

[← back to README](../README.md)