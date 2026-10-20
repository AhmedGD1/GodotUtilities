using Godot;

namespace GodotUtilities;

public static class ResourcePreloaderExtension
{
    /// <summary>
    /// Instantiates a <see cref="PackedScene"/> stored in the preloader under the given name
    /// and returns it as <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected root node type of the instantiated scene.</typeparam>
    /// <param name="resourcePreloader">The preloader to fetch the scene from.</param>
    /// <param name="name">The name the scene is registered under in the preloader.</param>
    /// <returns>
    /// The instantiated scene root as <typeparamref name="T"/>, or <see langword="null"/> if the
    /// resource does not exist, is not a <see cref="PackedScene"/>, or its root cannot be cast to
    /// <typeparamref name="T"/>. An error is pushed via <see cref="GD.PushError(string)"/> for the
    /// first two cases.
    /// </returns>
    public static T InstantiateSceneOrNull<T>(this ResourcePreloader resourcePreloader, StringName name) where T : Node
    {
        if (!resourcePreloader.HasResource(name))
        {
            GD.PushError($"resource with name '{name}' does not exist.");
            return null;
        }

        if (resourcePreloader.GetResource(name) is not PackedScene packedScene)
        {
            GD.PushError($"resource with name '{name}' is not a {nameof(PackedScene)}");
            return null;
        }

        return packedScene.InstantiateOrNull<T>();
    }

    /// <summary>
    /// Instantiates a <see cref="PackedScene"/> from the preloader by resolving its name from
    /// <typeparamref name="T"/>, and returns it as <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// The lookup first tries the type name as-is (e.g. <c>PlayerController</c>), then falls back
    /// to its snake_case form (e.g. <c>player_controller</c>).
    /// </remarks>
    /// <typeparam name="T">
    /// The expected root node type of the instantiated scene. Its name is also used as the
    /// resource key.
    /// </typeparam>
    /// <param name="resourcePreloader">The preloader to fetch the scene from.</param>
    /// <returns>
    /// The instantiated scene root as <typeparamref name="T"/>, or <see langword="null"/> if no
    /// matching resource exists, it is not a <see cref="PackedScene"/>, or its root cannot be cast
    /// to <typeparamref name="T"/>. An error is pushed via <see cref="GD.PushError(string)"/> for
    /// the first two cases.
    /// </returns>
    public static T InstantiateSceneOrNull<T>(this ResourcePreloader resourcePreloader) where T : Node
    {
        string name = typeof(T).Name;

        if (!resourcePreloader.HasResource(name))
            name = typeof(T).Name.ToSnakeCase();

        if (!resourcePreloader.HasResource(name))
        {
            GD.PushError($"resource with type '{typeof(T).Name}' does not exist.");
            return null;
        }

        if (resourcePreloader.GetResource(name) is not PackedScene packedScene)
        {
            GD.PushError($"resource with name '{name}' is not a {nameof(PackedScene)}");
            return null;
        }

        return packedScene.InstantiateOrNull<T>();
    }
}

