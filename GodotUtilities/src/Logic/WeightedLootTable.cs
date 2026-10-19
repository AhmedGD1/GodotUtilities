using Godot;

namespace GodotUtilities.Logic;

/// <summary>
/// A table of items with integer weights. Items with a higher weight are picked more often.
/// </summary>
/// <typeparam name="T">The type of the items in the table.</typeparam>
/// <param name="rng">
/// Optional random number generator used for picks. If <c>null</c>, <see cref="MathUtil.RNG"/> is used.
/// </param>
public sealed class WeightedLootTable<T>(RandomNumberGenerator rng = null)
{
    /// <summary>
    /// A table entry: an item and its weight.
    /// </summary>
    /// <param name="Item">The item that can be picked.</param>
    /// <param name="Weight">The relative chance of the item being picked. Always positive.</param>
    public readonly record struct LootItem(T Item, int Weight);

    private RandomNumberGenerator _rng = rng;

    private readonly List<LootItem> _entries = [];

    private int _totalWeight;

    private RandomNumberGenerator Rng => _rng ?? MathUtil.RNG;

    /// <summary>Whether the table has no entries.</summary>
    public bool IsEmpty => _entries.Count == 0;

    /// <summary>The sum of the weights of all entries.</summary>
    public int TotalWeight => _totalWeight;

    /// <summary>The number of entries in the table.</summary>
    public int ItemCount => _entries.Count;

    /// <summary>All entries in the table, in the order they were added.</summary>
    public IReadOnlyList<LootItem> Items => _entries;

    #region Configuration

    /// <summary>Uses <paramref name="rng"/> for picks. Pass <c>null</c> to fall back to <see cref="MathUtil.RNG"/>.</summary>
    public void SetRandom(RandomNumberGenerator rng) => _rng = rng;

    /// <summary>Adds an entry. Returns the table for chaining.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="weight"/> is not positive.</exception>
    /// <exception cref="InvalidOperationException">The total weight would exceed <see cref="int.MaxValue"/>.</exception>
    public WeightedLootTable<T> AddItem(T item, int weight)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(weight, 0);

        if ((long)_totalWeight + weight > int.MaxValue)
            throw new InvalidOperationException("Total weight would exceed int.MaxValue.");

        _entries.Add(new LootItem(item, weight));
        _totalWeight += weight;

        return this;
    }

    /// <summary>Removes every entry equal to <paramref name="item"/>. Returns whether any were removed.</summary>
    public bool RemoveItem(T item)
    {
        var comparer = EqualityComparer<T>.Default;
        int removed = _entries.RemoveAll(e => comparer.Equals(e.Item, item));

        if (removed == 0)
            return false;

        RecalculateTotal();
        return true;
    }

    /// <summary>Removes all entries from the table.</summary>
    public void Clear()
    {
        _entries.Clear();
        _totalWeight = 0;
    }

    /// <summary>
    /// Sets the weight of every entry equal to <paramref name="item"/>.
    /// Returns <c>false</c> if the item isn't in the table.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="weight"/> is not positive.</exception>
    public bool SetItemWeight(T item, int weight)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(weight, 0);
        return Reweight(item, _ => weight);
    }

    /// <summary>
    /// Adds <paramref name="change"/> (may be negative) to the weight of every entry equal to
    /// <paramref name="item"/>, never going below 1. Returns <c>false</c> if the item isn't in the table.
    /// </summary>
    public bool ModifyItemWeight(T item, int change) => Reweight(item, w => (int)Math.Clamp((long)w + change, 1, int.MaxValue));

    #endregion

    #region Queries

    /// <summary>True if at least one entry equals <paramref name="item"/>.</summary>
    public bool Contains(T item)
    {
        var comparer = EqualityComparer<T>.Default;

        foreach (var entry in _entries)
            if (comparer.Equals(entry.Item, item))
                return true;

        return false;
    }

    /// <summary>Combined weight of all entries equal to <paramref name="item"/>, or 0 if absent.</summary>
    public int GetItemWeight(T item)
    {
        var comparer = EqualityComparer<T>.Default;
        long sum = 0;

        foreach (var entry in _entries)
            if (comparer.Equals(entry.Item, item))
                sum += entry.Weight;

        return (int)sum;
    }

    /// <summary>Enumerates all entries.</summary>
    public IEnumerable<LootItem> GetAllItems() => _entries;

    /// <summary>Enumerates the items (not entries) that satisfy <paramref name="condition"/>.</summary>
    public IEnumerable<T> GetItems(Func<T, bool> condition)
    {
        foreach (var entry in _entries)
            if (condition(entry.Item))
                yield return entry.Item;
    }

    /// <summary>
    /// The chance, from 0 to 1, that a single <see cref="PickItem()"/> returns <paramref name="item"/>.
    /// </summary>
    public float GetChance(T item) => _totalWeight == 0 ? 0f : (float)GetItemWeight(item) / _totalWeight;

    #endregion

    #region Picking

    /// <summary>Picks a random item, respecting weights.</summary>
    /// <returns>The picked item, or <c>default</c> if the table is empty.</returns>
    public T PickItem() => TryPickItem(out var item) ? item : default;

    /// <summary>Picks a random item among those that satisfy <paramref name="condition"/>, respecting weights.</summary>
    /// <param name="condition">Filter for the items that can be picked. If <c>null</c>, all items are eligible.</param>
    /// <returns>The picked item, or <c>default</c> if no item is eligible.</returns>
    public T PickItem(Func<T, bool> condition) => TryPickItem(condition, out var item) ? item : default;

    /// <summary>Tries to pick a random item, respecting weights.</summary>
    /// <param name="item">The picked item, or <c>default</c> if the table is empty.</param>
    /// <returns><c>true</c> if an item was picked; otherwise <c>false</c>.</returns>
    public bool TryPickItem(out T item)
    {
        if (_entries.Count == 0)
        {
            item = default;
            return false;
        }

        item = _entries[Roll(_entries, _totalWeight, Rng)].Item;
        return true;
    }

    /// <summary>Tries to pick a random item among those that satisfy <paramref name="condition"/>, respecting weights.</summary>
    /// <param name="condition">Filter for the items that can be picked. If <c>null</c>, all items are eligible.</param>
    /// <param name="item">The picked item, or <c>default</c> if no item is eligible.</param>
    /// <returns><c>true</c> if an item was picked; otherwise <c>false</c>.</returns>
    public bool TryPickItem(Func<T, bool> condition, out T item)
    {
        if (condition is null)
            return TryPickItem(out item);

        var rng = Rng;
        int running = 0;
        int chosen = -1;

        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];

            if (!condition(entry.Item))
                continue;

            running += entry.Weight;

            if (rng.RandiRange(0, running - 1) < entry.Weight)
                chosen = i;
        }

        if (chosen < 0)
        {
            item = default;
            return false;
        }

        item = _entries[chosen].Item;
        return true;
    }

    /// <summary>Picks several random items, respecting weights.</summary>
    /// <param name="count">How many items to pick.</param>
    /// <param name="allowDuplicates">
    /// If <c>false</c>, each entry can be picked at most once, so fewer than <paramref name="count"/> items may be returned.
    /// </param>
    /// <returns>The picked items. Empty if <paramref name="count"/> is not positive or the table is empty.</returns>
    public List<T> PickItems(int count, bool allowDuplicates = true) =>
        PickItems(count, null, allowDuplicates);

    /// <summary>Picks several random items among those that satisfy <paramref name="condition"/>, respecting weights.</summary>
    /// <param name="count">How many items to pick.</param>
    /// <param name="condition">Filter for the items that can be picked. If <c>null</c>, all items are eligible.</param>
    /// <param name="allowDuplicates">
    /// If <c>false</c>, each entry can be picked at most once, so fewer than <paramref name="count"/> items may be returned.
    /// </param>
    /// <returns>The picked items. Empty if <paramref name="count"/> is not positive or no item is eligible.</returns>
    public List<T> PickItems(int count, Func<T, bool> condition, bool allowDuplicates = true)
    {
        var result = new List<T>();

        if (count <= 0 || _entries.Count == 0)
            return result;

        List<LootItem> source = _entries;
        int total = _totalWeight;

        if (condition is not null || !allowDuplicates)
        {
            source = new List<LootItem>(_entries.Count);
            total = 0;

            foreach (var entry in _entries)
            {
                if (condition is not null && !condition(entry.Item))
                    continue;

                source.Add(entry);
                total += entry.Weight;
            }

            if (source.Count == 0)
                return result;
        }

        var rng = Rng;

        if (allowDuplicates)
        {
            for (int i = 0; i < count; i++)
                result.Add(source[Roll(source, total, rng)].Item);

            return result;
        }

        count = Math.Min(count, source.Count);

        for (int i = 0; i < count; i++)
        {
            int index = Roll(source, total, rng);
            var picked = source[index];

            result.Add(picked.Item);
            total -= picked.Weight;

            int last = source.Count - 1;
            source[index] = source[last];
            source.RemoveAt(last);
        }

        return result;
    }

    #endregion

    #region Internals

    private static int Roll(List<LootItem> source, int totalWeight, RandomNumberGenerator rng)
    {
        int roll = rng.RandiRange(0, totalWeight - 1);

        for (int i = 0; i < source.Count; i++)
        {
            roll -= source[i].Weight;

            if (roll < 0)
                return i;
        }

        return source.Count - 1;
    }

    private bool Reweight(T item, Func<int, int> computeWeight)
    {
        var comparer = EqualityComparer<T>.Default;
        long newTotal = _totalWeight;
        bool found = false;

        for (int i = 0; i < _entries.Count; i++)
        {
            if (!comparer.Equals(_entries[i].Item, item))
                continue;

            found = true;
            newTotal += computeWeight(_entries[i].Weight) - _entries[i].Weight;
        }

        if (!found)
            return false;

        if (newTotal > int.MaxValue)
            throw new InvalidOperationException("Total weight would exceed int.MaxValue.");

        for (int i = 0; i < _entries.Count; i++)
        {
            if (comparer.Equals(_entries[i].Item, item))
                _entries[i] = _entries[i] with { Weight = computeWeight(_entries[i].Weight) };
        }

        _totalWeight = (int)newTotal;
        return true;
    }

    private void RecalculateTotal()
    {
        int total = 0;

        foreach (var entry in _entries)
            total += entry.Weight;

        _totalWeight = total;
    }

    #endregion
}
