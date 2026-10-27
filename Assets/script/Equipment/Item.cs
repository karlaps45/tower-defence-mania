using System.Runtime.ConstrainedExecution;
using UnityEngine;

public enum Slottag
{
    none,
    chestplate,
    helmet,
    charm,
    lootbox,
}
[CreateAssetMenu(menuName = "equipment/Equipment")]
public class Item : ScriptableObject
{



    public Sprite sprite;
    public SlotTag itemTag;

    [Header("stats")]
    [SerializeField] private int cost = 10;
    [SerializeField] private float physicalPower = 10f;
    [SerializeField] private float magicPower = 10f;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float maxHealth = 10f;
    [SerializeField] private float attackSpeed = 10f;
    [SerializeField] private float criticalChance = 10f;
    [SerializeField] private float criticalDamage = 10f;

    public int Cost
    {
        get { return cost; }
    }

    public float PhysicalPower
    {
        get { return physicalPower; }
    }
    public float MagicPower
    {
        get { return magicPower; }
    }

    public float DetectionRange
    {
        get { return detectionRange; }
    }

    public float MaxHealth
    {
        get { return maxHealth; }
    }

    public float AttackSpeed
    {
        get { return attackSpeed; }
    }

     public float CriticleChance
    {
        get { return criticalChance; }
    }
    public float CriticalDamage
    {
        get { return criticalDamage;  }
    }


}
