using System;
using UnityEngine;

public enum AbilityTrigger
{
    Passive, Timed, OnBulletHit, OnTowerDamaged, OnCrit,
    OnKill, OnLowHealth, EveryNthShot, OnShieldBreak
}

public enum AbilityEffectType
{
    FireBullets, Shockwave, Shield, Heal, Regen, Thorns,
    Buff, Lifesteal, Coins, DoubleShot, ShotModifier
}

public enum BulletPattern { Single, Spread, Ring, Burst }
public enum ScalingStat { PhysicalPower, MagicPower, MaxHealth, AttackSpeed }
public enum AbilityTarget { Self, AlliesInRange, SelfAndAllies }

public enum AbilityCondition
{
    None, TargetPoisoned, TargetBurning, TargetFrozen, TargetBelowHealth
}

[Serializable]
public class AbilityData
{
    public string abilityName = "Ability";
    [TextArea] public string flavor;

    [Header("Trigger")]
    public AbilityTrigger trigger = AbilityTrigger.Timed;
    [Range(0f, 1f)] public float procChance = 1f;
    [Tooltip("Seconds before it can proc again")] public float internalCooldown;
    [Tooltip("Timed: seconds between uses")] public float interval = 6f;
    [Tooltip("Every Nth shot")] public int nthShot = 5;
    [Tooltip("On low health: % threshold")] public float lowHealthPercent = 30f;
    [Tooltip("On crit: 1 = any, 2 = recursive, 3 = 2x recursive")] public int minCritTier = 1;

    [Header("Effect")]
    public AbilityEffectType effect = AbilityEffectType.FireBullets;
    public ScalingStat scaling = ScalingStat.PhysicalPower;
    [Tooltip("Strength as a multiple of the scaling stat. Thorns/Lifesteal: fraction (0.2 = 20%). Heal and Regen always use Magic Power.")]
    public float multiplier = 1f;
    public DamageType element = DamageType.Physical;
    public AbilityTarget target = AbilityTarget.Self;
    public float radius = 3f;
    public float duration = 4f;
    [Tooltip("Bullets in the pattern / coins gained")] public int count = 1;
    public BulletPattern pattern = BulletPattern.Single;
    public float spreadAngle = 40f;
    public float burstInterval = 0.1f;

    [Header("Shield decay")]
    public float decayPerSecond;
    public float decayPerHitPercent;

    [Header("Buff")]
    public StatType buffStat = StatType.PhysicalPower;
    public float buffAmount = 3f;

    [Header("Bullet / shockwave extras (size, pierce, poison, fire, frost, explode)")]
    public BulletModifiers bulletModifiers;

    [Header("Modifiers")]
    public AbilityCondition condition = AbilityCondition.None;
    public float conditionValue = 30f;
    [Min(1)] public int repeat = 1;
    [Tooltip("% of max health paid to use it")] public float healthCostPercent;

    public bool IsPassiveOnlyEffect =>
        effect == AbilityEffectType.Regen ||
        effect == AbilityEffectType.Thorns ||
        effect == AbilityEffectType.Lifesteal ||
        effect == AbilityEffectType.DoubleShot ||
        effect == AbilityEffectType.ShotModifier;

    public AbilityData Clone()
    {
        return (AbilityData)MemberwiseClone();
    }
}