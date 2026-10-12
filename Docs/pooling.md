[← back to README](../README.md)

# ObjectPool&lt;T&gt; / NodePool&lt;T&gt;

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
var bulletPool = new NodePool<Bullet>(bulletScene, bulletContainer) { Extensible = true };
bulletPool.Prewarm(20);

Bullet bullet = bulletPool.Get(); // enabled + visible, ready to use
// ... later ...
bulletPool.Release(bullet); // disabled + hidden, returned to the pool
```

## Constructors

```csharp
new ObjectPool<T>(Func<T> factory, bool extensible = true,
                  Action<T> onGet = null, Action<T> onRelease = null)

new NodePool<T>(PackedScene packedScene, Node root)   // T : Node
```

`ObjectPool<T>`'s optional `onGet` / `onRelease` callbacks are an alternative to implementing `IPoolable` when you don't own the type:

## Members

Both pools share the same shape:

- `Get()` / `TryGet(out value)` — retrieve an instance; `TryGet` returns `false` on exhaustion instead of pushing a warning. `Get()` pushes a warning and returns `null`/`default` when the pool is exhausted and not extensible.
- `Release(obj)` / `TryRelease(obj)` — return an instance; `TryRelease` returns `false` if it wasn't tracked as active (already released, or from another pool), while `Release` pushes a warning in that case.
- `Prewarm(count)` — eagerly create instances up front to avoid allocation spikes mid-game.
- `ReleaseAll()` — return every active instance to the pool at once (e.g. on wave clear).
- `Trim(targetSize, onTrim?)` — shrink the free (unused) pool down to `targetSize`, discarding the rest. Active instances are untouched.
- `Extensible` — if `true` (default), the pool creates a new instance when exhausted instead of failing. Settable at any time.
- `ActiveCount` / `FreeCount` / `TotalCount` — introspection.
- `Clear()` — empties the pool entirely. `NodePool<T>` deferred-frees every node it tracks (active and free); `ObjectPool<T>` releases everything and discards it.

`NodePool<T>` also exposes `CreateNew()`, which instantiates a node, parents it under the root and leaves it inactive — `Prewarm` and `Get` use it, and it throws `NullReferenceException` if the root node has been freed.

Free instances are reused most-recently-released first (a stack).

## IPoolable

If your pooled type implements `IPoolable` (`OnGet()` / `OnRelease()`), both pools call it automatically on checkout/return — put your "reset state" logic there instead of scattering it at call sites. `ObjectPool<T>` calls `IPoolable` first, then the `onGet`/`onRelease` callback.

## Details worth knowing

- **Invalid nodes are skipped.** `NodePool<T>` (and `ObjectPool<T>` for `GodotObject`s) discards freed instances it finds at the top of the free stack instead of handing out invalid references, and `NodePool<T>.ReleaseAll()`/`Clear()` skip nodes that were freed elsewhere (e.g. via `QueueFree` outside the pool).
- **What "inactive" means for nodes.** Releasing sets `ProcessMode` to `Disabled` and hides the node (`CanvasItem` or `Node3D`); getting it sets `ProcessMode` back to `Inherit` and shows it. A custom `ProcessMode` set on the scene's root (e.g. `Always`) is therefore replaced by `Inherit` after the first checkout. Pooled nodes also stay in the tree, so anything not covered by those two switches — collision shapes, audio players, particles — should be reset in `OnRelease()`/`OnGet()`.
- **`Trim` on `NodePool<T>`** runs only your `onTrim` callback, then deferred-frees each trimmed node; it does not call `IPoolable.OnRelease()` (free nodes were already released).
- **Active tracking uses `T`'s equality.** Both pools track active instances in a `HashSet<T>`, so pool reference types (or types with identity-style equality). Value types and `record`s with value equality will collide when two live instances compare equal.
- **Main thread only.** The pools aren't thread-safe.

[← back to README](../README.md)