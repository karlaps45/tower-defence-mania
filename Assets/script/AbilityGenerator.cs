using System.Collections.Generic;
using UnityEngine;

public static class AbilityGenerator
{
    private static readonly (AbilityEffectType effect, int weight)[] EffectWeights =
    {
        (AbilityEffectType.FireBullets, 22),
        (AbilityEffectType.Shockwave, 14),
        (AbilityEffectType.Shield, 10),
        (AbilityEffectType.Heal, 8),
        (AbilityEffectType.Regen, 5),
        (AbilityEffectType.Thorns, 6),
        (AbilityEffectType.Buff, 10),
        (AbilityEffectType.Lifesteal, 5),
        (AbilityEffectType.Coins, 4),
        (AbilityEffectType.DoubleShot, 5),
        (AbilityEffectType.ShotModifier, 8),
    };

    // ---------- Public ----------

    // How many abilities an item gets: none 65%, one 28%, two 6%, three 1% (shifts up with rarity 0-4).
    public static List<AbilityData> RollAbilities(int rarity, int seed = -1)
    {
        System.Random rng = seed >= 0 ? new System.Random(seed) : new System.Random();
        rarity = Mathf.Clamp(rarity, 0, 4);

        float none = 65f - 8f * rarity;
        float one = 28f + 4f * rarity;
        float two = 6f + 3f * rarity;
        float roll = Range(rng, 0f, 100f);

        int count = roll < none ? 0
            : roll < none + one ? 1
            : roll < none + one + two ? 2
            : 3;

        List<AbilityData> result = new List<AbilityData>();
        for (int i = 0; i < count; i++)
        {
            result.Add(Generate(rarity, rng));
        }
        return result;
    }

    public static AbilityData Generate(int rarity, int seed = -1)
    {
        System.Random rng = seed >= 0 ? new System.Random(seed) : new System.Random();
        return Generate(rarity, rng);
    }

    public static AbilityData Generate(int rarity, System.Random rng)
    {
        rarity = Mathf.Clamp(rarity, 0, 4);
        float power = 1f + rarity * 0.3f;

        AbilityData d = new AbilityData();
        d.effect = PickEffect(rng);
        d.trigger = PickTrigger(rng, d.effect);

        ConfigureTrigger(d, rng);
        ConfigureEffect(d, rng, power * TriggerFactor(d));
        AddModifiers(d, rng, rarity, power);

        d.abilityName = BuildName(d);
        return d;
    }

    // ---------- Helpers ----------

    private static float Range(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }

    private static int RangeInt(System.Random rng, int min, int max)
    {
        return rng.Next(min, max + 1);
    }

    private static bool Chance(System.Random rng, float probability)
    {
        return rng.NextDouble() < probability;
    }

    private static T Pick<T>(System.Random rng, params T[] options)
    {
        return options[rng.Next(options.Length)];
    }

    private static AbilityEffectType PickEffect(System.Random rng)
    {
        int total = 0;
        foreach (var entry in EffectWeights) total += entry.weight;

        int roll = rng.Next(total);
        foreach (var entry in EffectWeights)
        {
            roll -= entry.weight;
            if (roll < 0) return entry.effect;
        }
        return AbilityEffectType.FireBullets;
    }

    private static AbilityTrigger PickTrigger(System.Random rng, AbilityEffectType effect)
    {
        switch (effect)
        {
            case AbilityEffectType.Regen:
            case AbilityEffectType.Thorns:
            case AbilityEffectType.Lifesteal:
            case AbilityEffectType.DoubleShot:
            case AbilityEffectType.ShotModifier:
                return AbilityTrigger.Passive;

            case AbilityEffectType.Buff:
                return Pick(rng, AbilityTrigger.Passive, AbilityTrigger.Timed, AbilityTrigger.OnCrit,
                    AbilityTrigger.OnKill, AbilityTrigger.OnLowHealth, AbilityTrigger.OnTowerDamaged);

            case AbilityEffectType.Shield:
                return Pick(rng, AbilityTrigger.Timed, AbilityTrigger.OnLowHealth,
                    AbilityTrigger.OnTowerDamaged, AbilityTrigger.OnKill);

            case AbilityEffectType.Heal:
                return Pick(rng, AbilityTrigger.Timed, AbilityTrigger.OnLowHealth, AbilityTrigger.OnKill,
                    AbilityTrigger.OnCrit, AbilityTrigger.OnTowerDamaged, AbilityTrigger.EveryNthShot);

            case AbilityEffectType.Coins:
                return Pick(rng, AbilityTrigger.OnKill, AbilityTrigger.OnCrit, AbilityTrigger.OnBulletHit);

            default: // FireBullets, Shockwave
                return Pick(rng, AbilityTrigger.Timed, AbilityTrigger.OnBulletHit, AbilityTrigger.OnTowerDamaged,
                    AbilityTrigger.OnCrit, AbilityTrigger.OnKill, AbilityTrigger.EveryNthShot,
                    AbilityTrigger.OnLowHealth, AbilityTrigger.OnShieldBreak);
        }
    }

    private static void ConfigureTrigger(AbilityData d, System.Random rng)
    {
        switch (d.trigger)
        {
            case AbilityTrigger.Timed:
                d.interval = Range(rng, 4f, 12f);
                break;
            case AbilityTrigger.OnBulletHit:
                d.procChance = Range(rng, 0.10f, 0.15f);
                d.internalCooldown = 1f;
                break;
            case AbilityTrigger.OnTowerDamaged:
                d.procChance = Range(rng, 0.20f, 0.30f);
                d.internalCooldown = 3f;
                break;
            case AbilityTrigger.OnCrit:
                d.procChance = Range(rng, 0.25f, 0.35f);
                d.internalCooldown = 1.5f;
                d.minCritTier = Chance(rng, 0.15f) ? 2 : 1;
                break;
            case AbilityTrigger.OnKill:
                d.procChance = Range(rng, 0.40f, 0.60f);
                d.internalCooldown = 0.5f;
                break;
            case AbilityTrigger.OnLowHealth:
                d.lowHealthPercent = Range(rng, 25f, 35f);
                d.internalCooldown = 20f;
                break;
            case AbilityTrigger.EveryNthShot:
                d.nthShot = RangeInt(rng, 4, 10);
                break;
            case AbilityTrigger.OnShieldBreak:
                d.internalCooldown = 8f;
                break;
        }
    }

    // Rarer triggers and longer waits allow bigger effects.
    private static float TriggerFactor(AbilityData d)
    {
        switch (d.trigger)
        {
            case AbilityTrigger.Timed: return Mathf.Clamp(d.interval / 5f, 0.6f, 2.2f);
            case AbilityTrigger.OnBulletHit: return 1.1f;
            case AbilityTrigger.OnTowerDamaged: return 1.0f;
            case AbilityTrigger.OnCrit: return d.minCritTier >= 2 ? 2f : 1.1f;
            case AbilityTrigger.OnKill: return 0.9f;
            case AbilityTrigger.OnLowHealth: return 1.8f;
            case AbilityTrigger.EveryNthShot: return Mathf.Clamp(d.nthShot / 5f, 0.8f, 2f);
            case AbilityTrigger.OnShieldBreak: return 1.4f;
            default: return 1f;
        }
    }

    private static void ConfigureEffect(AbilityData d, System.Random rng, float strength)
    {
        switch (d.effect)
        {
            case AbilityEffectType.FireBullets:
                d.pattern = Pick(rng, BulletPattern.Single, BulletPattern.Spread, BulletPattern.Ring, BulletPattern.Burst);
                d.count = d.pattern == BulletPattern.Spread ? RangeInt(rng, 3, 7)
                    : d.pattern == BulletPattern.Ring ? RangeInt(rng, 6, 12)
                    : d.pattern == BulletPattern.Burst ? RangeInt(rng, 3, 6) : 1;
                d.spreadAngle = Range(rng, 25f, 70f);
                d.burstInterval = Range(rng, 0.08f, 0.15f);
                SetOffenseScaling(d, rng);
                d.multiplier = 1.3f * strength / Mathf.Sqrt(d.count);

                if (Chance(rng, 0.2f))   // giant bullets
                {
                    d.bulletModifiers.size = Range(rng, 1.5f, 2.5f);
                    d.multiplier *= 1.3f;
                }
                if (Chance(rng, 0.2f)) d.bulletModifiers.pierce = RangeInt(rng, 1, 3);
                break;

            case AbilityEffectType.Shockwave:
                d.radius = Range(rng, 2.5f, 5f);
                SetOffenseScaling(d, rng);
                d.multiplier = 1.6f * strength;
                break;

            case AbilityEffectType.Shield:
                d.scaling = Chance(rng, 0.5f) ? ScalingStat.MagicPower : ScalingStat.MaxHealth;
                d.multiplier = (d.scaling == ScalingStat.MaxHealth ? 0.6f : 2f) * strength;
                d.target = Pick(rng, AbilityTarget.Self, AbilityTarget.SelfAndAllies, AbilityTarget.AlliesInRange);
                d.radius = Range(rng, 3f, 5f);
                d.decayPerSecond = Chance(rng, 0.7f) ? Range(rng, 0.5f, 1.5f) : 0f;
                d.decayPerHitPercent = Chance(rng, 0.5f) ? Range(rng, 5f, 12f) : 0f;
                if (d.decayPerSecond <= 0f && d.decayPerHitPercent <= 0f) d.decayPerSecond = 1f;
                break;

            case AbilityEffectType.Heal:
                d.scaling = ScalingStat.MagicPower;
                d.multiplier = 1.5f * strength;
                d.target = Pick(rng, AbilityTarget.Self, AbilityTarget.SelfAndAllies, AbilityTarget.AlliesInRange);
                d.radius = Range(rng, 3f, 5f);
                break;

            case AbilityEffectType.Regen:
                d.scaling = ScalingStat.MagicPower;
                d.multiplier = 0.15f * strength;
                break;

            case AbilityEffectType.Thorns:
                d.element = Physical();
                d.multiplier = Mathf.Min(0.8f, Range(rng, 0.15f, 0.3f) * strength);
                break;

            case AbilityEffectType.Buff:
                ConfigureBuff(d, rng, strength);
                break;

            case AbilityEffectType.Lifesteal:
                d.multiplier = Range(rng, 0.05f, 0.1f) * strength;
                break;

            case AbilityEffectType.Coins:
                d.count = Mathf.Max(1, Mathf.RoundToInt(Range(rng, 1f, 3f) * strength));
                break;

            case AbilityEffectType.DoubleShot:
                d.procChance = Range(rng, 0.10f, 0.20f) * Mathf.Min(strength, 1.6f);
                break;

            case AbilityEffectType.ShotModifier:
                ConfigureShotModifier(d, rng, strength);
                break;
        }
    }

    private static DamageType Physical() { return DamageType.Physical; }

    private static void SetOffenseScaling(AbilityData d, System.Random rng)
    {
        bool magic = Chance(rng, 0.5f);
        d.scaling = magic ? ScalingStat.MagicPower : ScalingStat.PhysicalPower;
        d.element = magic ? DamageType.Magic : DamageType.Physical;
    }

    private static void ConfigureBuff(AbilityData d, System.Random rng, float strength)
    {
        d.buffStat = Pick(rng, StatType.PhysicalPower, StatType.MagicPower, StatType.AttackSpeed,
            StatType.CriticalChance, StatType.CriticalDamage, StatType.MaxHealth);
        d.target = d.trigger == AbilityTrigger.Passive
            ? Pick(rng, AbilityTarget.AlliesInRange, AbilityTarget.SelfAndAllies)
            : Pick(rng, AbilityTarget.Self, AbilityTarget.SelfAndAllies, AbilityTarget.AlliesInRange);
        d.radius = Range(rng, 3f, 5f);
        d.duration = Range(rng, 3f, 6f);

        float scale = d.trigger == AbilityTrigger.Passive ? 0.6f : 1f;   // auras are always on, so weaker

        switch (d.buffStat)
        {
            case StatType.PhysicalPower:
            case StatType.MagicPower: d.buffAmount = Range(rng, 3f, 6f) * strength * scale; break;
            case StatType.AttackSpeed: d.buffAmount = Range(rng, 0.2f, 0.5f) * strength * scale; break;
            case StatType.CriticalChance: d.buffAmount = Range(rng, 6f, 12f) * strength * scale; break;
            case StatType.CriticalDamage: d.buffAmount = Range(rng, 0.15f, 0.3f) * strength * scale; break;
            default: d.buffAmount = Range(rng, 8f, 16f) * strength * scale; break;
        }
    }

    private static void ConfigureShotModifier(AbilityData d, System.Random rng, float strength)
    {
        int kind = rng.Next(5);
        switch (kind)
        {
            case 0: AddStatus(d, rng, 0, strength); break;   // poison
            case 1: AddStatus(d, rng, 1, strength); break;   // fire
            case 2: AddStatus(d, rng, 2, strength); break;   // frost
            case 3: d.bulletModifiers.pierce = Mathf.Max(1, Mathf.RoundToInt(Range(rng, 1f, 2f) * strength)); break;
            default: d.bulletModifiers.size = Range(rng, 1.3f, 1.8f); break;
        }
    }

    // kind: 0 = poison, 1 = fire, 2 = frost
    private static void AddStatus(AbilityData d, System.Random rng, int kind, float strength)
    {
        if (kind == 0)
        {
            d.bulletModifiers.poisonDps = Range(rng, 1.5f, 3f) * strength;
            d.bulletModifiers.poisonDuration = Range(rng, 3f, 5f);
        }
        else if (kind == 1)
        {
            d.bulletModifiers.burnDps = Range(rng, 2f, 4f) * strength;
            d.bulletModifiers.burnDuration = Range(rng, 2f, 4f);
        }
        else
        {
            bool freeze = Chance(rng, 0.1f);
            d.bulletModifiers.slowPercent = freeze ? 100f : Range(rng, 20f, 60f);
            d.bulletModifiers.slowDuration = freeze ? Range(rng, 1f, 1.5f) : Range(rng, 2f, 4f);
        }
    }

    private static void AddModifiers(AbilityData d, System.Random rng, int rarity, float power)
    {
        bool offense = d.effect == AbilityEffectType.FireBullets || d.effect == AbilityEffectType.Shockwave;
        bool activates = !d.IsPassiveOnlyEffect && d.trigger != AbilityTrigger.Passive;

        List<string> available = new List<string>();
        if (offense) { available.Add("status"); available.Add("condition"); available.Add("repeat"); }
        if (d.effect == AbilityEffectType.Heal) available.Add("repeat");
        if (activates) available.Add("cost");

        if (available.Count == 0 || !Chance(rng, 0.35f + rarity * 0.05f)) return;

        int slots = Chance(rng, 0.25f) ? 2 : 1;

        for (int i = 0; i < slots && available.Count > 0; i++)
        {
            string pick = available[rng.Next(available.Count)];
            available.Remove(pick);

            switch (pick)
            {
                case "status":
                    AddStatus(d, rng, rng.Next(3), power);
                    d.multiplier *= 0.9f;
                    if (d.bulletModifiers.poisonDps > 0f && d.effect == AbilityEffectType.FireBullets && Chance(rng, 0.3f))
                    {
                        d.bulletModifiers.explodeMultiplier = Range(rng, 0.6f, 1.2f);
                        d.bulletModifiers.explodeRadius = Range(rng, 2f, 3.5f);
                    }
                    break;

                case "condition":
                    d.condition = Pick(rng, AbilityCondition.TargetPoisoned, AbilityCondition.TargetBurning,
                        AbilityCondition.TargetFrozen, AbilityCondition.TargetBelowHealth);
                    d.conditionValue = Range(rng, 25f, 40f);
                    d.multiplier *= 1.6f;

                    // Make sure the tower can actually meet a status condition.
                    if (d.condition == AbilityCondition.TargetPoisoned) AddStatus(d, rng, 0, power);
                    else if (d.condition == AbilityCondition.TargetBurning) AddStatus(d, rng, 1, power);
                    else if (d.condition == AbilityCondition.TargetFrozen)
                    {
                        d.bulletModifiers.slowPercent = 100f;
                        d.bulletModifiers.slowDuration = 1.5f;
                    }
                    break;

                case "repeat":
                    d.repeat = RangeInt(rng, 2, 3);
                    d.multiplier *= 0.7f;
                    break;

                case "cost":
                    d.healthCostPercent = Range(rng, 5f, 15f);
                    d.multiplier *= 1.3f;
                    d.buffAmount *= 1.3f;
                    break;
            }
        }
    }

    private static string BuildName(AbilityData d)
    {
        BulletModifiers m = d.bulletModifiers;

        string prefix = "";
        if (m.poisonDps > 0f) prefix = "Venomous ";
        else if (m.burnDps > 0f) prefix = "Blazing ";
        else if (m.slowPercent >= 100f) prefix = "Freezing ";
        else if (m.slowPercent > 0f) prefix = "Frost ";
        else if (m.Size > 1.4f) prefix = "Giant ";
        else if (m.pierce > 0) prefix = "Piercing ";
        else if (d.element == DamageType.Magic) prefix = "Arcane ";

        string noun;
        switch (d.effect)
        {
            case AbilityEffectType.FireBullets:
                noun = d.pattern == BulletPattern.Spread ? "Spread"
                    : d.pattern == BulletPattern.Ring ? "Ring"
                    : d.pattern == BulletPattern.Burst ? "Burst" : "Shot";
                break;
            case AbilityEffectType.Shockwave: noun = "Shockwave"; break;
            case AbilityEffectType.Shield: noun = "Ward"; break;
            case AbilityEffectType.Heal: noun = "Mending"; break;
            case AbilityEffectType.Regen: noun = "Renewal"; break;
            case AbilityEffectType.Thorns: noun = "Thorns"; break;
            case AbilityEffectType.Buff: noun = d.trigger == AbilityTrigger.Passive ? "Aura" : "Rally"; break;
            case AbilityEffectType.Lifesteal: noun = "Leech"; break;
            case AbilityEffectType.Coins: noun = "Bounty"; break;
            case AbilityEffectType.DoubleShot: noun = "Echo Shot"; break;
            default: noun = "Infusion"; break;
        }

        return prefix + noun;
    }
}