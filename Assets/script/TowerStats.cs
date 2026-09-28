using System.Runtime.ConstrainedExecution;
using UnityEngine;





[CreateAssetMenu(menuName = "Tower/Tower Stats")]
public class TowerStats : ScriptableObject
{
    public int cost = 10;
    public float PhysicalPower = 10f;
    public float MagicPower = 10f;
    public float detectionRange = 10f;
    public float maxHealth = 10f;
    public float AttackSpeed = 10f;
    public float criticalchance = 10f;
    public float criticalDamage = 10f;


}
