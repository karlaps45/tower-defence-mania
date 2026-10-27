using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public enum ItemSlotType { Weapon, Armor, Core }
public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary, Mythical }

[Serializable]
public struct StatBonus
{
    public StatType stat;
    public float amount;
}

[Serializable]
public class ItemData
{
    public string itemName = "Item";
    public ItemSlotType slot;
    public ItemRarity rarity;
    public List<StatBonus> stats = new List<StatBonus>();
    public List<AbilityData> abilities = new List<AbilityData>();

    public Color RarityColor
    {
        get
        {
            switch (rarity)
            {
                case ItemRarity.Uncommon: return new Color(0.3f, 0.85f, 0.3f);
                case ItemRarity.Rare: return new Color(0.3f, 0.55f, 1f);
                case ItemRarity.Epic: return new Color(0.7f, 0.35f, 0.95f);
                case ItemRarity.Legendary: return new Color(1f, 0.6f, 0.1f);
                case ItemRarity.Mythical: return new Color(1f, 0.2f, 0.55f);
                default: return new Color(0.75f, 0.75f, 0.75f);
            }
        }
    }

    public string ColoredName =>
        $"<color=#{ColorUtility.ToHtmlStringRGB(RarityColor)}>{itemName}</color>";

    public static string FormatStat(StatBonus bonus)
    {
        string nice = Regex.Replace(bonus.stat.ToString(), "(?<!^)([A-Z])", " $1");

        switch (bonus.stat)
        {
            case StatType.CriticalChance: return $"+{bonus.amount:0.#}% {nice}";
            case StatType.CriticalDamage: return $"+{bonus.amount:0.##}x {nice}";
            case StatType.AttackSpeed: return $"+{bonus.amount:0.##} {nice}";
            default: return $"+{bonus.amount:0.#} {nice}";
        }
    }

    public string GetDescription()
    {
        StringBuilder sb = new StringBuilder();

        foreach (StatBonus stat in stats)
        {
            sb.AppendLine(FormatStat(stat));
        }

        foreach (AbilityData ability in abilities)
        {
            sb.AppendLine();
            sb.AppendLine($"<b>{ability.abilityName}</b>");
            sb.AppendLine(AbilityDescriber.Describe(ability));
            if (!string.IsNullOrEmpty(ability.flavor))
                sb.AppendLine($"<i>{ability.flavor}</i>");
        }

        return sb.ToString().TrimEnd();
    }
}