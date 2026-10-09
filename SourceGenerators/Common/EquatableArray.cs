using System;
using System.Collections;
using System.Collections.Generic;

namespace GodotUtilities.SourceGenerators;

internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
{
    private readonly T[]? _array;

    public EquatableArray(T[] array) => _array = array;

    public EquatableArray(IEnumerable<T> items) => _array = System.Linq.Enumerable.ToArray(items);

    private T[] Items => _array ?? Array.Empty<T>();

    public int Count => Items.Length;

    public T this[int index] => Items[index];

    public bool Equals(EquatableArray<T> other)
    {
        var a = Items;
        var b = other.Items;

        if (a.Length != b.Length)
            return false;

        var comparer = EqualityComparer<T>.Default;
        for (var i = 0; i < a.Length; i++)
        {
            if (!comparer.Equals(a[i], b[i]))
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            foreach (var item in Items)
                hash = hash * 31 + (item is null ? 0 : item.GetHashCode());
            return hash;
        }
    }

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
