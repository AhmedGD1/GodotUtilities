using System;
using Godot;

namespace Utilities.Combat;

[GlobalClass]
public partial class HurtboxComponent : Area2D
{
    [Export] private HealthComponent healthComponent;
    [Export] private KnockbackComponent knockbackComponent;

    private CollisionShape2D collision;

    public bool Enabled { get; private set; }

    public override void _Ready()
    {   
        if (healthComponent is null)
            throw new Exception("[HurtboxComponent] health component is not assigned in the inspector");

        collision = this.GetChildOfType<CollisionShape2D>();
    }

    public void ReceiveDamage(DamageData data)
    {
        if (healthComponent.TakeDamage(data))
            knockbackComponent?.ApplyKnockback(data.Knockback);
    }

    public void Enable()
    {
        collision.SetDeferred("disabled", false);
        Enabled = true;
    }  

    public void Disable()
    {
        collision.SetDeferred("disabled", true);
        Enabled = false;
    }  
}
