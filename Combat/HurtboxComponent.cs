using System.Collections.Generic;
using Godot;

namespace Utilities.Combat;

[GlobalClass]
public partial class HurtboxComponent : Area2D
{
    [Export] private HealthComponent healthComponent;
    [Export] private KnockbackComponent knockbackComponent;

    private readonly HashSet<CollisionShape2D> collisions = new();

    public override void _Ready()
    {   
        UpdateCollisions();
    }

    public void ReceiveDamage(DamageData data)
    {
        if (healthComponent.TakeDamage(data))
            knockbackComponent?.ApplyKnockback(data.Knockback);
    }

    public void UpdateCollisions()
    {
        collisions.Clear();

        foreach (var child in GetChildren())
        {
            if (child is CollisionShape2D collision)
                collisions.Add(collision);
        }
    }

    public void Enable()
    {
        foreach (var collision in collisions) 
            collision.SetDeferred("disabled", false);
    }  

    public void Disable()
    {
        foreach (var collision in collisions) 
            collision.SetDeferred("disabled", true);
    }  
}
