namespace GodotUtilities;

/// <summary>How a <see cref="SingletonAttribute"/> class gets its instance.</summary>
public enum SingletonMode
{
    /// <summary>
    /// The node is placed in a scene by hand. It registers itself when it enters the tree,
    /// and clears the instance when it leaves.
    /// </summary>
    Scene = 0,

    /// <summary>
    /// The node is registered in Project Settings &gt; Globals &gt; Autoload. <c>Instance</c> looks it up
    /// under the scene tree root by name.
    /// </summary>
    Autoload = 1,

    /// <summary>
    /// Nothing registers it: <c>Instance</c> creates it on first access. Nodes are added to the
    /// scene tree root (deferred), the same place an autoload lives.
    /// </summary>
    Lazy = 2,
}

/// <summary>
/// Generates a static <c>Instance</c> (plus <c>HasInstance</c> / <c>TryGetInstance</c>) on a partial class,
/// so you don't write the singleton boilerplate yourself. The class must be <c>partial</c>.
/// </summary>
/// <param name="mode">How the instance is obtained. Defaults to <see cref="SingletonMode.Scene"/>.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class SingletonAttribute(SingletonMode mode = SingletonMode.Scene) : Attribute
{
    /// <summary>How the instance is obtained.</summary>
    public SingletonMode Mode { get; } = mode;

    /// <summary>
    /// The node name. For <see cref="SingletonMode.Autoload"/> it is the name used to look the autoload up
    /// (must match the name in Project Settings); for a <see cref="SingletonMode.Lazy"/> node it is the name given
    /// to the created node. Defaults to the class name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// <see cref="SingletonMode.Lazy"/> nodes only: instantiate this scene (e.g. "res://managers/audio.tscn")
    /// instead of <c>new</c>, for singletons that need child nodes. The scene's root must be this class.
    /// </summary>
    public string ScenePath { get; set; }
}
