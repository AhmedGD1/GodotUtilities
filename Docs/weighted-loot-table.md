[← back to README](../README.md)

# WeightedLootTable&lt;T&gt;

`GodotUtilities.Logic`

A weighted random-selection table — register items with an integer weight, then draw one or many at random, with the probability of each item proportional to its weight.

```csharp
var table = new WeightedLootTable<string>();
table.AddItem("Common Sword", weight: 70);
table.AddItem("Rare Sword", weight: 25);
table.AddItem("Legendary Sword", weight: 5);

string drop = table.PickItem(); // weighted random pick

// Pick 3 distinct items without repeats:
List<string> loot = table.PickItems(3, allowDuplicates: false);

// Only pick among items matching a condition:
string commonOnly = table.PickItem(item => item.StartsWith("Common"));
```

Other members: `RemoveItem`, `Clear`, `Contains`, `GetItemWeight`, `SetItemWeight`, `ModifyItemWeight` (adjust a weight up/down, e.g. for pity systems), `GetAllItems()` / `GetItems(condition)`, and `TotalWeight` / `ItemCount` / `IsEmpty` for introspection. You can supply your own `RandomNumberGenerator` in the constructor (or via `SetRandom`) for seeded/deterministic drops; otherwise it shares `MathUtil.RNG`.

> `PickItem()` returns `default` (`null` for reference types) on an empty table rather than throwing — check `IsEmpty` first if that distinction matters for your call site.

[← back to README](../README.md)
