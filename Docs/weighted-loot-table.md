[← back to README](../README.md)

# WeightedLootTable&lt;T&gt;

`GodotUtilities.Logic`

Weighted random selection: add items with integer weights, then pick one (or several) with probability proportional to weight. Good for drops, spawn tables, random events — anything where some outcomes should be rarer than others.

```csharp
using GodotUtilities.Logic;

var drops = new WeightedLootTable<string>()
    .AddItem("coin", 70)        // 70 / 100 = 70%
    .AddItem("potion", 25)      // 25%
    .AddItem("rare_sword", 5);  //  5%

string drop = drops.PickItem();

// Several picks, no repeats
List<string> three = drops.PickItems(3, allowDuplicates: false);

// Only consider items matching a condition
string weapon = drops.PickItem(item => item.EndsWith("_sword"));

// What are my odds?
float chance = drops.GetChance("rare_sword"); // 0.05
```

## Random source

By default the table draws from [`MathUtil.RNG`](math-util.md), so `MathUtil.SeedRNG` makes it deterministic. To give a table its own generator:

```csharp
var rng = new RandomNumberGenerator { Seed = 1234 };
var table = new WeightedLootTable<string>(rng);   // or: table.SetRandom(rng)
```

`SetRandom(null)` goes back to `MathUtil.RNG`.

## Building the table

- `AddItem(item, weight)` — adds an entry and returns the table for chaining. `weight` must be positive (`ArgumentOutOfRangeException` otherwise), and the total weight can't exceed `int.MaxValue` (`InvalidOperationException`). Adding the same item twice creates two entries; their weights add up when computing odds.
- `RemoveItem(item)` — removes **every** entry equal to `item`. Returns whether any were removed.
- `SetItemWeight(item, weight)` — sets the weight of every entry equal to `item` (each entry gets `weight`, so with duplicate entries the item's combined weight is a multiple of it). Returns `false` if the item isn't in the table.
- `ModifyItemWeight(item, change)` — adds `change` (may be negative) to the weight of every entry equal to `item`, never going below `1`. Returns `false` if the item isn't in the table.
- `Clear()` — removes everything.

Items are compared with `EqualityComparer<T>.Default`.

## Picking

- `PickItem()` — returns one item, or `default` if the table is empty.
- `PickItem(condition)` — like `PickItem()`, but only entries whose item satisfies `condition` are eligible; returns `default` if none do.
- `TryPickItem(out item)` / `TryPickItem(condition, out item)` — the same, returning `false` instead of `default` so you can tell "nothing to pick" apart from a legitimate `default` item. A `null` condition is treated as no condition.
- `PickItems(count, allowDuplicates = true)` / `PickItems(count, condition, allowDuplicates = true)` — returns a `List<T>` of picks.
  - With `allowDuplicates: true`, each pick is independent, so the same item can appear several times.
  - With `allowDuplicates: false`, picks are made without replacement and `count` is capped at the number of eligible entries (so the list can be shorter than `count`). Removal is by entry, so an item added twice can still come up twice.
  - Returns an empty list if `count <= 0`, the table is empty, or nothing satisfies the condition.

## Inspecting

- `IsEmpty`, `ItemCount` (number of entries), `TotalWeight`.
- `Items` — read-only list of `LootItem(Item, Weight)` entries (a live view); `GetAllItems()` enumerates the same entries.
- `GetItems(condition)` — enumerates the items (not entries) that satisfy `condition`.
- `Contains(item)`.
- `GetItemWeight(item)` — combined weight of all entries equal to `item`, or `0` if absent.
- `GetChance(item)` — the probability, `0..1`, that a single `PickItem()` returns `item`.

[← back to README](../README.md)