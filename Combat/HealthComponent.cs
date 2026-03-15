using System.Collections.Generic;
using Godot;

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
    [Signal] public delegate void DamagedEventHandler(Node2D source, float amount);
    [Signal] public delegate void HealedEventHandler(float amount);
    [Signal] public delegate void DiedEventHandler(Node2D source);
    [Signal] public delegate void RevivedEventHandler();

    [Export(PropertyHint.Range, "1, 1000")] private float maxHealth = 3f;
    [Export(PropertyHint.Range, "0, 0.99")] private float defense;
    [Export(PropertyHint.Range, "0, 5")] private float invincibilityTime;

    [Export] private bool destroyOnDeath;

    private readonly Dictionary<DamageType, float> resistances = new();
    private readonly HashSet<DamageType> immunity              = new();

    private float currentHealth;
    private float minHealth;

    private Cooldown invincibilityTimer;

    public override void _Ready()
    {
        SetHealth(maxHealth);

        invincibilityTimer = new Cooldown(invincibilityTime);
    }

    public override void _Process(double delta)
    {
        invincibilityTimer.Tick(delta);
    }

    public bool TakeDamage(DamageData data)
    {
        if (IsDead || IsInvincible || immunity.Contains(data.DamageType))
            return false;

        float finalDamage = CalculateDamage(data.DamageType, data.Damage);

        SetHealth(currentHealth - finalDamage);

        if (invincibilityTime > 0f)
            MakeInvincible();

        EmitSignalDamaged(data.Source, finalDamage);

        if (IsDead)
        {
            EmitSignalDied(data.Source);
            
            if (destroyOnDeath)
                Owner.QueueFree();
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
        EmitSignalHealthChanged(oldHealth, currentHealth);
    }

    public void SetMaxHealth(float value, bool healToMax = false)
    {
        maxHealth = Mathf.Max(minHealth + 1f, value);

        if (healToMax)
            SetHealth(maxHealth);
    }

    public void SetMinHealth(float value)
    {
        minHealth = Mathf.Min(value, maxHealth);
    }

    public void Heal(float amount)
    {
        if (amount <= 0f)
            return;
        SetHealth(currentHealth + amount);
        EmitSignalHealed(amount);
    }

    public void Kill() => Kill(null);

    public void Kill(Node2D source)
    {
        if (IsDead) 
            return;
        SetHealth(minHealth);
        EmitSignalDied(source);
    }

    public void Revive() => Revive(maxHealth);

    public void Revive(float amount)
    {
        if (IsAlive) 
            return;
        SetHealth(amount);
        invincibilityTimer.Reset();
        EmitSignalRevived();
    }

    #endregion

    #region Queries

    public bool IsAlive      => currentHealth > minHealth;
    public bool IsDead       => currentHealth <= minHealth;
    public bool IsInvincible => !invincibilityTimer.IsReady;

    public float Progress      => MathUtil.Progress(currentHealth, maxHealth);
    public float CurrentHealth => currentHealth;
    public float MaxHealth     => maxHealth;
    public float MinHealth     => minHealth;

    #endregion
}

public struct DamageData
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
