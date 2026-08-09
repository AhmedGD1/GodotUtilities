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
var bulletPool = new NodePool<Bullet>(bulletScene, bulletContainer) { Extendible = true };
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
- `Extendible` — if `true` (default), the pool creates a new instance when exhausted instead of failing.
- `ActiveCount` / `FreeCount` / `TotalCount` — introspection.
- `Clear()` — empties the pool entirely. `NodePool<T>` deferred-frees every node it tracks (active and free); `ObjectPool<T>` just releases and discards.

If your pooled type implements `IPoolable` (`OnGet()` / `OnRelease()`), both pools call it automatically on checkout/return — put your "reset state" logic there instead of scattering it at call sites. `NodePool<T>` also safely skips/discards instances it finds freed elsewhere (e.g. via `QueueFree` outside the pool) instead of returning invalid references.

[← back to README](../README.md)
