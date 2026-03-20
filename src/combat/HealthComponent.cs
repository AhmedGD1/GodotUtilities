using Godot.Collections;
using Godot;
using Utilities.Events;

namespace Utilities.Combat;

[GlobalClass]
public partial class HealthComponent : Node
{
    public enum DamageType
    {
        Physical,
        Ranged,
        Poison,
    }

    [Signal] public delegate void HealthChangedEventHandler(float previous, float current);
    [Signal] public delegate void MaxHealthChangedEventHandler(float previous, float current);
    [Signal] public delegate void DamagedEventHandler(Node2D source, float amount);
    [Signal] public delegate void HealedEventHandler(float amount);
    [Signal] public delegate void DiedEventHandler(Node2D source);
    [Signal] public delegate void RevivedEventHandler();
    [Signal] public delegate void DamagePreventedEventHandler();

    [Export] private bool destroyOnDeath;

    [Export(PropertyHint.Range, "1, 1000")] private float maxHealth = 3f;
    [Export(PropertyHint.Range, "0, 0.99")] private float defense;
    [Export(PropertyHint.Range, "0, 5")]    private float invincibilityTime;

    [Export] private Dictionary<DamageType, float> resistances = new();
    [Export] private Array<DamageType> immunity                = new();

    private float currentHealth;
    private float minHealth;

    private Cooldown invincibilityTimer;

    public override void _EnterTree() => invincibilityTimer = new Cooldown(invincibilityTime);

    public override void _Ready()
    {
        currentHealth = maxHealth;
    }

    public override void _Process(double delta)
    {
        invincibilityTimer.Tick(delta);
    }

    public bool TakeDamage(DamageData data)
    {
        if (IsDead())
            return false;
        
        if (IsInvincible() || immunity.Contains(data.DamageType))
        {
            EmitSignal(SignalName.DamagePrevented);
            return false;
        }

        float finalDamage = CalculateDamage(data.DamageType, data.Damage);

        SetHealth(currentHealth - finalDamage);

        if (invincibilityTime > 0f)
            MakeInvincible();

        EmitSignal(SignalName.Damaged, data.Source, finalDamage);

        if (IsDead())
        {
            EmitSignal(SignalName.Died, data.Source);
            
            if (destroyOnDeath) Owner.QueueFree();
        }
        return true;
    }

    private float CalculateDamage(DamageType type, float damage)
    {
        float result = damage;
        result *= 1f - defense;

        if (resistances.TryGetValue(type, out float resistance))
            result *= resistance;
        return result;
    }

    public void MakeInvincible()               => invincibilityTimer.Start();
    public void MakeInvincible(float duration) => invincibilityTimer.Start(duration);

    public void AddImmunity(DamageType type)    => immunity.Add(type);
    public void RemoveImmunity(DamageType type) => immunity.Remove(type);

    public void SetResistance(DamageType type, float resistance)
    {
        resistances[type] = resistance;
    }

    public bool RemoveResistance(DamageType type)
    {
        return resistances.Remove(type);
    }

    #region Health Manipulation

    private void SetHealth(float value)
    {
        float oldHealth = currentHealth;
        currentHealth = Mathf.Clamp(value, minHealth, maxHealth);

        if (oldHealth != currentHealth)
            EmitSignal(SignalName.HealthChanged, oldHealth, currentHealth);
    }

    public void SetMaxHealth(float value, bool healToMax = false)
    {
        float oldValue = maxHealth;
        maxHealth = Mathf.Max(minHealth + 0.01f, value);

        if (oldValue != maxHealth) EmitSignal(SignalName.MaxHealthChanged, oldValue, maxHealth);
        if (healToMax) SetHealth(maxHealth);
    }

    public void SetMinHealth(float value)
    {
        minHealth = Mathf.Min(value, maxHealth);
    }

    public void Heal(float amount)
    {
        if (amount <= 0f)
            return;

        float oldValue = currentHealth;
        SetHealth(currentHealth + amount);
        EmitSignal(SignalName.Healed, currentHealth - oldValue);
    }

    public void Kill() => Kill(null);

    public void Kill(Node2D source)
    {
        if (IsDead()) 
            return;
        SetHealth(minHealth);
        EmitSignal(SignalName.Died, source);

        if (destroyOnDeath) Owner.QueueFree();
    }

    public void Revive() => Revive(maxHealth);

    public void Revive(float amount)
    {
        if (IsAlive()) 
            return;
        SetHealth(Mathf.Max(minHealth + 0.01f, amount));
        invincibilityTimer.Stop();
        EmitSignal(SignalName.Revived);
    }

    #endregion

    #region Queries

    public bool IsAlive()      => currentHealth > minHealth;
    public bool IsDead()       => currentHealth <= minHealth;
    public bool IsInvincible() => !invincibilityTimer.IsReady;

    public float Progress      => MathUtil.Progress(currentHealth, maxHealth);
    public float CurrentHealth => currentHealth;
    public float MaxHealth     => maxHealth;
    public float MinHealth     => minHealth;

    #endregion
}

public readonly struct DamageData
{
    public HealthComponent.DamageType DamageType { get; }

    public Node2D Source     { get; }
    public Vector2 Knockback { get; }
    public float Damage      { get; }

    public DamageData(Node2D source, float damage, HealthComponent.DamageType type, Vector2 knockback)
    {
        Source     = source;
        Damage     = damage;
        DamageType = type;
        Knockback  = knockback;
    }
}
