using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float fallbackMaximumHealth = 100f;

    public float CurrentHealth { get; private set; }
    public float MaximumHealth { get; private set; }
    public bool IsDead { get; private set; }

    public event Action<float, float> HealthChanged;
    public event Action Died;

    private void Awake()
    {
        MaximumHealth = fallbackMaximumHealth;
        CurrentHealth = MaximumHealth;
    }

    public void SetMaximumHealth(float newMaximumHealth, bool refillHealth = true)
    {
        MaximumHealth = Mathf.Max(1f, newMaximumHealth);

        if (refillHealth)
        {
            CurrentHealth = MaximumHealth;
        }
        else
        {
            CurrentHealth = Mathf.Min(CurrentHealth, MaximumHealth);
        }

        HealthChanged?.Invoke(CurrentHealth, MaximumHealth);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        CurrentHealth -= amount;
        CurrentHealth = Mathf.Max(CurrentHealth, 0f);
        HealthChanged?.Invoke(CurrentHealth, MaximumHealth);

        Debug.Log($"{name} took {amount} damage");

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        CurrentHealth += amount;
        CurrentHealth = Mathf.Min(CurrentHealth, MaximumHealth);
        HealthChanged?.Invoke(CurrentHealth, MaximumHealth);

        Debug.Log($"{name} healed for {amount}");
    }

    private void Die()
    {
        if (IsDead)
            return;

        IsDead = true;
        Debug.Log($"{name} died");
        Died?.Invoke();
        Destroy(gameObject);
    }
}
