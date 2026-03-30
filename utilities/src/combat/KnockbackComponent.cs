using Godot;

namespace Utilities.Combat;

[GlobalClass]
public partial class KnockbackComponent : Node
{
    [Signal] public delegate void KnockbackStartedEventHandler();
    [Signal] public delegate void KnockbackEndedEventHandler();

    private const float DEFAULT_DURATION = 0.25f;

    private CharacterBody2D controller;
    private float timer;

    public override void _Ready() => controller = GetOwner<CharacterBody2D>();

    public override void _Process(double delta)
    {
        if (timer <= 0f) return;
        
        timer -= (float)delta;

        if (timer <= 0f)
            EmitSignal(SignalName.KnockbackEnded);
    }

    public void ApplyKnockback(Vector2 force) => ApplyKnockback(force, DEFAULT_DURATION);

    public void ApplyKnockback(Vector2 force, float duration)
    {
        controller.Velocity += force;
        EmitSignal(SignalName.KnockbackStarted);

        timer = duration;
    }
}

