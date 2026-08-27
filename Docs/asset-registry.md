[← back to README](../README.md)

# AssetRegistry

`GodotUtilities`

Maps human-friendly `StringName` ids to resource paths, so the rest of your code can load assets by name (`"player_idle"`) instead of hardcoding `res://` paths everywhere.

```csharp
var registry = new AssetRegistry();

// Register a whole folder — each file gets an id auto-derived from its filename (snake_case)
registry.RegisterDirectory("res://assets/textures", recursive: true);

// Or register individual entries manually
registry.Register("player_icon", "res://ui/icons/player.png");

// Load by id later, anywhere in your code
Texture2D icon = registry.Load<Texture2D>("player_icon");

// Or the non-throwing version
if (registry.TryLoad<Texture2D>("enemy_icon", out var enemyIcon))
    sprite.Texture = enemyIcon;
```

Also exposes `TryRegister` / `TryRegisterAuto` (non-overwriting variants, id derived from filename), `Unregister`, `Contains`, `Clear`, and introspection via `GetIds()`, `GetPaths()`, `GetMap()`, `GetPath` / `TryGetPath`. Pass a `HashSet<string>` of allowed extensions to the constructor to restrict which files `RegisterDirectory` picks up; `.uid` and `.import` files are always skipped regardless.

> `RegisterDirectory` derives ids from filenames alone (not full paths), so two files with the same basename in different subfolders will collide — `Register` (and therefore `RegisterAuto`) pushes a warning and overwrites the existing entry rather than failing silently.

[← back to README](../README.md)
