using System;
using UnityEngine;

[Serializable]
public struct BulletModifiers
{
    [Tooltip("0 or 1 = normal size")] public float size;
    [Tooltip("Extra targets the bullet passes through")] public int pierce;

    [Header("Poison (green)")]
    public float poisonDps;
    public float poisonDuration;

    [Header("Explode if target already poisoned")]
    public float explodeMultiplier;
    public float explodeRadius;

    [Header("Fire (orange)")]
    public float burnDps;
    public float burnDuration;

    [Header("Frost (light blue), 100 = frozen")]
    public float slowPercent;
    public float slowDuration;

    public float Size => size > 0f ? size : 1f;

    public static BulletModifiers Merge(BulletModifiers a, BulletModifiers b)
    {
        BulletModifiers r = a;
        r.size = Mathf.Max(a.Size, b.Size);
        r.pierce = a.pierce + b.pierce;
        r.poisonDps = a.poisonDps + b.poisonDps;
        r.poisonDuration = Mathf.Max(a.poisonDuration, b.poisonDuration);
        r.explodeMultiplier = Mathf.Max(a.explodeMultiplier, b.explodeMultiplier);
        r.explodeRadius = Mathf.Max(a.explodeRadius, b.explodeRadius);
        r.burnDps = a.burnDps + b.burnDps;
        r.burnDuration = Mathf.Max(a.burnDuration, b.burnDuration);
        r.slowPercent = Mathf.Max(a.slowPercent, b.slowPercent);
        r.slowDuration = Mathf.Max(a.slowDuration, b.slowDuration);
        return r;
    }

}

[Serializable]
public struct BulletConfig
{
    public BulletEffect Effect;
    public DamageType DamageType;
    public float Amount;
    public float CritChance;
    public float CritDamage;
    public BulletModifiers Modifiers;
    public bool FromAbility;
    public static BulletConfig Basic(
        BulletEffect effect, DamageType damageType, float amount,
        float critChance, float critDamage)
    {
        return new BulletConfig
        {
            Effect = effect,
            DamageType = damageType,
            Amount = amount,
            CritChance = critChance,
            CritDamage = critDamage
        };
    }
}