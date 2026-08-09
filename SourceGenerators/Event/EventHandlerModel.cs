using System;
using System.Collections.Generic;

namespace GodotUtilities.SourceGenerators;

internal readonly record struct EventHandlerModel(
    string MethodName,
    string EventTypeFullName,
    bool TakesParameter);

internal sealed record WireableClassModel
{
    public required string ClassName { get; init; }
    public required string Namespace { get; init; }
    public required bool IsPartial { get; init; }
    public required bool DerivesFromNode { get; init; }
    public required bool IsNested { get; init; }
    public required string FilePathHint { get; init; }
    public required EquatableArray<EventHandlerModel> Handlers { get; init; }
}

internal readonly struct EquatableArray<T>(IReadOnlyList<T> items) : IEquatable<EquatableArray<T>>
    where T : IEquatable<T>
{
    private readonly IReadOnlyList<T> _items = items;

    public IReadOnlyList<T> Items => _items ?? [];

    public bool Equals(EquatableArray<T> other)
    {
        var a = Items;
        var b = other.Items;
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (!a[i].Equals(b[i])) return false;
        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = 17;
        foreach (var item in Items)
            hash = hash * 31 + item.GetHashCode();
        return hash;
    }
}
