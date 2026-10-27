using System.Collections.Generic;
using UnityEngine;

public class StatusEffects : MonoBehaviour
{
    private const float TickInterval = 0.25f;

    private class PoisonStack
    {
        public float Dps;
        public float Remaining;
    }

    private readonly List<PoisonStack> poisonStacks = new List<PoisonStack>();
    private float burnDps;
    private float burnRemaining;
    private float slowPercent;
    private float slowRemaining;

    private float pendingPoison;
    private float pendingBurn;
    private float tickTimer;
    private GameObject lastSource;
    private Health health;

    public bool HasPoison => poisonStacks.Count > 0;
    public bool IsBurning => burnRemaining > 0f;
    public bool IsSlowed => slowRemaining > 0f;
    public bool IsFrozen => IsSlowed && slowPercent >= 100f;
    public float SpeedMultiplier =>
        IsSlowed ? Mathf.Clamp01(1f - slowPercent / 100f) : 1f;

    public static StatusEffects GetOrAdd(GameObject target)
    {
        StatusEffects status = target.GetComponent<StatusEffects>();
        if (status == null)
        {
            status = target.AddComponent<StatusEffects>();
        }
        return status;
    }

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    public void AddPoison(float dps, float duration, GameObject source)
    {
        poisonStacks.Add(new PoisonStack { Dps = dps, Remaining = duration });
        lastSource = source;
    }

    // Burn doesn't stack: refreshes the duration, keeps the stronger DPS.
    public void ApplyBurn(float dps, float duration, GameObject source)
    {
        burnDps = IsBurning ? Mathf.Max(burnDps, dps) : dps;
        burnRemaining = duration;
        lastSource = source;
    }

    // Slow doesn't stack either: strongest slow wins, duration refreshes.
    public void ApplySlow(float percent, float duration)
    {
        slowPercent = IsSlowed ? Mathf.Max(slowPercent, percent) : percent;
        slowRemaining = duration;
    }

    // Removes all poison and returns the damage it had left to deal.
    public float ConsumePoison()
    {
        float total = 0f;
        foreach (PoisonStack stack in poisonStacks)
        {
            total += stack.Dps * stack.Remaining;
        }

        poisonStacks.Clear();
        pendingPoison = 0f;
        return total;
    }

    private void Update()
    {
        if (health == null || health.IsDead)
            return;

        float dt = Time.deltaTime;

        for (int i = poisonStacks.Count - 1; i >= 0; i--)
        {
            PoisonStack stack = poisonStacks[i];
            pendingPoison += stack.Dps * Mathf.Min(dt, stack.Remaining);
            stack.Remaining -= dt;

            if (stack.Remaining <= 0f)
            {
                poisonStacks.RemoveAt(i);
            }
        }

        if (burnRemaining > 0f)
        {
            pendingBurn += burnDps * Mathf.Min(dt, burnRemaining);
            burnRemaining -= dt;
        }

        if (slowRemaining > 0f)
        {
            slowRemaining -= dt;
        }

        // Damage lands in small ticks, not every frame, so it doesn't flood events.
        tickTimer += dt;
        bool dotsFinished = !HasPoison && !IsBurning;

        if (tickTimer >= TickInterval || (dotsFinished && (pendingPoison > 0f || pendingBurn > 0f)))
        {
            Flush();
            tickTimer = 0f;
        }
    }

    private void Flush()
    {
        if (pendingPoison > 0f)
        {
            health.TakeDamage(new DamageInfo
            {
                Amount = pendingPoison,
                Type = DamageType.Poison,
                Source = lastSource
            });
            pendingPoison = 0f;
        }

        if (pendingBurn > 0f && !health.IsDead)
        {
            health.TakeDamage(new DamageInfo
            {
                Amount = pendingBurn,
                Type = DamageType.Magic,
                Source = lastSource
            });
            pendingBurn = 0f;
        }
    }

    public static void ApplyFromModifiers(
    Health target, BulletModifiers m, float critMultiplier, GameObject source)
    {
        bool hasPoison = m.poisonDps > 0f && m.poisonDuration > 0f;
        bool hasBurn = m.burnDps > 0f && m.burnDuration > 0f;
        bool hasSlow = m.slowPercent > 0f && m.slowDuration > 0f;

        if (!hasPoison && !hasBurn && !hasSlow)
            return;

        StatusEffects status = GetOrAdd(target.gameObject);
        if (hasPoison) status.AddPoison(m.poisonDps * critMultiplier, m.poisonDuration, source);
        if (hasBurn) status.ApplyBurn(m.burnDps * critMultiplier, m.burnDuration, source);
        if (hasSlow) status.ApplySlow(m.slowPercent, m.slowDuration);
    }
}