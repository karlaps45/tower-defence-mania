using System;
using UnityEngine;

public class Shield : MonoBehaviour, IDamageAbsorber
{
    private float maxShield;
    private float decayPerSecond;
    private float decayPerHitPercent;

    public float Current { get; private set; }
    public event Action<float, float> ShieldChanged;   // current, max

    private void Awake()
    {
        Health health = GetComponent<Health>();
        if (health != null) health.SetAbsorber(this);
    }

    // A weaker shield never overwrites a stronger one.
    public void Apply(float amount, float decayPerSecond, float decayPerHitPercent)
    {
        if (amount <= Current)
            return;

        maxShield = amount;
        Current = amount;
        this.decayPerSecond = decayPerSecond;
        this.decayPerHitPercent = decayPerHitPercent;
        ShieldChanged?.Invoke(Current, maxShield);
    }

    public float Absorb(float incoming)
    {
        if (Current <= 0f)
            return incoming;

        float absorbed = Mathf.Min(Current, incoming);
        Current -= absorbed;

        // Every hit also wears the shield down by a % of its max.
        Current = Mathf.Max(0f, Current - maxShield * decayPerHitPercent / 100f);

        ShieldChanged?.Invoke(Current, maxShield);
        return incoming - absorbed;
    }

    private void Update()
    {
        if (Current <= 0f || decayPerSecond <= 0f)
            return;

        Current = Mathf.Max(0f, Current - decayPerSecond * Time.deltaTime);
        ShieldChanged?.Invoke(Current, maxShield);
    }
}