using System;
using System.Collections.Generic;
using UnityEngine;

public enum StatType
{
    PhysicalPower, MagicPower, DetectionRange, MaxHealth,
    AttackSpeed, CriticalChance, CriticalDamage
}
public class TowerStats : MonoBehaviour
{
    [SerializeField] private int cost = 10;
    [SerializeField] private float physicalPower = 10f;
    [SerializeField] private float magicPower = 10f;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float maxHealth = 10f;
    [SerializeField] private float attackSpeed = 10f;
    [SerializeField] private float criticalChance = 10f;   // percent, can exceed 100
    [SerializeField] private float criticalDamage = 2f;    // multiplier, 2 = double damage

    private struct Modifier
    {
        public object Source;
        public StatType Stat;
        public float Amount;
    }

    private readonly List<Modifier> modifiers = new List<Modifier>();

    public event Action StatsChanged;

    public int Cost { get => cost; set => cost = value; }
    public float PhysicalPower => Get(StatType.PhysicalPower);
    public float MagicPower => Get(StatType.MagicPower);
    public float DetectionRange => Get(StatType.DetectionRange);
    public float MaxHealth => Get(StatType.MaxHealth);
    public float AttackSpeed => Get(StatType.AttackSpeed);
    public float CriticalChance => Get(StatType.CriticalChance);
    public float CriticalDamage => Get(StatType.CriticalDamage);

    public float Get(StatType type)
    {
        float value = GetBase(type);
        foreach (Modifier modifier in modifiers)
        {
            if (modifier.Stat == type) value += modifier.Amount;
        }
        return value;
    }

    private float GetBase(StatType type)
    {
        switch (type)
        {
            case StatType.PhysicalPower: return physicalPower;
            case StatType.MagicPower: return magicPower;
            case StatType.DetectionRange: return detectionRange;
            case StatType.MaxHealth: return maxHealth;
            case StatType.AttackSpeed: return attackSpeed;
            case StatType.CriticalChance: return criticalChance;
            default: return criticalDamage;
        }
    }

    // "source" is whatever added it (an item), so it can be removed on swap.
    public void AddModifier(object source, StatType stat, float amount)
    {
        modifiers.Add(new Modifier { Source = source, Stat = stat, Amount = amount });
        StatsChanged?.Invoke();
    }

    public void RemoveModifiers(object source)
    {
        modifiers.RemoveAll(m => m.Source == source);
        StatsChanged?.Invoke();
    }
}