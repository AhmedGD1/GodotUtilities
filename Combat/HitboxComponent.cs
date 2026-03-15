using System;
using System.Collections.Generic;
using Godot;

namespace Utilities.Combat;

[GlobalClass]
public partial class HitboxComponent : Area2D
{
    [Signal] public delegate void HurtboxDetectedEventHandler(HurtboxComponent hurtbox);

    [Export] public HealthComponent.DamageType damageType = HealthComponent.DamageType.Physical;

    [Export(PropertyHint.Range, "1, 1000")] private float damage = 1f;
    [Export(PropertyHint.Range, "0, 1000")] private float knockbackForce;

    private readonly HashSet<CollisionShape2D> collisions = new();

    public bool Enabled { get; private set; }

    public override void _Ready()
    {   
        UpdateCollisions();
        AreaEntered += OnAreaEntered;
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
        Enabled = true;
    }  

    public void Disable()
    {
        foreach (var collision in collisions) 
            collision.SetDeferred("disabled", true);
        Enabled = false;
    }  

    public void MultiplyKnockback(float multiplier)
    {
        knockbackForce *= multiplier;
    }

    private void OnAreaEntered(Area2D area)
    {
        if (area is HurtboxComponent hurtbox)
        {
            Vector2 kbDirection = GlobalPosition.DirectionTo(hurtbox.GlobalPosition);
            var data = new DamageData(this, damage, damageType, knockbackForce * kbDirection);

            hurtbox.ReceiveDamage(data);
            EmitSignalHurtboxDetected(hurtbox);
        }
    }

}
