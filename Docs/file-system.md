[← back to README](../README.md)

# FileSystem

`GodotUtilities`

Static helpers for discovering and loading `Resource`s directly from the Godot resource filesystem (`res://`, `user://`), without hand-rolling `DirAccess`/`ResourceLoader` scanning code each time.

```csharp
// Load every texture directly inside a folder as Texture2D
var icons = FileSystem.LoadResourcesInPath<Texture2D>("res://ui/icons");

// ...or descend into subfolders too
var allTextures = FileSystem.LoadResourcesInPath<Texture2D>("res://assets", recursive: true);

// Instantiate every scene in a folder as a given node type
var enemyScenes = FileSystem.InstantiateScenesInPath<Enemy>("res://enemies");
```

- `LoadResourcesInPath<T>(path, recursive = false)` — loads every resource directly inside `path` that successfully loads as `T`, returning them as a `List<T>`. Entries that fail to load, or load as a different type, are skipped with a pushed warning rather than throwing. Pass `recursive: true` to also descend into subdirectories.
- `InstantiateScenesInPath<T>(dirPath)` — loads every `.tscn`/`PackedScene` directly inside `dirPath` and instantiates each one, keeping only instances that are (or derive from) `T`. Instances that don't match `T` are freed immediately (`QueueFree`) rather than leaked.
- `ForResourcesInDirectory(path, fileAction, includeSubdirectories = false)` — the lower-level building block both of the above (and `AssetRegistry.RegisterDirectory`) are built on: walks `path`, invoking `fileAction(fileName, fullPath)` for every file found. Pass `includeSubdirectories: true` to recurse.

> These operate on whatever `ResourceLoader.ListDirectory` can see, so they work against exported PCK contents as well as the editor filesystem — unlike `DirAccess`-based scanning, which only sees loose files.

[← back to README](../README.md)
