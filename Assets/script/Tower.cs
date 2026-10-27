using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class Tower : MonoBehaviour
{
    private static readonly List<Tower> activeTowers = new List<Tower>();

    public static IReadOnlyList<Tower> ActiveTowers => activeTowers;

    public event System.Action<Transform, Vector2> ShotFired;
    public bool TargetsEnemies => targetType == TowerTargetType.Enemies;
    private AbilityController abilities;

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

    private CircleCollider2D detectionRadius;
    private readonly List<Transform> enemiesInRange = new List<Transform>();
    private readonly List<Health> alliesInRange = new List<Health>();
    private Transform currentTarget;
    private float attackTimer;

    public Transform CurrentTarget => currentTarget;
    public TowerStats RuntimeStats => stats;   // keeps other scripts compiling
    public float PlacementRadius => placementRadius;
    public int Cost => stats != null ? stats.Cost : 0;

    [Header("Bullet modifiers (testing for now, abilities will fill these later)")]
    [SerializeField] private BulletModifiers bulletModifiers;

    public event System.Action<Health, DamageInfo> BulletHit;

    public void NotifyBulletHit(Health target, DamageInfo info)
    {
        BulletHit?.Invoke(target, info);
    }
    public bool IsDetectionCollider(Collider2D otherCollider)
    {
        return otherCollider == detectionRadius;
    }

    private void Awake()
    {
        if (stats == null) stats = GetComponent<TowerStats>();
        abilities = GetComponent<AbilityController>();
        if (abilities == null) abilities = gameObject.AddComponent<AbilityController>();
        if (GetComponent<EquipmentSlots>() == null) gameObject.AddComponent<EquipmentSlots>();

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
        detectionRadius = GetComponent<CircleCollider2D>();
        detectionRadius.isTrigger = true;

        ApplyStats(refillHealth: true);
        stats.StatsChanged += OnStatsChanged;
    }

    private void OnDestroy()
    {
        if (stats != null) stats.StatsChanged -= OnStatsChanged;
    }

    private void OnStatsChanged() => ApplyStats(refillHealth: false);

    private void ApplyStats(bool refillHealth)
    {
        detectionRadius.radius = stats.DetectionRange;

        Health health = GetComponent<Health>();
        if (health != null)
        {
            health.SetMaximumHealth(stats.MaxHealth, refillHealth);
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
        attackTimer = 1f / Mathf.Max(0.01f, stats.AttackSpeed);
    }
    public void FireBullet(BulletConfig config, Vector2 direction)
    {
        if (bulletPrefab == null)
            return;

        Transform shotOrigin = firePoint != null ? firePoint : transform;
        Bullet newBullet = Instantiate(bulletPrefab, shotOrigin.position, Quaternion.identity);
        newBullet.Initialize(gameObject, config);

        Rigidbody2D body = newBullet.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = direction.normalized * bulletSpeed;
        }
    }

    public BulletConfig BuildShotConfig()
    {
        BulletConfig config = targetType == TowerTargetType.Enemies
            ? BulletConfig.Basic(BulletEffect.Damage, DamageType.Physical,
                stats.PhysicalPower, stats.CriticalChance, stats.CriticalDamage)
            : BulletConfig.Basic(BulletEffect.Heal, DamageType.Magic,
                stats.MagicPower, 0f, 1f);

        config.Modifiers = abilities != null
            ? abilities.ModifyShot(bulletModifiers)
            : bulletModifiers;
        return config;
    }
    private void Shoot(Transform target)
    {
        Transform shotOrigin = firePoint != null ? firePoint : transform;
        Vector2 direction = (target.position - shotOrigin.position).normalized;

        FireBullet(BuildShotConfig(), direction);
        ShotFired?.Invoke(target, direction);
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