using System;
using UnityEngine;

public interface IDamageAbsorber
{
    // Receives incoming damage, returns what is left after absorbing.
    float Absorb(float amount);
}

public class Health : MonoBehaviour
{
    [SerializeField] private float fallbackMaximumHealth = 100f;

    private IDamageAbsorber absorber;
    public DamageInfo LastDamage { get; private set; }
    public static event Action<Health, DamageInfo> AnyKilled;
    public event Action<float> Healed;
    public float CurrentHealth { get; private set; }
    public float MaximumHealth { get; private set; }
    public bool IsDead { get; private set; }

    public event Action<float, float> HealthChanged;
    public event Action Died;
    public event Action<DamageInfo> Damaged;   // amount is after shield absorption

    private void Awake()
    {
        MaximumHealth = fallbackMaximumHealth;
        CurrentHealth = MaximumHealth;
        absorber = GetComponent<IDamageAbsorber>();
    }



    public void SetMaximumHealth(float newMaximumHealth, bool refillHealth = true)
    {
        MaximumHealth = Mathf.Max(1f, newMaximumHealth);
        CurrentHealth = refillHealth
            ? MaximumHealth
            : Mathf.Min(CurrentHealth, MaximumHealth);
        HealthChanged?.Invoke(CurrentHealth, MaximumHealth);
    }

    // Old callers keep working.
    public void TakeDamage(float amount)
    {
        TakeDamage(new DamageInfo { Amount = amount, Type = DamageType.Physical });
    }

    public void TakeDamage(DamageInfo info)
    {
        if (IsDead || info.Amount <= 0f)
            return;

        if (absorber != null)
        {
            info.Amount = absorber.Absorb(info.Amount);
        }

        if (info.Amount > 0f)
        {
            CurrentHealth = Mathf.Max(CurrentHealth - info.Amount, 0f);
            HealthChanged?.Invoke(CurrentHealth, MaximumHealth);
        }

        Damaged?.Invoke(info);   // fires even if the shield absorbed everything

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        float before = CurrentHealth;
        CurrentHealth = Mathf.Min(CurrentHealth + amount, MaximumHealth);
        HealthChanged?.Invoke(CurrentHealth, MaximumHealth);

        float healed = CurrentHealth - before;
        if (healed > 0f) Healed?.Invoke(healed);
    }

    private void Die()
    {
        if (IsDead)
            return;
        AnyKilled?.Invoke(this, LastDamage);
        IsDead = true;
        Died?.Invoke();
        Destroy(gameObject);
    }


    public void SetAbsorber(IDamageAbsorber newAbsorber)
    {
        absorber = newAbsorber;
    }
}