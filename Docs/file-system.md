[← back to README](../README.md)

# FileSystem

`GodotUtilities`

Static helpers for discovering and bulk-loading `Resource` files directly from the Godot filesystem (`res://`, `user://`) by type, without needing to register them first.

```csharp
// Load every SpriteFrames resource under a folder
List<SpriteFrames> allFrames = FileSystem.LoadResourcesInPath<SpriteFrames>("res://sprites", recursive: true);

// Or just get the paths without loading, e.g. to lazy-load later
List<string> paths = FileSystem.ScanFolder<PackedScene>("res://levels");
```

- `LoadResourcesInPath<T>(path, recursive)` — loads and returns every resource of type `T` found in a folder. Entries that fail to load or load as a different type are skipped, with a warning pushed for each.
- `ScanFolder<T>(path, recursive)` — same scan, but returns matching resource *paths* only, without loading them.

[← back to README](../README.md)
