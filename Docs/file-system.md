[← back to README](../README.md)

# FileSystem

`GodotUtilities`

Static helpers for discovering and loading `Resource`s directly from the Godot resource filesystem (`res://`, `user://`), without hand-rolling `DirAccess`/`ResourceLoader` scanning code each time.

```csharp
// Load every texture directly inside a folder as Texture2D
var icons = FileSystem.LoadResourcesInPath<Texture2D>("res://ui/icons/");

// ...or descend into subfolders too
var allTextures = FileSystem.LoadResourcesInPath<Texture2D>("res://assets/", recursive: true);

// Build a name -> path map, then load by name
var paths = FileSystem.GetResourcePaths("res://assets/textures/", recursive: true);
Texture2D idle = ResourceLoader.Load<Texture2D>(paths["player_idle"]);
```

## Members

- `LoadResourcesInPath<T>(path, recursive = false)` — loads every resource directly inside `path` that loads as `T`, returning them as a `List<T>`. Entries that fail to load, or load as a different type, are skipped. Pass `recursive: true` to also descend into subdirectories. `path` must not be empty; a trailing `/` is added if missing.
- `GetResourcePaths(directory, recursive = false)` — scans `directory` and returns a `Dictionary<StringName, string>` mapping an id to the resource's full path. The id is the file's name without its extension, converted to snake_case (`PlayerIdle.png` → `player_idle`). Only entries for which `ResourceLoader.Exists` is true are included. If two files produce the same id (the same file name in different folders, or `Hero.png` and `hero.tres`), the **first one found wins** and later ones are silently skipped.
- `ForResourcesInDirectory(path, fileAction, includeSubdirectories = false)` — the lower-level building block `GetResourcePaths` is built on: walks `path`, invoking `fileAction(fileName, fullPath)` for every entry found. With `includeSubdirectories: true` it recurses into folders; with `false`, folder entries (names ending in `/`) are passed to the callback like any other entry, so filter them out if you only want files.


## Migrating from older versions

- **`AssetRegistry` was removed.** Its job — mapping friendly ids to paths — is now `FileSystem.GetResourcePaths`. Keep the returned dictionary yourself (or wrap it in your own class) and load with `ResourceLoader.

[← back to README](../README.md)