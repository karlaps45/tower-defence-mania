using System;
using System.Collections.Generic;

public static class MythicalAbilities
{
    private static readonly Func<Random, AbilityData>[] Presets =
    {
        Worldbreaker, Plaguebringer, PhoenixHeart, Stormcaller, GlacialThrone,
        MidasTouch, Chronomancer, Soulreaver, ReapingHaste, EternalThorns,
        Supernova, DragonsBreath, AegisDominion, BloodPact, Deathbloom
    };

    // 1 mythical ability guaranteed, 35% for a second, ~4% for a third. No repeats.
    public static List<AbilityData> Roll(Random rng)
    {
        int count = 1;
        if (rng.NextDouble() < 0.35)
        {
            count = 2;
            if (rng.NextDouble() < 0.12) count = 3;
        }

        List<int> pool = new List<int>();
        for (int i = 0; i < Presets.Length; i++) pool.Add(i);

        List<AbilityData> result = new List<AbilityData>();
        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int pick = rng.Next(pool.Count);
            result.Add(Presets[pool[pick]](rng));
            pool.RemoveAt(pick);
        }
        return result;
    }

    // Small random variation so two copies are never identical.
    private static float V(Random rng, float value, float spread = 0.1f)
    {
        return value * (1f + ((float)rng.NextDouble() * 2f - 1f) * spread);
    }

    private static AbilityData Worldbreaker(Random rng)
    {
        AbilityData d = new AbilityData
        {
            abilityName = "Worldbreaker",
            flavor = "The ground remembers every step it was denied.",
            trigger = AbilityTrigger.Timed,
            interval = V(rng, 14f),
            effect = AbilityEffectType.Shockwave,
            scaling = ScalingStat.PhysicalPower,
            element = DamageType.Physical,
            multiplier = V(rng, 6f),
            radius = V(rng, 7f)
        };
        d.bulletModifiers.slowPercent = 100f;     // freezes everything it hits
        d.bulletModifiers.slowDuration = 1.5f;
        return d;
    }

    private static AbilityData Plaguebringer(Random rng)
    {
        AbilityData d = new AbilityData
        {
            abilityName = "Plaguebringer",
            flavor = "Nothing it touches stays healthy for long.",
            trigger = AbilityTrigger.Passive,
            effect = AbilityEffectType.ShotModifier
        };
        d.bulletModifiers.poisonDps = V(rng, 6f);
        d.bulletModifiers.poisonDuration = 6f;
        d.bulletModifiers.explodeMultiplier = V(rng, 1.5f);
        d.bulletModifiers.explodeRadius = V(rng, 4f);
        return d;
    }

    private static AbilityData PhoenixHeart(Random rng)
    {
        return new AbilityData
        {
            abilityName = "Phoenix Heart",
            flavor = "It burns out, and it burns back.",
            trigger = AbilityTrigger.OnLowHealth,
            lowHealthPercent = 30f,
            internalCooldown = 45f,
            effect = AbilityEffectType.Heal,
            scaling = ScalingStat.MagicPower,
            multiplier = V(rng, 8f),
            target = AbilityTarget.SelfAndAllies,
            radius = V(rng, 6f)
        };
    }

    private static AbilityData Stormcaller(Random rng)
    {
        AbilityData d = new AbilityData
        {
            abilityName = "Stormcaller",
            flavor = "Every third breath is a thunderclap.",
            trigger = AbilityTrigger.EveryNthShot,
            nthShot = 3,
            effect = AbilityEffectType.FireBullets,
            pattern = BulletPattern.Ring,
            count = 12,
            scaling = ScalingStat.MagicPower,
            element = DamageType.Magic,
            multiplier = V(rng, 1.1f)
        };
        d.bulletModifiers.pierce = 2;
        return d;
    }

    private static AbilityData GlacialThrone(Random rng)
    {
        AbilityData d = new AbilityData
        {
            abilityName = "Glacial Throne",
            flavor = "Winter does not hurry. It simply arrives.",
            trigger = AbilityTrigger.Passive,
            effect = AbilityEffectType.ShotModifier
        };
        d.bulletModifiers.slowPercent = V(rng, 60f);
        d.bulletModifiers.slowDuration = 3f;
        d.bulletModifiers.size = 1.3f;
        return d;
    }

    private static AbilityData MidasTouch(Random rng)
    {
        return new AbilityData
        {
            abilityName = "Midas Touch",
            flavor = "Everything it kills turns to coin.",
            trigger = AbilityTrigger.OnKill,
            procChance = 1f,
            internalCooldown = 0.2f,
            effect = AbilityEffectType.Coins,
            count = rng.Next(4, 7)
        };
    }

    private static AbilityData Chronomancer(Random rng)
    {
        return new AbilityData
        {
            abilityName = "Chronomancer",
            flavor = "Seconds are cheaper when you own them.",
            trigger = AbilityTrigger.Passive,
            effect = AbilityEffectType.Buff,
            target = AbilityTarget.SelfAndAllies,
            radius = V(rng, 6f),
            buffStat = StatType.AttackSpeed,
            buffAmount = V(rng, 0.6f)
        };
    }

    private static AbilityData Soulreaver(Random rng)
    {
        return new AbilityData
        {
            abilityName = "Soulreaver",
            flavor = "What it takes, it keeps.",
            trigger = AbilityTrigger.Passive,
            effect = AbilityEffectType.Lifesteal,
            multiplier = V(rng, 0.25f)
        };
    }

    private static AbilityData ReapingHaste(Random rng)
    {
        return new AbilityData
        {
            abilityName = "Reaping Haste",
            flavor = "Each death quickens the hand.",
            trigger = AbilityTrigger.OnKill,
            procChance = 0.8f,
            internalCooldown = 0.3f,
            effect = AbilityEffectType.Buff,
            target = AbilityTarget.Self,
            duration = 4f,
            buffStat = StatType.AttackSpeed,
            buffAmount = V(rng, 1f)
        };
    }

    private static AbilityData EternalThorns(Random rng)
    {
        return new AbilityData
        {
            abilityName = "Eternal Thorns",
            flavor = "Every wound given is a wound returned.",
            trigger = AbilityTrigger.Passive,
            effect = AbilityEffectType.Thorns,
            element = DamageType.Physical,
            multiplier = V(rng, 1f)
        };
    }

    private static AbilityData Supernova(Random rng)
    {
        AbilityData d = new AbilityData
        {
            abilityName = "Supernova",
            flavor = "A small star, briefly furious.",
            trigger = AbilityTrigger.OnCrit,
            minCritTier = 1,
            procChance = 0.3f,
            internalCooldown = 2f,
            effect = AbilityEffectType.FireBullets,
            pattern = BulletPattern.Ring,
            count = 16,
            scaling = ScalingStat.PhysicalPower,
            element = DamageType.Fire,
            multiplier = V(rng, 1.5f)
        };
        d.bulletModifiers.burnDps = V(rng, 8f);
        d.bulletModifiers.burnDuration = 3f;
        return d;
    }

    private static AbilityData DragonsBreath(Random rng)
    {
        AbilityData d = new AbilityData
        {
            abilityName = "Dragon's Breath",
            flavor = "Ten heartbeats of pure fire.",
            trigger = AbilityTrigger.Timed,
            interval = V(rng, 8f),
            effect = AbilityEffectType.FireBullets,
            pattern = BulletPattern.Burst,
            count = 10,
            burstInterval = 0.06f,
            scaling = ScalingStat.MagicPower,
            element = DamageType.Fire,
            multiplier = V(rng, 0.9f)
        };
        d.bulletModifiers.pierce = 3;
        d.bulletModifiers.burnDps = V(rng, 6f);
        d.bulletModifiers.burnDuration = 4f;
        d.bulletModifiers.size = 1.5f;
        return d;
    }

    private static AbilityData AegisDominion(Random rng)
    {
        return new AbilityData
        {
            abilityName = "Aegis Dominion",
            flavor = "None shall fall while it stands.",
            trigger = AbilityTrigger.OnTowerDamaged,
            procChance = 0.35f,
            internalCooldown = 4f,
            effect = AbilityEffectType.Shield,
            scaling = ScalingStat.MaxHealth,
            multiplier = V(rng, 1.5f),
            target = AbilityTarget.SelfAndAllies,
            radius = V(rng, 5f),
            decayPerSecond = 2f,
            decayPerHitPercent = 5f
        };
    }

    private static AbilityData BloodPact(Random rng)
    {
        return new AbilityData
        {
            abilityName = "Blood Pact",
            flavor = "Power is a debt paid in advance.",
            trigger = AbilityTrigger.Timed,
            interval = V(rng, 10f),
            effect = AbilityEffectType.Buff,
            target = AbilityTarget.SelfAndAllies,
            radius = V(rng, 6f),
            duration = 6f,
            buffStat = StatType.PhysicalPower,
            buffAmount = V(rng, 25f),
            healthCostPercent = 12f
        };
    }

    private static AbilityData Deathbloom(Random rng)
    {
        AbilityData d = new AbilityData
        {
            abilityName = "Deathbloom",
            flavor = "Where one falls, a garden grows.",
            trigger = AbilityTrigger.OnKill,
            procChance = 0.6f,
            internalCooldown = 0.3f,
            effect = AbilityEffectType.Shockwave,
            scaling = ScalingStat.MagicPower,
            element = DamageType.Poison,
            multiplier = V(rng, 2f),
            radius = V(rng, 4f)
        };
        d.bulletModifiers.poisonDps = V(rng, 4f);
        d.bulletModifiers.poisonDuration = 5f;
        return d;
    }
}