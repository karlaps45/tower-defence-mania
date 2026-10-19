using UnityEngine;

public enum Slottag
{
    none,
    chestplate,
    helmet,
    charm,
    lootbox,
}
[CreateAssetMenu(menuName = "equipment/Equipment" )]
public class Item : ScriptableObject
{

    

    public Sprite sprite;
    public Slottag Itemtag;

    [Header("stats")]
    public int cost = 10;
    public float PhysicalPower = 10f;
    public float MagicPower = 10f;
    public float detectionRange = 10f;
    public float maxHealth = 10f;
    public float AttackSpeed = 10f;
    public float criticalchance = 10f;
    public float criticalDamage = 10f;
}
