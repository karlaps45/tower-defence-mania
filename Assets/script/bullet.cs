using UnityEngine;

public enum BulletEffect
{
    Damage,
    Heal
}

public class Bullet : MonoBehaviour
{
    [SerializeField] private BulletEffect effect;
    [SerializeField] private float amount = 10f;
    [SerializeField] private float lifetime = 5f;

    private GameObject owner;
    private TeamMember ownerTeam;
    private bool hasHit;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void Initialize(GameObject bulletOwner)
    {
        owner = bulletOwner;
        ownerTeam = owner.GetComponentInParent<TeamMember>();
    }

    public void Initialize(
        GameObject bulletOwner,
        BulletEffect bulletEffect,
        float effectAmount)
    {
        Initialize(bulletOwner);
        effect = bulletEffect;
        amount = effectAmount;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit)
            return;

        Tower tower = other.GetComponentInParent<Tower>();
        if (tower != null && tower.IsDetectionCollider(other))
            return;

        // Don't hit the object that fired the bullet.
        if (owner != null &&
            (other.gameObject == owner ||
             other.transform.IsChildOf(owner.transform)))
        {
            return;
        }

        Health targetHealth = other.GetComponentInParent<Health>();

        // The bullet hit a wall or something without health.
        if (targetHealth == null)
        {
            hasHit = true;
            Destroy(gameObject);
            return;
        }

        TeamMember targetTeam =
            targetHealth.GetComponentInParent<TeamMember>();

        if (targetTeam == null || ownerTeam == null)
        {
            hasHit = true;
            Destroy(gameObject);
            return;
        }

        bool isAlly = targetTeam.Team == ownerTeam.Team;

        if (effect == BulletEffect.Damage && !isAlly)
        {
            targetHealth.TakeDamage(amount);
        }
        else if (effect == BulletEffect.Heal && isAlly)
        {
            targetHealth.Heal(amount);
        }

        hasHit = true;
        Destroy(gameObject);
    }
}
