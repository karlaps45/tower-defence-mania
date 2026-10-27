using System.Collections.Generic;
using System.Text;

public static class AbilityDescriber
{
    public static string Describe(AbilityData d)
    {
        string sentence = TriggerText(d) + EffectText(d);
        return Capitalize(sentence) + "." + ModifierText(d);
    }

    private static string TriggerText(AbilityData d)
    {
        string chance = d.procChance < 1f ? $" ({Pct(d.procChance)} chance)" : "";

        switch (d.trigger)
        {
            case AbilityTrigger.Passive: return "";
            case AbilityTrigger.Timed: return $"every {d.interval:0.#}s, ";
            case AbilityTrigger.OnBulletHit: return $"on hit{chance}, ";
            case AbilityTrigger.OnTowerDamaged: return $"when damaged{chance}, ";
            case AbilityTrigger.OnCrit:
                string crit = d.minCritTier >= 3 ? "on a 2x recursive crit"
                    : d.minCritTier == 2 ? "on a recursive crit" : "on crit";
                return crit + chance + ", ";
            case AbilityTrigger.OnKill: return $"on kill{chance}, ";
            case AbilityTrigger.OnLowHealth: return $"below {d.lowHealthPercent:0}% health, ";
            case AbilityTrigger.EveryNthShot: return $"every {d.nthShot}th shot, ";
            default: return "when the shield breaks, ";
        }
    }

    private static string EffectText(AbilityData d)
    {
        string element = d.element != DamageType.Physical ? $" as {Nice(d.element.ToString())} damage" : "";

        switch (d.effect)
        {
            case AbilityEffectType.FireBullets:
                string pattern;
                switch (d.pattern)
                {
                    case BulletPattern.Spread: pattern = $"fire {d.count} bullets in a {d.spreadAngle:0}° spread"; break;
                    case BulletPattern.Ring: pattern = $"fire a ring of {d.count} bullets"; break;
                    case BulletPattern.Burst: pattern = $"fire a burst of {d.count} bullets"; break;
                    default: pattern = "fire a bullet"; break;
                }
                return $"{pattern} dealing {Pct(d.multiplier)} {Nice(d.scaling.ToString())}{element}{Extras(d.bulletModifiers)}";

            case AbilityEffectType.Shockwave:
                return $"release a shockwave (radius {d.radius:0.#}) dealing {Pct(d.multiplier)} {Nice(d.scaling.ToString())}{element}{Extras(d.bulletModifiers)}";

            case AbilityEffectType.Shield:
                string decay = "";
                if (d.decayPerSecond > 0f) decay += $", fading {d.decayPerSecond:0.#}/s";
                if (d.decayPerHitPercent > 0f) decay += $", losing {d.decayPerHitPercent:0}% per hit";
                return $"gain a shield of {Pct(d.multiplier)} {Nice(d.scaling.ToString())}{TargetText(d)}{decay}";

            case AbilityEffectType.Heal:
                return $"heal {Pct(d.multiplier)} Magic Power{TargetText(d)}";

            case AbilityEffectType.Regen:
                return $"regenerate {Pct(d.multiplier)} Magic Power per second";

            case AbilityEffectType.Thorns:
                return $"reflect {Pct(d.multiplier)} of damage taken back at attackers";

            case AbilityEffectType.Buff:
                string stat = Nice(d.buffStat.ToString());
                if (d.trigger == AbilityTrigger.Passive)
                    return $"{AuraWho(d)} within {d.radius:0.#} gain +{d.buffAmount:0.#} {stat}";
                return $"grant +{d.buffAmount:0.#} {stat} for {d.duration:0.#}s{TargetText(d)}";

            case AbilityEffectType.Lifesteal:
                return $"heal for {Pct(d.multiplier)} of damage dealt";

            case AbilityEffectType.Coins:
                return $"gain {d.count} coins";

            case AbilityEffectType.DoubleShot:
                return $"{Pct(d.procChance)} chance to fire an extra shot";

            case AbilityEffectType.ShotModifier:
                return "your shots gain: " + ExtrasPlain(d.bulletModifiers);
        }
        return "";
    }

    private static string AuraWho(AbilityData d)
    {
        return d.target == AbilityTarget.Self ? "this tower"
            : d.target == AbilityTarget.AlliesInRange ? "nearby towers" : "this tower and nearby towers";
    }

    private static string TargetText(AbilityData d)
    {
        switch (d.target)
        {
            case AbilityTarget.AlliesInRange: return $" for nearby allies (radius {d.radius:0.#})";
            case AbilityTarget.SelfAndAllies: return $" for itself and nearby allies (radius {d.radius:0.#})";
            default: return "";
        }
    }

    private static string ModifierText(AbilityData d)
    {
        StringBuilder sb = new StringBuilder();

        switch (d.condition)
        {
            case AbilityCondition.TargetPoisoned: sb.Append(" Only works on poisoned targets."); break;
            case AbilityCondition.TargetBurning: sb.Append(" Only works on burning targets."); break;
            case AbilityCondition.TargetFrozen: sb.Append(" Only works on frozen targets."); break;
            case AbilityCondition.TargetBelowHealth:
                sb.Append($" Only works on targets below {d.conditionValue:0}% health."); break;
        }

        if (d.repeat > 1) sb.Append($" Repeats {d.repeat} times.");
        if (d.healthCostPercent > 0f) sb.Append($" Costs {d.healthCostPercent:0}% of max health.");
        if (d.internalCooldown > 0f && d.trigger != AbilityTrigger.Timed)
            sb.Append($" ({d.internalCooldown:0.#}s cooldown)");

        return sb.ToString();
    }

    private static string ExtrasPlain(BulletModifiers m)
    {
        List<string> parts = new List<string>();

        if (m.Size > 1.01f) parts.Add($"{m.Size:0.#}x size");
        if (m.pierce > 0) parts.Add($"pierce {m.pierce}");
        if (m.poisonDps > 0f) parts.Add($"poison {m.poisonDps:0.#}/s for {m.poisonDuration:0.#}s");
        if (m.burnDps > 0f) parts.Add($"burn {m.burnDps:0.#}/s for {m.burnDuration:0.#}s");
        if (m.slowPercent > 0f)
            parts.Add(m.slowPercent >= 100f
                ? $"freeze for {m.slowDuration:0.#}s"
                : $"slow {m.slowPercent:0}% for {m.slowDuration:0.#}s");
        if (m.explodeMultiplier > 0f)
            parts.Add($"explode poisoned targets ({Pct(m.explodeMultiplier)}, radius {m.explodeRadius:0.#})");

        return string.Join(", ", parts);
    }

    private static string Extras(BulletModifiers m)
    {
        string plain = ExtrasPlain(m);
        return plain.Length == 0 ? "" : " (" + plain + ")";
    }

    private static string Pct(float value)
    {
        return UnityEngine.Mathf.RoundToInt(value * 100f) + "%";
    }

    private static string Capitalize(string s)
    {
        return s.Length == 0 ? s : char.ToUpper(s[0]) + s.Substring(1);
    }

    private static string Nice(string name)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i])) sb.Append(' ');
            sb.Append(name[i]);
        }
        return sb.ToString();
    }
}