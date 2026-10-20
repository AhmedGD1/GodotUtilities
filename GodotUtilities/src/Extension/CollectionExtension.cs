using Godot;

namespace GodotUtilities;

public static class CollectionExtension
{
    /// <summary>
    /// Determines whether the specified collection contains no elements.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to check.</param>
    /// <returns>
    /// <see langword="true"/> if the collection contains no elements;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="collection"/> is <see langword="null"/>.
    /// </exception>
    public static bool IsEmpty<T>(this ICollection<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        return collection.Count == 0;
    }

    /// <summary>
    /// Adds the specified item to the list if it is not already present.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list to add the item to.</param>
    /// <param name="item">The item to add.</param>
    /// <returns>
    /// <see langword="true"/> if the item was added; otherwise,
    /// <see langword="false"/> if an equal item is already present.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="list"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// Equality is determined by <see cref="List{T}.Contains(T)"/>, which uses
    /// <see cref="EqualityComparer{T}.Default"/>.
    /// This operation is O(n).
    /// </remarks>
    public static bool AddUnique<T>(this List<T> list, T item)
    {
        ArgumentNullException.ThrowIfNull(list);

        if (list.Contains(item))
            return false;

        list.Add(item);
        return true;
    }

    /// <summary>
    /// Selects a random element from the specified list.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list to select an element from.</param>
    /// <param name="rng">
    /// The random number generator to use, or <see langword="null"/>
    /// to use <see cref="MathUtil.RNG"/>.
    /// </param>
    /// <returns>A randomly selected element from the list.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="list"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The list contains no elements.
    /// </exception>
    /// <remarks>
    /// This operation does not modify the list.
    /// Selection is uniform over the list's indices, assuming a uniform
    /// random number generator.
    /// </remarks>
    public static T PickRandom<T>(this IList<T> list, RandomNumberGenerator rng = null)
    {
        ArgumentNullException.ThrowIfNull(list);

        if (list.IsEmpty())
            throw new InvalidOperationException("Cannot pick from an empty list.");

        var random = rng ?? MathUtil.RNG;
        var index = random.RandiRange(0, list.Count - 1);

        return list[index];
    }

    /// <summary>
    /// Randomly rearranges the elements of the specified list in place.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list to shuffle.</param>
    /// <param name="rng">
    /// The random number generator to use, or <see langword="null"/>
    /// to use <see cref="MathUtil.RNG"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="list"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// Uses the Fisher–Yates shuffle algorithm and runs in O(n) time.
    /// The list's element order is modified, but its size remains unchanged.
    /// </remarks>
    public static void Shuffle<T>(this IList<T> list, RandomNumberGenerator rng = null)
    {
        ArgumentNullException.ThrowIfNull(list);

        var random = rng ?? MathUtil.RNG;

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.RandiRange(0, i);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

