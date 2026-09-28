using UnityEngine;

public enum Team
{
    Player,
    Enemy
}

public class TeamMember : MonoBehaviour
{
    [field: SerializeField]
    public Team Team { get; private set; }
}