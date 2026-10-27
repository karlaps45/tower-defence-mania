using System.Collections.Generic;
using UnityEngine;

public enum BulletEffect
{
    Damage,
    Heal
}

public class Bullet : MonoBehaviour
{
    private static readonly Color CritColor = new Color(1f, 0.92f, 0.1f);       // yellow
    private static readonly Color RecursiveColor = new Color(1f, 0.15f, 0.15f); // red
    private static readonly Color DoubleRecursiveColor = new Color(0.2f, 0.3f, 1f); // blue
    private static readonly Color PoisonColor = new Color(0.2f, 0.9f, 0.2f);    // green
    private static readonly Color FireColor = new Color(1f, 0.5f, 0f);          // orange
    private static readonly Color FrostColor = new Color(0.6f, 0.85f, 1f);      // light blue

    [SerializeField] private BulletEffect effect;
    [SerializeField] private float amount = 10f;
    [SerializeField] private float lifetime = 5f;

    private GameObject owner;
    private TeamMember ownerTeam;
    private Tower ownerTower;
    private BulletConfig config;
    private int critTier;
    private float critMultiplier = 1f;
    private float finalAmount;
    private int hitsLeft = 1;
    private readonly HashSet<Health> hitTargets = new HashSet<Health>();

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    // Old overloads keep working.
    public void Initialize(GameObject bulletOwner)
    {
        Initialize(bulletOwner, BulletConfig.Basic(
            effect, DamageType.Physical, amount, 0f, 1f));
    }

    public void Initialize(
        GameObject bulletOwner,
        BulletEffect bulletEffect,
        float effectAmount)
    {
        Initialize(bulletOwner, BulletConfig.Basic(
            bulletEffect, DamageType.Physical, effectAmount, 0f, 1f));
    }

    public void Initialize(GameObject bulletOwner, BulletConfig bulletConfig)
    {
        owner = bulletOwner;
        ownerTeam = owner != null ? owner.GetComponentInParent<TeamMember>() : null;
        ownerTower = owner != null ? owner.GetComponentInParent<Tower>() : null;

        config = bulletConfig;
        effect = config.Effect;
        hitsLeft = 1 + Mathf.Max(0, config.Modifiers.pierce);

        // Crit is rolled once, at launch, so the bullet can show its color in flight.
        critTier = effect == BulletEffect.Damage
            ? CritResolver.RollTier(config.CritChance)
            : 0;
        critMultiplier = CritResolver.Multiplier(critTier, config.CritDamage);
        finalAmount = config.Amount * critMultiplier;

        transform.localScale *= config.Modifiers.Size;
        ApplyColor();
    }

    private void ApplyColor()
    {
        Color color;

        if (critTier == 1) color = CritColor;
        else if (critTier == 2) color = RecursiveColor;
        else if (critTier >= 3) color = DoubleRecursiveColor;
        else
        {
            // No crit: blend the colors of whatever effects the bullet carries.
            BulletModifiers m = config.Modifiers;
            Color sum = new Color(0f, 0f, 0f, 0f);
            int count = 0;

            if (m.poisonDps > 0f || m.explodeMultiplier > 0f) { sum += PoisonColor; count++; }
            if (m.burnDps > 0f) { sum += FireColor; count++; }
            if (m.slowPercent > 0f) { sum += FrostColor; count++; }

            if (count == 0) return;   // plain bullet keeps its sprite color

            color = sum / count;
            color.a = 1f;
        }

        foreach (SpriteRenderer sprite in GetComponentsInChildren<SpriteRenderer>())
        {
            sprite.color = color;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hitsLeft <= 0)
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
            Spend();
            return;
        }

        // Several colliders on one target must not hit twice.
        if (!hitTargets.Add(targetHealth))
            return;

        TeamMember targetTeam = targetHealth.GetComponentInParent<TeamMember>();

        if (targetTeam == null || ownerTeam == null)
        {
            Spend();
            return;
        }

        bool isAlly = targetTeam.Team == ownerTeam.Team;

        if (effect == BulletEffect.Damage && !isAlly)
        {
            DamageTarget(targetHealth);
            hitsLeft--;
        }
        else if (effect == BulletEffect.Heal && isAlly)
        {
            targetHealth.Heal(finalAmount);
            hitsLeft--;
        }
        else
        {
            hitsLeft = 0;   // wrong team: bullet is used up, same as before
        }

        if (hitsLeft <= 0)
        {
            Spend();
        }
    }

    private void Spend()
    {
        hitsLeft = 0;
        Destroy(gameObject);
    }

    private void DamageTarget(Health target)
    {
        BulletModifiers m = config.Modifiers;
        StatusEffects status = target.GetComponent<StatusEffects>();

        // Explode on an already-poisoned target: consumes the poison.
        if (m.explodeMultiplier > 0f && m.explodeRadius > 0f &&
            status != null && status.HasPoison)
        {
            float storedPoison = status.ConsumePoison();
            Explode(target.transform.position,
                storedPoison * m.explodeMultiplier * critMultiplier);
        }

        DamageInfo info = new DamageInfo
        {
            Amount = finalAmount,
            Type = config.DamageType,
            Source = owner,
            CritTier = critTier
        };

        target.TakeDamage(info);

        if (!target.IsDead)
        {
            ApplyStatuses(target, m);
        }

        if (ownerTower != null)
        {
            ownerTower.NotifyBulletHit(target, info);
        }
    }

    private void ApplyStatuses(Health target, BulletModifiers m)
    {
        bool hasPoison = m.poisonDps > 0f && m.poisonDuration > 0f;
        bool hasBurn = m.burnDps > 0f && m.burnDuration > 0f;
        bool hasSlow = m.slowPercent > 0f && m.slowDuration > 0f;

        if (!hasPoison && !hasBurn && !hasSlow)
            return;

        StatusEffects status = StatusEffects.GetOrAdd(target.gameObject);

        // Crit is baked into damage over time when it is applied.
        if (hasPoison) status.AddPoison(m.poisonDps * critMultiplier, m.poisonDuration, owner);
        if (hasBurn) status.ApplyBurn(m.burnDps * critMultiplier, m.burnDuration, owner);
        if (hasSlow) status.ApplySlow(m.slowPercent, m.slowDuration);
    }

    private void Explode(Vector2 position, float damage)
    {
        if (damage <= 0f)
            return;

        HashSet<Health> alreadyHit = new HashSet<Health>();

        foreach (Collider2D hit in Physics2D.OverlapCircleAll(position, config.Modifiers.explodeRadius))
        {
            Health health = hit.GetComponentInParent<Health>();
            if (health == null || !alreadyHit.Add(health))
                continue;

            TeamMember team = health.GetComponentInParent<TeamMember>();
            if (team == null || ownerTeam == null || team.Team == ownerTeam.Team)
                continue;

            health.TakeDamage(new DamageInfo
            {
                Amount = damage,
                Type = DamageType.Magic,
                Source = owner,
                CritTier = critTier
            });
        }
    }
}