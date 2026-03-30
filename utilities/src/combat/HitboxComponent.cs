using Godot;

namespace Utilities.Combat;

[GlobalClass]
public partial class HitboxComponent : Area2D
{
    [Signal] public delegate void HurtboxDetectedEventHandler(HurtboxComponent hurtbox);

    [Export] public HealthComponent.DamageType damageType = HealthComponent.DamageType.Physical;

    [Export(PropertyHint.Range, "1, 1000")] public float damage = 1f;
    [Export(PropertyHint.Range, "0, 1000")] public float knockbackForce;

    private CollisionShape2D collision;

    public bool Enabled { get; private set; }

    public override void _Ready()
    { 
        AreaEntered += OnAreaEntered;
        collision    = this.GetChildOfType<CollisionShape2D>();
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
