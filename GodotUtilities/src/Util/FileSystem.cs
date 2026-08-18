using Godot;

namespace GodotUtilities;

/// <summary>
/// Utility methods for discovering and loading <see cref="Resource"/> files
/// from the Godot resource filesystem (e.g. <c>res://</c>, <c>user://</c>).
/// </summary>
public static class FileSystem
{
    /// <summary>
    /// Loads every resource of type <typeparamref name="T"/> found directly inside
    /// <paramref name="path"/>, optionally descending into subdirectories.
    /// </summary>
    /// <typeparam name="T">The <see cref="Resource"/> type to filter and load as.</typeparam>
    /// <param name="path">The directory path to scan (e.g. <c>res://assets/icons</c>).</param>
    /// <param name="recursive">If <c>true</c>, subdirectories are scanned as well.</param>
    /// <returns>
    /// A list of successfully loaded resources of type <typeparamref name="T"/>.
    /// Entries that fail to load or load as a different type are skipped and a
    /// warning is pushed via <see cref="GD.PushWarning(string)"/>.
    /// </returns>
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
            {
                GD.PushWarning($"Could not load resource at {fullPath} with type {typeof(T).Name}");
                continue;
            }
            results.Add(res);
        }

        return results;
    }

    public static List<T> InstantiateScenesInPath<T>(string dirPath) where T : Node
    {
        if (dirPath[^1] != '/')
            dirPath += "/";

        var scenes = new List<T>();
        var files = ResourceLoader.ListDirectory(dirPath);

        foreach (var fileName in files)
        {
            if (fileName.EndsWith('/')) continue;

            var fullPath = $"{dirPath}{fileName}";
            
            if (GD.Load(fullPath) is PackedScene packedScene)
            {
                var scene = packedScene.Instantiate();
                if (scene is T node)
                    scenes.Add(node);
                else
                    scene.QueueFree();
            }
        }

        return scenes;
    }
    
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
}
