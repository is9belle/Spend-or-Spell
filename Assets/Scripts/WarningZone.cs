using System;
using UnityEngine;

public class WarningZone : MonoBehaviour
{
    static Sprite disc, ring;
    static readonly Color red = new Color(0.95f, 0.15f, 0.12f);

    float radius, duration, t;
    Action onDone;
    SpriteRenderer fillSr, ringSr;

    static void EnsureSprites()
    {
        if (disc != null && ring != null) return;
        int n = 256;
        var d = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var r = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float dx = (x + 0.5f - n / 2f) / (n / 2f), dy = (y + 0.5f - n / 2f) / (n / 2f);
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            float edge = Mathf.Clamp01((1f - dist) * 40f);
            d.SetPixel(x, y, new Color(1f, 1f, 1f, edge));
            r.SetPixel(x, y, new Color(1f, 1f, 1f, edge * Mathf.Clamp01((dist - 0.9f) * 40f)));
        }
        d.Apply(); r.Apply();
        disc = Sprite.Create(d, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);   // 1 unit across
        ring = Sprite.Create(r, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
    }

    static SpriteRenderer Layer(Transform parent, Sprite s, float size, Color c, int order)
    {
        var g = new GameObject("Layer");
        g.transform.SetParent(parent, false);
        g.transform.localScale = new Vector3(size, size, 1f);
        var sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = s; sr.color = c; sr.sortingOrder = order;
        return sr;
    }

    // a red circle that fills up over 'duration', then calls onDone and disappears
    public static WarningZone Spawn(Vector3 pos, float radius, float duration, Action onDone = null)
    {
        EnsureSprites();
        var go = new GameObject("WarningZone");
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        var z = go.AddComponent<WarningZone>();
        z.radius = radius;
        z.duration = Mathf.Max(0.05f, duration);
        z.onDone = onDone;
        float size = radius * 2f;
        Layer(go.transform, disc, size, new Color(red.r, red.g, red.b, 0.16f), -4);
        z.fillSr = Layer(go.transform, disc, 0.01f, new Color(red.r, red.g, red.b, 0.4f), -3);
        z.ringSr = Layer(go.transform, ring, size, new Color(1f, 0.3f, 0.25f, 0.9f), -2);
        return z;
    }

    // an expanding ring for impacts and splits
    public static void Burst(Vector3 pos, float radius, float time = 0.4f)
    {
        EnsureSprites();
        var go = new GameObject("Burst");
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        var b = go.AddComponent<RingBurst>();
        b.radius = radius; b.time = Mathf.Max(0.05f, time);
        b.sr = go.AddComponent<SpriteRenderer>();
        b.sr.sprite = ring;
        b.sr.sortingOrder = -1;
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / duration);
        float size = radius * 2f * k;
        fillSr.transform.localScale = new Vector3(size, size, 1f);
        float pulse = 0.5f + 0.5f * Mathf.Sin(t * 16f);
        fillSr.color = new Color(red.r, red.g, red.b, 0.28f + 0.22f * pulse * k + 0.2f * k);
        ringSr.color = new Color(1f, 0.3f, 0.25f, 0.65f + 0.35f * pulse);
        if (k >= 1f)
        {
            if (onDone != null) onDone();
            Destroy(gameObject);
        }
    }
}

public class RingBurst : MonoBehaviour
{
    public float radius, time;
    public SpriteRenderer sr;
    float t;

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / time);
        float size = radius * 2f * Mathf.Lerp(0.5f, 1.15f, 1f - (1f - k) * (1f - k));
        transform.localScale = new Vector3(size, size, 1f);
        sr.color = new Color(1f, 0.5f, 0.35f, 1f - k);
        if (k >= 1f) Destroy(gameObject);
    }
}