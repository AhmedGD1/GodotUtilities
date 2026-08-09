[← back to README](../README.md)

# PhysicsQuery2D

`GodotUtilities.Logic`

Static helpers for 2D physics-space queries — raycasts, shape/circle checks, and shape/circle overlaps — without needing to hold a reference to a specific node's physics space.

```csharp
if (PhysicsQuery2D.Raycast(from, to, out RaycastHit hit, collisionMask: 1))
    GD.Print($"Hit {hit.Collider.GetType().Name} at {hit.Position}");

if (PhysicsQuery2D.CheckCircle(position, radius: 32f, out CollisionObject2D collider))
    GD.Print($"Something is here: {collider}");

CollisionObject2D[] nearby = PhysicsQuery2D.OverlapCircle(position, radius: 64f, maxResults: 8);
```

- `Raycast` casts a ray and returns the closest hit (including `Area2D`s) as a `RaycastHit` struct (`Position`, `Normal`, `Collider`, `ColliderRid`).
- `Check` / `CheckCircle` test whether a `Shape2D` (or circle) at a position overlaps anything and return the first collider found.
- `Overlap` / `OverlapCircle` return every collider overlapping a shape or circle, up to a max result count.
- `IntersectShape` is the lower-level primitive behind the above, for when you need more than just the collider (e.g. shape index or RID).

**How it works:** all queries run through `PhysicsDirectSpaceState2D`. By default the space is resolved lazily from the main scene tree's root viewport (via `Engine.GetMainLoop()`), so you can call these methods from anywhere without passing a node around. If you're working inside a `SubViewport` with its own physics world, call `PhysicsQuery2D.SetWorld(world)` once to override which space subsequent queries run against, and `ResetWorld()` to go back to the default.

> Because the configured world is static/global, avoid interleaving queries against different worlds from the same frame without bracketing each with `SetWorld`/`ResetWorld` — e.g. a minimap and the main game querying different `World2D`s concurrently.

[← back to README](../README.md)
