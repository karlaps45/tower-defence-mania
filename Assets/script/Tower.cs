using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class Tower : MonoBehaviour
{
    private static readonly List<Tower> activeTowers = new List<Tower>();

    public static IReadOnlyList<Tower> ActiveTowers => activeTowers;

    public enum TowerTargetType
    {
        Enemies,
        Allies
    }

    [SerializeField] private TowerStats stats;
    [SerializeField] private TowerTargetType targetType = TowerTargetType.Enemies;
    [SerializeField, Min(0.1f)] private float placementRadius = 0.5f;

    [Header("Projectile")]
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 10f;

    private TowerStats runtimeStats;
    private CircleCollider2D detectionRadius;
    private readonly List<Transform> enemiesInRange = new List<Transform>();
    private readonly List<Health> alliesInRange = new List<Health>();
    private Transform currentTarget;
    private float attackTimer;

    public Transform CurrentTarget => currentTarget;
    public TowerStats RuntimeStats => runtimeStats;
    public float PlacementRadius => placementRadius;
    public int Cost => stats != null ? stats.cost : 0;

    public bool IsDetectionCollider(Collider2D otherCollider)
    {
        return otherCollider == detectionRadius;
    }

    private void OnEnable()
    {
        if (!activeTowers.Contains(this))
        {
            activeTowers.Add(this);
        }
    }

    private void OnDisable()
    {
        activeTowers.Remove(this);
    }

    private void Start()
    {
        runtimeStats = Instantiate(stats);

        Health health = GetComponent<Health>();
        if (health != null)
        {
            health.SetMaximumHealth(runtimeStats.maxHealth);
        }

        detectionRadius = GetComponent<CircleCollider2D>();
        detectionRadius.isTrigger = true;
        detectionRadius.radius = runtimeStats.detectionRange;
    }

    public void IncreaseMaximumHealth(float amount)
    {
        runtimeStats.maxHealth += amount;

        Health health = GetComponent<Health>();
        if (health != null)
        {
            health.SetMaximumHealth(runtimeStats.maxHealth);
        }
    }

    private void Update()
    {
        SelectTarget();
        UpdateShooting();
    }

    private void SelectTarget()
    {
        if (targetType == TowerTargetType.Allies)
        {
            alliesInRange.RemoveAll(ally => ally == null);
            alliesInRange.Sort((first, second) =>
                first.CurrentHealth.CompareTo(second.CurrentHealth));

            currentTarget = alliesInRange.Count > 0
                ? alliesInRange[0].transform
                : null;
        }
        else
        {
            enemiesInRange.RemoveAll(enemy => enemy == null);
            currentTarget = FindClosestEnemy();
        }
    }

    private Transform FindClosestEnemy()
    {
        Transform closestEnemy = null;
        float closestDistanceSquared = float.PositiveInfinity;

        foreach (Transform enemy in enemiesInRange)
        {
            float distanceSquared = (
                enemy.position - transform.position
            ).sqrMagnitude;

            if (distanceSquared < closestDistanceSquared)
            {
                closestDistanceSquared = distanceSquared;
                closestEnemy = enemy;
            }
        }

        return closestEnemy;
    }

    private void UpdateShooting()
    {
        attackTimer -= Time.deltaTime;

        if (currentTarget == null || bulletPrefab == null || attackTimer > 0f)
            return;

        Shoot(currentTarget);

        // AttackSpeed is treated as attacks per second.
        attackTimer = 1f / Mathf.Max(0.01f, runtimeStats.AttackSpeed);
    }

    private void Shoot(Transform target)
    {
        Transform shotOrigin = firePoint != null ? firePoint : transform;
        Vector2 direction = (
            target.position - shotOrigin.position
        ).normalized;

        Bullet newBullet = Instantiate(
            bulletPrefab,
            shotOrigin.position,
            Quaternion.identity
        );

        BulletEffect effect = targetType == TowerTargetType.Enemies
            ? BulletEffect.Damage
            : BulletEffect.Heal;

        float amount = targetType == TowerTargetType.Enemies
            ? runtimeStats.PhysicalPower
            : runtimeStats.MagicPower;

        newBullet.Initialize(gameObject, effect, amount);

        Rigidbody2D bulletBody = newBullet.GetComponent<Rigidbody2D>();
        if (bulletBody != null)
        {
            bulletBody.linearVelocity = direction * bulletSpeed;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Transform target = other.transform.root;

        if (targetType == TowerTargetType.Enemies && target.CompareTag("Enemy"))
        {
            if (!enemiesInRange.Contains(target))
            {
                enemiesInRange.Add(target);
            }
        }
        else if (targetType == TowerTargetType.Allies && target.CompareTag("Player"))
        {
            Health health = target.GetComponent<Health>();

            if (health != null && !alliesInRange.Contains(health))
            {
                alliesInRange.Add(health);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Transform target = other.transform.root;
        enemiesInRange.Remove(target);

        Health health = target.GetComponent<Health>();
        if (health != null)
        {
            alliesInRange.Remove(health);
        }
    }
}
