using Godot;

namespace GodotUtilities;

/// <summary>
/// Maps human-friendly <see cref="StringName"/> ids to resource paths, and provides
/// helpers for bulk-registering a folder and loading registered resources by id.
/// </summary>
/// <param name="validExtensions">
/// If provided, restricts <see cref="RegisterFolder"/> to only these file extensions
/// (without the leading dot, e.g. <c>"tres"</c>). Files with extensions outside this
/// set are skipped during folder scans. If <c>null</c>, all extensions are allowed
/// except those in the built-in excluded set (<c>"uid"</c>, <c>"import"</c>), which
/// is always applied regardless of this parameter.
/// </param>
public sealed class AssetRegistry(HashSet<string> validExtensions = null)
{
    private static readonly HashSet<string> DefaultExludedExtensions = ["uid", "import"];
    
    private readonly Dictionary<StringName, string> _map = [];

    private bool IsSupportedFile(string fileName)
    {
        var extension = fileName.GetExtension().ToLowerInvariant();
        return !DefaultExludedExtensions.Contains(extension) 
            && (validExtensions?.Contains(extension) ?? true);
    }

    private static StringName DeriveId(string path)
    {
        return path.GetFile().GetBaseName().ToSnakeCase();
    }

    #region Register

    /// <summary>
    /// Attempts to register <paramref name="path"/> under <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The id to register the asset under.</param>
    /// <param name="path">The resource path to associate with <paramref name="id"/>.</param>
    /// <returns><c>true</c> if the id was not already registered; otherwise <c>false</c>.</returns>
    public bool TryRegister(StringName id, string path) => _map.TryAdd(id, path);

    /// <summary>
    /// Registers <paramref name="path"/> under <paramref name="id"/>. If <paramref name="id"/>
    /// is already registered, a warning is pushed before the existing entry is overwritten.
    /// </summary>
    /// <param name="id">The id to register the asset under.</param>
    /// <param name="path">The resource path to associate with <paramref name="id"/>.</param>
    public void Register(StringName id, string path)
    {
        if (_map.TryGetValue(id, out var existing))
        {
            GD.PushWarning(
                $"Asset id '{id}' already exists.\n" +
                $"Existing: {existing}\n" +
                $"New: {path}");
        }

        _map.Add(id, path);
    }

    /// <summary>
    /// Attempts to register <paramref name="path"/> using an id automatically derived
    /// from its file name (snake_case, extension stripped).
    /// </summary>
    /// <param name="path">The resource path to register.</param>
    /// <returns><c>true</c> if the derived id was not already registered; otherwise <c>false</c>.</returns>
    public bool TryRegisterAuto(string path) => TryRegister(DeriveId(path), path);

    /// <summary>
    /// Registers <paramref name="path"/> using an id automatically derived from its
    /// file name (snake_case, extension stripped).
    /// </summary>
    /// <param name="path">The resource path to register.</param>
    public void RegisterAuto(string path) => Register(DeriveId(path), path);

    /// <summary>
    /// Scans <paramref name="folderPath"/> and registers every supported resource found,
    /// using an automatically derived id for each (see <see cref="RegisterAuto"/>).
    /// Files whose extension is in the excluded-extensions set, or that are not
    /// recognized as loadable resources, are skipped.
    /// </summary>
    /// <param name="folderPath">The directory path to scan (e.g. <c>res://assets</c>).</param>
    /// <param name="recursive">If <c>true</c>, subdirectories are scanned as well.</param>
    public void RegisterFolder(string folderPath, bool recursive = false)
    {
        if (folderPath[^1] != '/')
            folderPath += "/";
    
        var entries = ResourceLoader.ListDirectory(folderPath);
    
        foreach (var entry in entries)
        {
            if (entry.EndsWith('/'))
            {
                if (recursive)
                    RegisterFolder(folderPath + entry, recursive);
                continue;
            }
    
            var fullPath = folderPath + entry;
    
            if (IsSupportedFile(entry) && ResourceLoader.Exists(fullPath))
                RegisterAuto(fullPath);
        }
    }

    #endregion

    #region Load

    /// <summary>
    /// Loads the resource registered under <paramref name="id"/> as type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected resource type.</typeparam>
    /// <param name="id">The registered asset id.</param>
    /// <returns>The loaded resource.</returns>
    /// <exception cref="KeyNotFoundException"><paramref name="id"/> is not registered.</exception>
    /// <exception cref="InvalidDataException">
    /// The resource at the registered path could not be loaded as <typeparamref name="T"/>.
    /// </exception>
    public T Load<T>(StringName id) where T : Resource
    {
        if (!_map.TryGetValue(id, out string path))
            throw new KeyNotFoundException($"Unknown asset id '{id}'.");

        var resource = ResourceLoader.Load<T>(path);

        return resource
            ?? throw new InvalidDataException($"Asset '{id}' at '{path}' is not a valid {typeof(T).Name}.");
    }

    /// <summary>
    /// Attempts to load the resource registered under <paramref name="id"/> as type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected resource type.</typeparam>
    /// <param name="id">The registered asset id.</param>
    /// <param name="resource">
    /// The loaded resource if successful; otherwise <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if <paramref name="id"/> is registered and the resource loaded successfully
    /// as <typeparamref name="T"/>; otherwise <c>false</c>.
    /// </returns>
    public bool TryLoad<T>(StringName id, out T resource) where T : Resource
    {
        if (!_map.TryGetValue(id, out string path))
        {
            resource = null;
            return false;
        }

        resource = ResourceLoader.Load<T>(path);
        return resource != null;
    }
    
    #endregion

    #region Queries

    /// <summary>
    /// Removes the registration for <paramref name="id"/>, if present.
    /// </summary>
    /// <param name="id">The asset id to remove.</param>
    /// <returns><c>true</c> if an entry was removed; otherwise <c>false</c>.</returns>
    public bool Unregister(StringName id) => _map.Remove(id);

    /// <summary>
    /// Determines whether <paramref name="id"/> is registered.
    /// </summary>
    /// <param name="id">The asset id to check.</param>
    /// <returns><c>true</c> if registered; otherwise <c>false</c>.</returns>
    public bool Contains(StringName id) => _map.ContainsKey(id);

    /// <summary>
    /// Removes all registered entries.
    /// </summary>
    public void Clear() => _map.Clear();

    /// <summary>
    /// Gets all registered resource paths.
    /// </summary>
    public IEnumerable<string> GetPaths() => _map.Values;

    /// <summary>
    /// Gets all registered asset ids.
    /// </summary>
    public IEnumerable<StringName> GetIds() => _map.Keys;

    /// <summary>
    /// Gets a read-only view of the full id-to-path map.
    /// </summary>
    public IReadOnlyDictionary<StringName, string> GetMap() => _map.AsReadOnly();

    /// <summary>
    /// Attempts to get the resource path registered under <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The asset id to look up.</param>
    /// <param name="path">The registered path if found; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if <paramref name="id"/> is registered; otherwise <c>false</c>.</returns>
    public bool TryGetPath(StringName id, out string path)
    {
        path = GetPath(id);
        return path != null;
    }

    /// <summary>
    /// Gets the resource path registered under <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The asset id to look up.</param>
    /// <returns>The registered resource path.</returns>
    /// <exception cref="KeyNotFoundException"><paramref name="id"/> is not registered.</exception>
    public string GetPath(StringName id)
    {
        if (!_map.TryGetValue(id, out var path))
            throw new KeyNotFoundException($"Unknown asset id '{id}'.");
        return path;
    }

    #endregion
}
