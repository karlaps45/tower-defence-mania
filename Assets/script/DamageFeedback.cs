using TMPro;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class DamageFeedback : MonoBehaviour
{
    [SerializeField] private float heightOffset = 0.6f;
    [SerializeField] private float baseFontSize = 4f;

    private static readonly Color Tier1 = new Color(1f, 0.92f, 0.1f);       // yellow
    private static readonly Color Tier2 = new Color(1f, 0.15f, 0.15f);      // red
    private static readonly Color Tier3 = new Color(0.2f, 0.3f, 1f);        // blue
    private static readonly Color PoisonColor = new Color(0.2f, 0.9f, 0.2f);
    private static readonly Color FireColor = new Color(1f, 0.5f, 0f);
    private static readonly Color HealColor = new Color(0.3f, 1f, 0.4f);

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.Damaged += OnDamaged;
        health.Healed += OnHealed;
    }

    private void OnDisable()
    {
        if (health == null) return;
        health.Damaged -= OnDamaged;
        health.Healed -= OnHealed;
    }

    private void OnDamaged(DamageInfo info)
    {
        if (info.Amount <= 0.5f) return;

        string text = Mathf.RoundToInt(info.Amount).ToString();
        Color color = Color.white;
        float size = baseFontSize;

        if (info.CritTier > 0)
        {
            text += new string('!', info.CritTier);
            color = info.CritTier == 1 ? Tier1 : info.CritTier == 2 ? Tier2 : Tier3;
            size = baseFontSize + info.CritTier * 1.5f;
        }
        else if (info.Type == DamageType.Poison)
        {
            color = PoisonColor;
            size = baseFontSize * 0.6f;
        }
        else if (info.Type == DamageType.Fire)
        {
            color = FireColor;
            size = baseFontSize * 0.6f;
        }

        Spawn(text, color, size);
    }

    private void OnHealed(float amount)
    {
        if (amount < 0.5f) return;
        Spawn("+" + Mathf.RoundToInt(amount), HealColor, baseFontSize * 0.8f);
    }

    private void Spawn(string text, Color color, float size)
    {
        Vector3 position = transform.position
            + Vector3.up * heightOffset
            + Vector3.right * Random.Range(-0.3f, 0.3f);
        FloatingText.Spawn(position, text, color, size);
    }
}

public class FloatingText : MonoBehaviour
{
    private TextMeshPro text;
    private float lifetime;
    private float age;

    public static void Spawn(Vector3 position, string message, Color color, float fontSize, float lifetime = 0.9f)
    {
        GameObject go = new GameObject("FloatingText");
        go.transform.position = position;

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = message;
        tmp.color = color;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = 100;

        FloatingText floating = go.AddComponent<FloatingText>();
        floating.text = tmp;
        floating.lifetime = lifetime;
    }

    private void Update()
    {
        age += Time.deltaTime;
        transform.position += Vector3.up * 1.2f * Time.deltaTime;

        Color c = text.color;
        c.a = 1f - age / lifetime;
        text.color = c;

        if (age >= lifetime) Destroy(gameObject);
    }
}