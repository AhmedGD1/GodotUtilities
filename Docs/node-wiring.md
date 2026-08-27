[← back to README](../README.md)

# Node Wiring (`[Node]` / `WireNodes()`)

`GodotUtilities`

Declarative scene-tree wiring: mark a field or property with `[Node]`, then call the generated `WireNodes()` once (typically in `_Ready`) to resolve every annotated member from the scene tree — no more repetitive `GetNode<T>("Path/To/Thing")` calls scattered through `_Ready`.

```csharp
public partial class Player : CharacterBody2D
{
    [Node] private AnimationPlayer AnimationPlayer;
    [Node] private Sprite2D Sprite;

    // Explicit path when the name doesn't match the node's own name
    [Node("UI/HealthBar")] private ProgressBar HealthBar;

    public override void _Ready() => WireNodes();
}
```

Like `[EventHandler]`/`WireEvents()`, `WireNodes()` is written for you at compile time by a Roslyn source generator — no reflection involved.

## Resolution order

For each `[Node]` member, `WireNodes()` tries, in order, until one succeeds:

1. The explicit path passed to the attribute (`[Node("UI/HealthBar")]`), if given.
2. The member name converted to PascalCase, snake_case, and camelCase — each tried both as a normal node path and as a [unique name](https://docs.godotengine.org/en/stable/tutorials/scripting/scene_unique_nodes.html) (`%Name`).
3. A case- and underscore-insensitive match against this node's **direct children** (computed once up front, not rescanned per member). If the match found this way isn't exactly one of the member's canonical name forms above, a warning is pushed noting it was a best-guess match.
4. If nothing matches at all, an error is printed and the member is left unassigned (`null`/default).

This means a field like `HealthBar` will resolve to a child literally named `HealthBar`, `health_bar`, `healthBar`, or a unique-named `%HealthBar`, without you needing to specify a path — and if the child is instead named something loosely similar, it'll still be found via the fallback, with a warning so you know to tidy up the name or add an explicit path.

## Rules the generator enforces

Each of these has its own compiler diagnostic (`GUNOD001`–`GUNOD010`):

- The containing class must be declared `partial` and must derive from `Godot.Node` (`WireNodes()` uses `GetNodeOrNull<T>`, which requires it).
- The `[Node]` member's type must itself derive from `Godot.Node`.
- `[Node]` members must be instance members — not `static`.
- `[Node]` fields can't be `readonly`; `[Node]` properties need an accessible, non-`init` setter and can't be `required` — `WireNodes()` assigns after construction, so the member has to still be writable at that point.
- Two `[Node]` members resolving to the same path in the same class is a warning (`GUNOD006`), not an error — but almost always a mistake.
- An empty explicit path (`[Node("")]`) is a warning; omit the argument instead to fall back to name-based resolution.

> `[Node]`/`WireNodes()` requires the `SourceGenerators` project to be referenced as an analyzer — see the [setup guide](../README.md#setup). Without it, the attribute has no effect and `WireNodes()` won't be generated.

Looking for event-driven wiring instead of scene-tree wiring? See [EventBus](event-bus.md) for `[EventHandler]`/`WireEvents()`.

[← back to README](../README.md)
