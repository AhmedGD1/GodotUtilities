using System.Linq;
using System;
using Godot;

namespace Utilities.Combat;

[GlobalClass]
public partial class HurtboxComponent : Area2D
{
    [Export] private HealthComponent healthComponent;
    [Export] private KnockbackComponent knockbackComponent;

    private CollisionShape2D[] collisions;

    public bool Enabled { get; private set; }

    public override void _Ready()
    {   
        if (healthComponent is null)
            throw new Exception("[HurtboxComponent] health component is not assigned in the inspector");

        InitializeCollisions();
    }

    public void ReceiveDamage(DamageData data)
    {
        if (healthComponent.TakeDamage(data))
            knockbackComponent?.ApplyKnockback(data.Knockback);
    }

    public void InitializeCollisions()
    {
        collisions = (CollisionShape2D[])GetChildren().ToArray();
    }

    public void Enable()
    {
        foreach (var collision in collisions) 
            collision.SetDeferred("disabled", false);
        Enabled = true;
    }  

    public void Disable()
    {
        foreach (var collision in collisions) 
            collision.SetDeferred("disabled", true);
        Enabled = false;
    }  
}
