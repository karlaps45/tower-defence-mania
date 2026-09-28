using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float movementSpeed = 5f;

    [Header("Tower attack")]
    [SerializeField] private Bullet attackBulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float attackRange = 6f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float attackDamage = 2f;
    [SerializeField] private float bulletSpeed = 8f;
    [SerializeField] private float targetRefreshInterval = 0.25f;

    [Header("Reward")]
    [SerializeField] private int coinReward = 5;

    private Transform[] waypoints;
    private int waypointIndex;
    private Tower closestTower;
    private float attackTimer;
    private float targetRefreshTimer;
    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();

        if (health != null)
        {
            health.Died += RewardPlayer;
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Died -= RewardPlayer;
        }
    }

    public void Initialize(
        Transform[] newWaypoints,
        float speedMultiplier = 1f,
        float healthMultiplier = 1f)
    {
        waypoints = newWaypoints;
        waypointIndex = 0;
        movementSpeed *= speedMultiplier;

        if (health != null)
        {
            health.SetMaximumHealth(
                health.MaximumHealth * healthMultiplier
            );
        }
    }

    private void Update()
    {
        UpdateTowerAttack();

        if (waypoints == null || waypoints.Length == 0)
            return;

        Transform target = waypoints[waypointIndex];

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            movementSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, target.position) < 0.05f)
        {
            waypointIndex++;

            if (waypointIndex >= waypoints.Length)
            {
                ReachedEnd();
            }
        }
    }

    private void UpdateTowerAttack()
    {
        attackTimer -= Time.deltaTime;
        targetRefreshTimer -= Time.deltaTime;

        if (targetRefreshTimer <= 0f)
        {
            FindClosestTower();
            targetRefreshTimer = targetRefreshInterval;
        }

        if (closestTower == null || attackBulletPrefab == null)
            return;

        float distanceToTower = Vector2.Distance(
            transform.position,
            closestTower.transform.position
        );

        if (distanceToTower <= attackRange && attackTimer <= 0f)
        {
            ShootAt(closestTower.transform);
            attackTimer = attackCooldown;
        }
    }

    private void FindClosestTower()
    {
        closestTower = null;
        float closestDistanceSquared = float.PositiveInfinity;

        foreach (Tower tower in Tower.ActiveTowers)
        {
            if (tower == null || !tower.gameObject.activeInHierarchy)
                continue;

            float distanceSquared = (
                tower.transform.position - transform.position
            ).sqrMagnitude;

            if (distanceSquared < closestDistanceSquared)
            {
                closestDistanceSquared = distanceSquared;
                closestTower = tower;
            }
        }
    }

    private void ShootAt(Transform target)
    {
        Transform shotOrigin = firePoint != null ? firePoint : transform;
        Vector2 direction = (
            target.position - shotOrigin.position
        ).normalized;

        Bullet newBullet = Instantiate(
            attackBulletPrefab,
            shotOrigin.position,
            Quaternion.identity
        );

        newBullet.Initialize(gameObject, BulletEffect.Damage, attackDamage);

        Rigidbody2D bulletBody = newBullet.GetComponent<Rigidbody2D>();
        if (bulletBody != null)
        {
            bulletBody.linearVelocity = direction * bulletSpeed;
        }
    }

    private void ReachedEnd()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.DamagePlayer(1);
        }

        Destroy(gameObject);
    }

    private void RewardPlayer()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCoins(coinReward);
        }
    }
}
