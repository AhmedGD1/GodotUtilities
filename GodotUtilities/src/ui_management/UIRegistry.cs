using Godot.Collections;
using Godot;

namespace Utilities.UIManagement;

[GlobalClass]
public partial class UIRegistry : Resource
{
    [Export] public string[] goBackActions                 = [];
    [Export] public Dictionary<string, PackedScene> panels = [];
}