using Godot.Collections;
using Godot;

namespace Utilities.UIManagement;

[GlobalClass]
public partial class UIRegistry : Resource
{
    [Export] public string[] goBackActions                 = ["ui_cancel"];
    [Export] public Dictionary<string, PackedScene> panels = [];
}