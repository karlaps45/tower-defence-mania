using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Health))]
public class TowerHealthDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Slider healthBar;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void Start()
    {
        health.HealthChanged += UpdateDisplay;
        UpdateDisplay(health.CurrentHealth, health.MaximumHealth);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.HealthChanged -= UpdateDisplay;
        }
    }

    private void UpdateDisplay(float currentHealth, float maximumHealth)
    {
        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(currentHealth)} / " +
                Mathf.CeilToInt(maximumHealth);
        }

        if (healthBar != null)
        {
            healthBar.minValue = 0f;
            healthBar.maxValue = maximumHealth;
            healthBar.value = currentHealth;
        }
    }
}
