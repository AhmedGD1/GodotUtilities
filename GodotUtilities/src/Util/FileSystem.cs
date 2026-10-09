using Godot;

namespace GodotUtilities;

/// <summary>
/// Utility methods for discovering and loading <see cref="Resource"/> files
/// from the Godot resource filesystem (e.g., <c>res://</c>, <c>user://</c>).
/// </summary>
public static class FileSystem
{
    /// <summary>
    /// Recursively or non-recursively loads all resources of type <typeparamref name="T"/> within a specified directory.
    /// </summary>
    /// <typeparam name="T">The specific type of <see cref="Resource"/> to load and filter by.</typeparam>
    /// <param name="path">The directory path to search in (e.g., <c>res://Assets/Textures/</c>).</param>
    /// <param name="recursive"><c>true</c> to scan subdirectories recursively; otherwise, <c>false</c>.</param>
    /// <returns>A list of loaded resources matching type <typeparamref name="T"/>.</returns>
    public static List<T> LoadResourcesInPath<T>(string path, bool recursive = false) where T : Resource
    {
        if (path[^1] != '/')
            path += "/";

        var results = new List<T>();
        var entries = ResourceLoader.ListDirectory(path);

        foreach (var entry in entries)
        {
            if (entry.EndsWith('/'))
            {
                if (recursive)
                    results.AddRange(LoadResourcesInPath<T>(path + entry, recursive));
                continue;
            }

            var fullPath = path + entry;
            var loadedRes = GD.Load(fullPath);

            if (loadedRes is not T res)
                continue;

            results.Add(res);
        }

        return results;
    }

    /// <summary>
    /// Iterates through all entries in a given directory and executes a specified action for each file.
    /// </summary>
    /// <param name="path">The directory path to search in.</param>
    /// <param name="fileAction">
    /// A callback action invoked for each file. Passes the file name as the first argument 
    /// and the full resource path as the second argument.
    /// </param>
    /// <param name="includeSubdirectories"><c>true</c> to iterate through subdirectories recursively; otherwise, <c>false</c>.</param>
    public static void ForResourcesInDirectory(string path, Action<string, string> fileAction, bool includeSubdirectories = false)
    {
        var files = ResourceLoader.ListDirectory(path);

        foreach (var file in files)
        {
            if (file.EndsWith('/') && includeSubdirectories)
            {
                ForResourcesInDirectory($"{path}/{file}", fileAction, includeSubdirectories);
                continue;
            }

            fileAction(file, $"{path}/{file}");
        }
    }

    /// <summary>
    /// Scans a directory for valid resource files and returns a dictionary mapping a snake_case key to its full path.
    /// </summary>
    /// <param name="directory">The directory path to search in.</param>
    /// <param name="recursive"><c>true</c> to search subdirectories recursively; otherwise, <c>false</c>.</param>
    /// <returns>
    /// A dictionary where each key is a <see cref="StringName"/> derived from the file's base name in snake_case, 
    /// and the value is the full resource path string.
    /// </returns>
    public static Dictionary<StringName, string> GetResourcePaths(string directory, bool recursive = false)
    {
        var paths = new Dictionary<StringName, string>();

        ForResourcesInDirectory(
            directory,
            (file, path) =>
            {
                if (!ResourceLoader.Exists(path))
                    return;

                var id = file.GetBaseName().ToSnakeCase();
                paths.TryAdd(id, path);
            },
            includeSubdirectories: recursive
        );

        return paths;
    }
}

