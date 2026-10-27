using UnityEngine;

public enum DamageType { Physical, Magic, Poison, Fire, True }

public struct DamageInfo
{
    public float Amount;
    public DamageType Type;
    public GameObject Source;
    public int CritTier;
    public bool FromAbility;
    public bool IsCrit => CritTier > 0;
}

public static class CritResolver
{
    private const float Tier2Multiplier = 1.5f;
    private const float Tier3Multiplier = 2.3f;

    public static int RollTier(float critChance)
    {
        int tier = 0;
        for (int i = 0; i < 3; i++)
        {
            float chance = critChance - 100f * i;
            if (chance <= 0f || Random.value * 100f >= chance) break;
            tier++;
        }
        return tier;
    }

    public static DamageInfo Resolve(
        float baseAmount, DamageType type, GameObject source,
        float critChance, float critDamage)
    {
        int tier = RollTier(critChance);
        float multiplier = tier == 0 ? 1f
            : tier == 1 ? critDamage
            : tier == 2 ? critDamage * Tier2Multiplier
            : critDamage * Tier3Multiplier;

        return new DamageInfo
        {
            Amount = baseAmount * multiplier,
            Type = type,
            Source = source,
            CritTier = tier
        };
    }

    public static float Multiplier(int tier, float critDamage)
    {
        switch (tier)
        {
            case 0: return 1f;
            case 1: return critDamage;
            case 2: return critDamage * Tier2Multiplier;
            default: return critDamage * Tier3Multiplier;
        }
    }
}