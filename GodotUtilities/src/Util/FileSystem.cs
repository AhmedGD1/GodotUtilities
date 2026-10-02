using Godot;

namespace GodotUtilities;

/// <summary>
/// Utility methods for discovering and loading <see cref="Resource"/> files
/// from the Godot resource filesystem (e.g. <c>res://</c>, <c>user://</c>).
/// </summary>
public static class FileSystem
{
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
