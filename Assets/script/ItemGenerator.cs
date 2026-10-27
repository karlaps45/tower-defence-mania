using System.Collections.Generic;
using UnityEngine;

public static class ItemGenerator
{
    // Common, Uncommon, Rare, Epic, Legendary
    private static readonly float[] RarityWeights = { 60f, 25f, 10f, 4f, 1f, 0.1f };
    private static readonly int[] StatCounts = { 1, 2, 2, 3, 3, 4 };

    // Stat roll range at Common. Higher rarities multiply it by (1 + 0.45 per tier).
    private static readonly Dictionary<StatType, (float min, float max, int decimals)> Ranges =
        new Dictionary<StatType, (float, float, int)>
        {
            { StatType.PhysicalPower,  (2f, 4f, 1) },
            { StatType.MagicPower,     (2f, 4f, 1) },
            { StatType.MaxHealth,      (10f, 20f, 0) },
            { StatType.AttackSpeed,    (0.1f, 0.2f, 2) },
            { StatType.CriticalChance, (3f, 6f, 1) },
            { StatType.CriticalDamage, (0.1f, 0.2f, 2) },
            { StatType.DetectionRange, (0.3f, 0.6f, 1) },
        };

    // Which stats each slot tends to roll, and how likely each is.
    private static readonly Dictionary<ItemSlotType, (StatType stat, float weight)[]> Pools =
        new Dictionary<ItemSlotType, (StatType, float)[]>
        {
            { ItemSlotType.Weapon, new[]
                {
                    (StatType.PhysicalPower, 5f), (StatType.MagicPower, 5f),
                    (StatType.AttackSpeed, 4f), (StatType.CriticalChance, 3f),
                    (StatType.CriticalDamage, 2f)
                } },
            { ItemSlotType.Armor, new[]
                {
                    (StatType.MaxHealth, 8f), (StatType.DetectionRange, 2f),
                    (StatType.PhysicalPower, 1f), (StatType.MagicPower, 1f)
                } },
            { ItemSlotType.Core, new[]
                {
                    (StatType.CriticalChance, 4f), (StatType.CriticalDamage, 4f),
                    (StatType.MagicPower, 3f), (StatType.DetectionRange, 3f),
                    (StatType.AttackSpeed, 2f), (StatType.PhysicalPower, 2f),
                    (StatType.MaxHealth, 2f)
                } },
        };

    private static readonly Dictionary<StatType, string> Adjectives =
        new Dictionary<StatType, string>
        {
            { StatType.PhysicalPower, "Brutal" }, { StatType.MagicPower, "Arcane" },
            { StatType.AttackSpeed, "Swift" }, { StatType.CriticalChance, "Keen" },
            { StatType.CriticalDamage, "Savage" }, { StatType.MaxHealth, "Sturdy" },
            { StatType.DetectionRange, "Farsight" },
        };

    private static readonly Dictionary<ItemSlotType, string[]> Nouns =
        new Dictionary<ItemSlotType, string[]>
        {
            { ItemSlotType.Weapon, new[] { "Blade", "Cannon", "Staff", "Launcher" } },
            { ItemSlotType.Armor, new[] { "Plate", "Mantle", "Shell", "Aegis" } },
            { ItemSlotType.Core, new[] { "Core", "Crystal", "Reactor", "Sigil" } },
        };

    // Random rarity and slot. Luck shifts the odds toward higher rarities (0 = normal).
    public static ItemData GenerateRandom(float luck = 0f, int seed = -1)
    {
        System.Random rng = seed >= 0 ? new System.Random(seed) : new System.Random();
        ItemRarity rarity = PickRarity(rng, luck);
        ItemSlotType slot = (ItemSlotType)rng.Next(System.Enum.GetValues(typeof(ItemSlotType)).Length);
        return Build(rarity, slot, rng);
    }

    public static ItemData Generate(ItemRarity rarity, ItemSlotType? slot = null, int seed = -1)
    {
        System.Random rng = seed >= 0 ? new System.Random(seed) : new System.Random();
        ItemSlotType chosen = slot ?? (ItemSlotType)rng.Next(System.Enum.GetValues(typeof(ItemSlotType)).Length);
        return Build(rarity, chosen, rng);
    }

    private static ItemData Build(ItemRarity rarity, ItemSlotType slot, System.Random rng)
    {
        ItemData item = new ItemData { slot = slot, rarity = rarity };
        float scale = 1f + 0.45f * (int)rarity;

        List<(StatType stat, float weight)> pool =
            new List<(StatType stat, float weight)>(Pools[slot]);
        int statCount = StatCounts[(int)rarity];

        for (int i = 0; i < statCount && pool.Count > 0; i++)
        {
            int index = PickWeighted(rng, pool);
            StatType stat = pool[index].stat;
            pool.RemoveAt(index);

            var range = Ranges[stat];
            float value = (range.min + (float)rng.NextDouble() * (range.max - range.min)) * scale;
            float factor = Mathf.Pow(10f, range.decimals);

            item.stats.Add(new StatBonus
            {
                stat = stat,
                amount = Mathf.Round(value * factor) / factor
            });
        }

        if (rarity == ItemRarity.Mythical)
        {
            item.abilities = MythicalAbilities.Roll(rng);
        }
        else
        {
            item.abilities = AbilityGenerator.RollAbilities((int)rarity, rng.Next());

            // Legendary items always carry an ability.
            if (item.abilities.Count == 0 && rarity == ItemRarity.Legendary)
            {
                item.abilities.Add(AbilityGenerator.Generate((int)rarity, rng));
            }
        }

        item.itemName = BuildName(item, rng);
        return item;
    }

    private static string BuildName(ItemData item, System.Random rng)
    {
        string[] nouns = Nouns[item.slot];
        string noun = nouns[rng.Next(nouns.Length)];
        if (item.rarity == ItemRarity.Mythical && item.abilities.Count > 0)
        {
            return $"{item.abilities[0].abilityName} {noun}";
        }
        string adjective = item.stats.Count > 0 ? Adjectives[item.stats[0].stat] : "Plain";
        return $"{adjective} {noun}";
    }

    private static ItemRarity PickRarity(System.Random rng, float luck)
    {
        float[] weights = new float[RarityWeights.Length];
        float total = 0f;

        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] = RarityWeights[i] * (1f + Mathf.Max(0f, luck) * i);
            total += weights[i];
        }

        float roll = (float)rng.NextDouble() * total;
        for (int i = 0; i < weights.Length; i++)
        {
            roll -= weights[i];
            if (roll <= 0f) return (ItemRarity)i;
        }
        return ItemRarity.Common;
    }

    private static int PickWeighted(System.Random rng, List<(StatType stat, float weight)> pool)
    {
        float total = 0f;
        foreach (var entry in pool) total += entry.weight;

        float roll = (float)rng.NextDouble() * total;
        for (int i = 0; i < pool.Count; i++)
        {
            roll -= pool[i].weight;
            if (roll <= 0f) return i;
        }
        return pool.Count - 1;
    }
}