using Godot;
using Utilities.Logic;

namespace Utilities;

public partial class GameUtilities : Node
{
    public static GameUtilities Instance { get; private set; }

    public override void _EnterTree()
    {
        Instance = this;

        Physics2D.UpdateSpaceState(GetViewport());
    }
}
