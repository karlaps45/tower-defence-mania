using UnityEngine;

public static class AbilityVfx
{
    private static Material lineMaterial;

    public static void Pulse(Vector2 position, float radius, Color color, float duration = 0.35f)
    {
        if (lineMaterial == null)
        {
            lineMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        GameObject go = new GameObject("AbilityPulse");
        go.transform.position = position;

        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.widthMultiplier = 0.08f;
        line.material = lineMaterial;
        line.sortingOrder = 50;

        go.AddComponent<PulseAnimator>().Init(line, radius, color, duration);
    }
}

public class PulseAnimator : MonoBehaviour
{
    private const int Segments = 40;

    private LineRenderer line;
    private float maxRadius;
    private float duration;
    private Color color;
    private float age;

    public void Init(LineRenderer lineRenderer, float radius, Color pulseColor, float time)
    {
        line = lineRenderer;
        maxRadius = radius;
        color = pulseColor;
        duration = Mathf.Max(0.05f, time);
        line.positionCount = Segments;
    }

    private void Update()
    {
        age += Time.deltaTime;
        float t = Mathf.Clamp01(age / duration);
        float radius = Mathf.Lerp(maxRadius * 0.2f, maxRadius, 1f - (1f - t) * (1f - t));

        for (int i = 0; i < Segments; i++)
        {
            float angle = i * Mathf.PI * 2f / Segments;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }

        Color faded = color;
        faded.a = 1f - t;
        line.startColor = faded;
        line.endColor = faded;

        if (t >= 1f) Destroy(gameObject);
    }
}