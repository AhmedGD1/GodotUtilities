using Godot;

namespace Utilities.Combat;

[GlobalClass]
public partial class KnockbackComponent : Node
{
    [Signal] public delegate void KnockbackStartedEventHandler();

    private CharacterBody2D controller;

    public override void _Ready()
    {
        controller = GetOwner<CharacterBody2D>();
    }

    public void ApplyKnockback(Vector2 force)
    {
        controller.Velocity += force;
        EmitSignalKnockbackStarted();
    }
}

