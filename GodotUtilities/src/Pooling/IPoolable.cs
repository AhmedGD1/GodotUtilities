namespace GodotUtilities.Pooling;

/// <summary>
/// Implemented by types that need to react to being taken from or returned to
/// an <see cref="ObjectPool{T}"/> or <see cref="NodePool{T}"/>, e.g. to reset
/// internal state on reuse.
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// Called when this instance is retrieved from its pool, before it's handed
    /// back to the caller.
    /// </summary>
    void OnGet();

    /// <summary>
    /// Called when this instance is returned to its pool, before it's stored
    /// for reuse.
    /// </summary>
    void OnRelease();
}
