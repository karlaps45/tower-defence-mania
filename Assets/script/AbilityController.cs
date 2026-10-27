using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct AbilityContext
{
    public Health Target;
    public GameObject Attacker;
}

public class AbilityRuntime
{
    public readonly AbilityData Data;
    public float Cooldown;
    public float Timer;
    public int ShotCount;
    public bool LowHealthArmed = true;
    public readonly List<TowerStats> Buffed = new List<TowerStats>();
    public readonly Dictionary<TowerStats, float> BuffExpiry = new Dictionary<TowerStats, float>();

    public AbilityRuntime(AbilityData data) { Data = data; }

    public bool IsPassive =>
        Data.trigger == AbilityTrigger.Passive || Data.IsPassiveOnlyEffect;

    public string Description => AbilityDescriber.Describe(Data);
}

public class AbilityController : MonoBehaviour
{
    // Timed abilities speed up a little with attack speed (much less than shots do).
    private const float TimerSpeedPerAttackSpeed = 0.1f;
    private const float PassiveTick = 0.25f;
    private const float AuraRefresh = 0.5f;

    [SerializeField] private List<AbilityData> startingAbilities = new List<AbilityData>();

    private readonly List<AbilityRuntime> abilities = new List<AbilityRuntime>();
    private Tower tower;
    private TowerStats stats;
    private Health health;
    private Shield shield;
    private TeamMember ownTeam;
    private float previousShield;

    public IReadOnlyList<AbilityRuntime> Abilities => abilities;

    private void Awake()
    {
        tower = GetComponent<Tower>();
        stats = GetComponent<TowerStats>();
        health = GetComponent<Health>();
        ownTeam = GetComponentInParent<TeamMember>();

        shield = GetComponent<Shield>();
        if (shield == null) shield = gameObject.AddComponent<Shield>();
    }

    private void OnEnable()
    {
        if (tower != null)
        {
            tower.BulletHit += HandleBulletHit;
            tower.ShotFired += HandleShotFired;
        }
        if (health != null)
        {
            health.Damaged += HandleDamaged;
            health.HealthChanged += HandleHealthChanged;
        }
        if (shield != null) shield.ShieldChanged += HandleShieldChanged;
        Health.AnyKilled += HandleAnyKilled;
    }

    private void OnDisable()
    {
        if (tower != null)
        {
            tower.BulletHit -= HandleBulletHit;
            tower.ShotFired -= HandleShotFired;
        }
        if (health != null)
        {
            health.Damaged -= HandleDamaged;
            health.HealthChanged -= HandleHealthChanged;
        }
        if (shield != null) shield.ShieldChanged -= HandleShieldChanged;
        Health.AnyKilled -= HandleAnyKilled;
    }

    private void Start()
    {
        foreach (AbilityData data in startingAbilities)
        {
            AddAbility(data.Clone());
        }
    }

    // ---------- Public API (equipment will use these in stage 4) ----------

    public AbilityRuntime AddAbility(AbilityData data)
    {
        AbilityRuntime runtime = new AbilityRuntime(data);
        runtime.Timer = data.trigger == AbilityTrigger.Timed ? data.interval : 0f;
        abilities.Add(runtime);
        return runtime;
    }

    public void RemoveAbility(AbilityRuntime runtime)
    {
        if (runtime == null) return;
        Cleanup(runtime);
        abilities.Remove(runtime);
    }

    public void RemoveAbility(AbilityData data)
    {
        RemoveAbility(abilities.Find(a => a.Data == data));
    }

    public void ClearAbilities()
    {
        foreach (AbilityRuntime runtime in abilities) Cleanup(runtime);
        abilities.Clear();
    }

    // Called by Tower for every normal shot.
    public BulletModifiers ModifyShot(BulletModifiers baseModifiers)
    {
        BulletModifiers result = baseModifiers;
        foreach (AbilityRuntime r in abilities)
        {
            if (r.Data.effect == AbilityEffectType.ShotModifier)
            {
                result = BulletModifiers.Merge(result, r.Data.bulletModifiers);
            }
        }
        return result;
    }

    [ContextMenu("Add random ability (rarity 2)")]
    private void DebugAddRandomAbility()
    {
        if (!Application.isPlaying) return;
        AbilityData data = AbilityGenerator.Generate(2);
        AddAbility(data);
        Debug.Log($"{data.abilityName}: {AbilityDescriber.Describe(data)}");
    }

    [ContextMenu("Log ability descriptions")]
    private void DebugLogDescriptions()
    {
        foreach (AbilityRuntime r in abilities)
            Debug.Log($"{r.Data.abilityName}: {r.Description}");
    }

    private void Cleanup(AbilityRuntime r)
    {
        foreach (TowerStats ts in r.Buffed)
        {
            if (ts != null) ts.RemoveModifiers(r);
        }
        r.Buffed.Clear();
        r.BuffExpiry.Clear();
    }

    // ---------- Per-frame ----------

    private void Update()
    {
        if (stats == null) return;

        float dt = Time.deltaTime;
        float timerSpeed = 1f + stats.AttackSpeed * TimerSpeedPerAttackSpeed;

        for (int i = 0; i < abilities.Count; i++)
        {
            AbilityRuntime r = abilities[i];
            AbilityData d = r.Data;
            r.Cooldown -= dt;

            if (d.trigger == AbilityTrigger.Timed && !d.IsPassiveOnlyEffect)
            {
                r.Timer -= dt * timerSpeed;
                if (r.Timer <= 0f)
                {
                    // If it couldn't fire (no target yet), retry shortly.
                    r.Timer = TryProc(r, new AbilityContext()) ? d.interval : 0.25f;
                }
            }
            else if (r.IsPassive)
            {
                PassiveUpdate(r, dt);
            }

            TickBuffExpiry(r);
        }
    }

    private void PassiveUpdate(AbilityRuntime r, float dt)
    {
        AbilityData d = r.Data;

        if (d.effect == AbilityEffectType.Regen && health != null)
        {
            r.Timer += dt;
            if (r.Timer >= PassiveTick)
            {
                health.Heal(stats.MagicPower * d.multiplier * r.Timer);
                r.Timer = 0f;
            }
        }
        else if (d.effect == AbilityEffectType.Buff)
        {
            r.Timer -= dt;
            if (r.Timer <= 0f)
            {
                RefreshAura(r);
                r.Timer = AuraRefresh;
            }
        }
    }

    private void TickBuffExpiry(AbilityRuntime r)
    {
        if (r.BuffExpiry.Count == 0) return;

        List<TowerStats> expired = null;
        foreach (KeyValuePair<TowerStats, float> pair in r.BuffExpiry)
        {
            if (pair.Key == null || Time.time >= pair.Value)
            {
                if (expired == null) expired = new List<TowerStats>();
                expired.Add(pair.Key);
            }
        }

        if (expired == null) return;

        foreach (TowerStats ts in expired)
        {
            if (ts != null) ts.RemoveModifiers(r);
            r.BuffExpiry.Remove(ts);
            r.Buffed.Remove(ts);
        }
    }

    // ---------- Event handlers ----------

    private void HandleShotFired(Transform target, Vector2 direction)
    {
        Health targetHealth = target != null ? target.GetComponent<Health>() : null;

        foreach (AbilityRuntime r in abilities)
        {
            AbilityData d = r.Data;

            if (d.effect == AbilityEffectType.DoubleShot)
            {
                if (Random.value <= d.procChance)
                    tower.FireBullet(tower.BuildShotConfig(), direction);
                continue;
            }

            if (d.trigger == AbilityTrigger.EveryNthShot)
            {
                r.ShotCount++;
                if (r.ShotCount >= Mathf.Max(1, d.nthShot))
                {
                    r.ShotCount = 0;
                    TryProc(r, new AbilityContext { Target = targetHealth });
                }
            }
        }
    }

    private void HandleBulletHit(Health target, DamageInfo info)
    {
        foreach (AbilityRuntime r in abilities)
        {
            AbilityData d = r.Data;

            if (d.effect == AbilityEffectType.Lifesteal)
            {
                if (health != null) health.Heal(info.Amount * d.multiplier);
                continue;
            }

            // Ability-made hits never trigger more abilities.
            if (info.FromAbility) continue;

            AbilityContext ctx = new AbilityContext { Target = target };

            if (d.trigger == AbilityTrigger.OnBulletHit)
            {
                TryProc(r, ctx);
            }
            else if (d.trigger == AbilityTrigger.OnCrit && info.CritTier >= d.minCritTier)
            {
                TryProc(r, ctx);
            }
        }
    }

    private void HandleDamaged(DamageInfo info)
    {
        if (info.FromAbility) return;

        foreach (AbilityRuntime r in abilities)
        {
            AbilityData d = r.Data;

            if (d.effect == AbilityEffectType.Thorns)
            {
                Reflect(info, d);
            }
            else if (d.trigger == AbilityTrigger.OnTowerDamaged)
            {
                TryProc(r, new AbilityContext { Attacker = info.Source });
            }
        }
    }

    private void Reflect(DamageInfo info, AbilityData d)
    {
        if (info.Source == null || info.Amount <= 0f) return;

        Health attacker = info.Source.GetComponentInParent<Health>();
        if (attacker == null || attacker == health) return;

        TeamMember team = attacker.GetComponentInParent<TeamMember>();
        if (team == null || ownTeam == null || team.Team == ownTeam.Team) return;

        attacker.TakeDamage(new DamageInfo
        {
            Amount = info.Amount * d.multiplier,
            Type = d.element,
            Source = gameObject,
            FromAbility = true
        });
    }

    private void HandleHealthChanged(float current, float max)
    {
        float percent = max > 0f ? current / max * 100f : 100f;

        foreach (AbilityRuntime r in abilities)
        {
            if (r.Data.trigger != AbilityTrigger.OnLowHealth) continue;

            if (percent <= r.Data.lowHealthPercent)
            {
                if (r.LowHealthArmed)
                {
                    r.LowHealthArmed = false;
                    TryProc(r, new AbilityContext());
                }
            }
            else if (percent > r.Data.lowHealthPercent + 10f)
            {
                r.LowHealthArmed = true;
            }
        }
    }

    private void HandleShieldChanged(float current, float max)
    {
        bool broke = previousShield > 0f && current <= 0f;
        previousShield = current;

        if (!broke) return;

        foreach (AbilityRuntime r in abilities)
        {
            if (r.Data.trigger == AbilityTrigger.OnShieldBreak)
                TryProc(r, new AbilityContext());
        }
    }

    private void HandleAnyKilled(Health victim, DamageInfo lastHit)
    {
        if (victim == health || lastHit.Source != gameObject) return;

        foreach (AbilityRuntime r in abilities)
        {
            if (r.Data.trigger == AbilityTrigger.OnKill)
                TryProc(r, new AbilityContext());
        }
    }

    // ---------- Activation ----------

    private bool TryProc(AbilityRuntime r, AbilityContext ctx)
    {
        AbilityData d = r.Data;

        if (r.Cooldown > 0f) return false;
        if (d.procChance < 1f && Random.value > d.procChance) return false;
        if (!ConditionMet(d, ctx)) return false;
        if (!Execute(r, ctx)) return false;

        r.Cooldown = d.internalCooldown;
        return true;
    }

    private bool ConditionMet(AbilityData d, AbilityContext ctx)
    {
        if (d.condition == AbilityCondition.None) return true;

        Health target = ctx.Target;
        if (target == null && tower != null && tower.CurrentTarget != null)
        {
            target = tower.CurrentTarget.GetComponent<Health>();
        }
        if (target == null) return false;

        StatusEffects status = target.GetComponent<StatusEffects>();

        switch (d.condition)
        {
            case AbilityCondition.TargetPoisoned: return status != null && status.HasPoison;
            case AbilityCondition.TargetBurning: return status != null && status.IsBurning;
            case AbilityCondition.TargetFrozen: return status != null && status.IsFrozen;
            case AbilityCondition.TargetBelowHealth:
                return target.CurrentHealth / target.MaximumHealth * 100f <= d.conditionValue;
        }
        return true;
    }

    private bool Execute(AbilityRuntime r, AbilityContext ctx)
    {
        AbilityData d = r.Data;

        float cost = health != null ? health.MaximumHealth * d.healthCostPercent / 100f : 0f;
        if (cost > 0f && (health == null || health.CurrentHealth <= cost)) return false;

        if (!RunEffect(r, ctx)) return false;

        if (cost > 0f)
        {
            health.TakeDamage(new DamageInfo
            {
                Amount = cost,
                Type = DamageType.True,
                FromAbility = true
            });
        }

        if (d.repeat > 1)
        {
            StartCoroutine(RepeatRoutine(r, ctx, d.repeat - 1));
        }
        return true;
    }

    private IEnumerator RepeatRoutine(AbilityRuntime r, AbilityContext ctx, int times)
    {
        for (int i = 0; i < times; i++)
        {
            yield return new WaitForSeconds(0.35f);
            RunEffect(r, ctx);
        }
    }

    private bool RunEffect(AbilityRuntime r, AbilityContext ctx)
    {
        AbilityData d = r.Data;

        switch (d.effect)
        {
            case AbilityEffectType.FireBullets: return FireBulletsEffect(d, ctx);
            case AbilityEffectType.Shockwave: return ShockwaveEffect(d);
            case AbilityEffectType.Shield: return ShieldEffect(d);
            case AbilityEffectType.Heal: return HealEffect(d);
            case AbilityEffectType.Buff: return BuffEffect(r);
            case AbilityEffectType.Coins: return CoinsEffect(d);
        }
        return false;   // passive-only effects don't "fire"
    }

    // ---------- Effects ----------

    private float ScaleValue(ScalingStat scaling)
    {
        switch (scaling)
        {
            case ScalingStat.PhysicalPower: return stats.PhysicalPower;
            case ScalingStat.MagicPower: return stats.MagicPower;
            case ScalingStat.MaxHealth: return stats.MaxHealth;
            default: return stats.AttackSpeed;
        }
    }

    private BulletConfig BuildConfig(AbilityData d)
    {
        BulletConfig config = BulletConfig.Basic(
            BulletEffect.Damage, d.element,
            ScaleValue(d.scaling) * d.multiplier,
            stats.CriticalChance, stats.CriticalDamage);

        config.Modifiers = d.bulletModifiers;
        config.FromAbility = true;
        return config;
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        return Quaternion.Euler(0f, 0f, degrees) * v;
    }

    private bool FireBulletsEffect(AbilityData d, AbilityContext ctx)
    {
        if (tower == null || !tower.TargetsEnemies) return false;

        Transform targetTransform = ctx.Target != null
            ? ctx.Target.transform
            : tower.CurrentTarget;
        if (targetTransform == null) return false;

        Vector2 aim = ((Vector2)targetTransform.position - (Vector2)transform.position).normalized;
        BulletConfig config = BuildConfig(d);
        int count = Mathf.Max(1, d.count);

        switch (d.pattern)
        {
            case BulletPattern.Single:
                tower.FireBullet(config, aim);
                break;

            case BulletPattern.Spread:
                for (int i = 0; i < count; i++)
                {
                    float angle = count == 1
                        ? 0f
                        : Mathf.Lerp(-d.spreadAngle / 2f, d.spreadAngle / 2f, i / (float)(count - 1));
                    tower.FireBullet(config, Rotate(aim, angle));
                }
                break;

            case BulletPattern.Ring:
                for (int i = 0; i < count; i++)
                {
                    tower.FireBullet(config, Rotate(aim, 360f * i / count));
                }
                break;

            case BulletPattern.Burst:
                tower.FireBullet(config, aim);
                if (count > 1) StartCoroutine(BurstRoutine(d, config, count - 1));
                break;
        }
        return true;
    }

    private IEnumerator BurstRoutine(AbilityData d, BulletConfig config, int remaining)
    {
        for (int i = 0; i < remaining; i++)
        {
            yield return new WaitForSeconds(Mathf.Max(0.02f, d.burstInterval));

            if (tower == null || tower.CurrentTarget == null) yield break;

            Vector2 aim = ((Vector2)tower.CurrentTarget.position - (Vector2)transform.position).normalized;
            tower.FireBullet(config, aim);
        }
    }

    private bool ShockwaveEffect(AbilityData d)
    {
        float baseDamage = ScaleValue(d.scaling) * d.multiplier;
        HashSet<Health> seen = new HashSet<Health>();
        bool hitAny = false;

        foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, d.radius))
        {
            Health target = hit.GetComponentInParent<Health>();
            if (target == null || !seen.Add(target)) continue;

            TeamMember team = target.GetComponentInParent<TeamMember>();
            if (team == null || ownTeam == null || team.Team == ownTeam.Team) continue;

            DamageInfo info = CritResolver.Resolve(
                baseDamage, d.element, gameObject,
                stats.CriticalChance, stats.CriticalDamage);
            info.FromAbility = true;

            target.TakeDamage(info);

            if (!target.IsDead)
            {
                StatusEffects.ApplyFromModifiers(
                    target, d.bulletModifiers,
                    CritResolver.Multiplier(info.CritTier, stats.CriticalDamage),
                    gameObject);
            }
            hitAny = true;
        }

        if (hitAny)
        {
            AbilityVfx.Pulse(transform.position, d.radius, new Color(1f, 0.55f, 0.1f));
        }
        return hitAny;
    }

    private List<Health> GetHealthTargets(AbilityData d)
    {
        List<Health> result = new List<Health>();

        if (d.target != AbilityTarget.AlliesInRange && health != null)
        {
            result.Add(health);
        }

        if (d.target != AbilityTarget.Self)
        {
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, d.radius))
            {
                Health other = hit.GetComponentInParent<Health>();
                if (other == null || other == health || result.Contains(other)) continue;

                TeamMember team = other.GetComponentInParent<TeamMember>();
                if (team == null || ownTeam == null || team.Team != ownTeam.Team) continue;

                result.Add(other);
            }
        }
        return result;
    }

    private bool ShieldEffect(AbilityData d)
    {
        float amount = ScaleValue(d.scaling) * d.multiplier;
        if (amount <= 0f) return false;

        bool applied = false;
        foreach (Health target in GetHealthTargets(d))
        {
            Shield targetShield = target.GetComponent<Shield>();
            if (targetShield == null) targetShield = target.gameObject.AddComponent<Shield>();

            targetShield.Apply(amount, d.decayPerSecond, d.decayPerHitPercent);
            applied = true;
        }

        if (applied)
        {
            AbilityVfx.Pulse(transform.position, Mathf.Max(1f, d.radius * 0.5f), new Color(0.4f, 0.8f, 1f));
        }
        return applied;
    }

    private bool HealEffect(AbilityData d)
    {
        // Healing always scales with Magic Power.
        float amount = stats.MagicPower * d.multiplier;
        bool healedAny = false;

        foreach (Health target in GetHealthTargets(d))
        {
            if (target.CurrentHealth < target.MaximumHealth)
            {
                target.Heal(amount);
                healedAny = true;
            }
        }

        if (healedAny)
        {
            AbilityVfx.Pulse(transform.position, Mathf.Max(1f, d.radius * 0.5f), new Color(0.3f, 1f, 0.4f));
        }
        return healedAny;
    }

    private bool CoinsEffect(AbilityData d)
    {
        if (GameManager.Instance == null) return false;
        GameManager.Instance.AddCoins(Mathf.Max(1, d.count));
        return true;
    }

    private List<TowerStats> BuffTargets(AbilityData d)
    {
        List<TowerStats> result = new List<TowerStats>();

        if (d.target != AbilityTarget.AlliesInRange && stats != null)
        {
            result.Add(stats);
        }

        if (d.target != AbilityTarget.Self)
        {
            foreach (Tower other in Tower.ActiveTowers)
            {
                if (other == null || other == tower) continue;
                if (Vector2.Distance(other.transform.position, transform.position) > d.radius) continue;

                TowerStats otherStats = other.RuntimeStats;
                if (otherStats != null) result.Add(otherStats);
            }
        }
        return result;
    }

    // Timed / triggered buff: refreshes instead of stacking, expires after the duration.
    private bool BuffEffect(AbilityRuntime r)
    {
        AbilityData d = r.Data;
        List<TowerStats> targets = BuffTargets(d);
        if (targets.Count == 0) return false;

        float duration = d.duration > 0f ? d.duration : 4f;

        foreach (TowerStats ts in targets)
        {
            ts.RemoveModifiers(r);
            ts.AddModifier(r, d.buffStat, d.buffAmount);

            if (!r.Buffed.Contains(ts)) r.Buffed.Add(ts);
            r.BuffExpiry[ts] = Time.time + duration;
        }

        AbilityVfx.Pulse(transform.position, Mathf.Max(1f, d.radius * 0.5f), new Color(1f, 0.9f, 0.2f));
        return true;
    }

    // Passive buff: an aura that follows towers entering and leaving range.
    private void RefreshAura(AbilityRuntime r)
    {
        AbilityData d = r.Data;
        List<TowerStats> wanted = BuffTargets(d);

        for (int i = r.Buffed.Count - 1; i >= 0; i--)
        {
            TowerStats ts = r.Buffed[i];
            if (ts == null)
            {
                r.Buffed.RemoveAt(i);
            }
            else if (!wanted.Contains(ts))
            {
                ts.RemoveModifiers(r);
                r.Buffed.RemoveAt(i);
            }
        }

        foreach (TowerStats ts in wanted)
        {
            if (!r.Buffed.Contains(ts))
            {
                ts.AddModifier(r, d.buffStat, d.buffAmount);
                r.Buffed.Add(ts);
            }
        }
    }
}