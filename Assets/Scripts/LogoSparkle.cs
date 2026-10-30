using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LogoSparkleUI : MonoBehaviour
{
    [Header("Where the sparkles appear")]
    public bool useRectSize = true;                           // take the size from the logo's RectTransform
    public Vector2 area = new Vector2(600f, 200f);            // used if the box above is off (UI pixels)

    [Header("Sparkles")]
    public float perSecond = 12f;
    public float sparkSize = 24f;                             // UI pixels
    public Vector2 lifeRange = new Vector2(0.7f, 1.5f);
    public Vector2 riseRange = new Vector2(20f, 70f);         // pixels per second, drifts upward
    public float driftAmount = 25f;
    public Color[] colors =
    {
        new Color(1f, 0.92f, 0.6f),
        new Color(1f, 0.85f, 0.4f),
        new Color(0.7f, 0.9f, 1f),
        Color.white
    };

    [Header("Glow")]
    public Color glowColor = new Color(1f, 0.8f, 0.4f, 0.22f);
    public Vector2 glowMargin = new Vector2(1.2f, 1.6f);

    class Spark { public RectTransform rt; public Image img; public float age, life, size, rise, drift, phase; public Color color; }

    readonly List<Spark> sparks = new List<Spark>();
    Sprite star;
    RectTransform logoRt, glowRt, container;
    Image glowImg;
    float seed, carry;

    void Start()
    {
        seed = Random.Range(0f, 100f);
        logoRt = (RectTransform)transform;
        if (useRectSize) area = logoRt.rect.size;

        star = MakeStar();

        // Glow: a sibling placed BEHIND the logo
        var g = new GameObject("LogoGlow", typeof(RectTransform), typeof(Image));
        glowRt = (RectTransform)g.transform;
        glowRt.SetParent(logoRt.parent, false);
        glowRt.SetSiblingIndex(logoRt.GetSiblingIndex());     // takes the logo's slot, pushing the logo in front
        glowImg = g.GetComponent<Image>();
        glowImg.sprite = MakeGlow();
        glowImg.raycastTarget = false;

        // Sparkles: children of the logo, so they draw IN FRONT and follow it around
        var c = new GameObject("LogoSparkles", typeof(RectTransform));
        container = (RectTransform)c.transform;
        container.SetParent(logoRt, false);
        container.anchorMin = container.anchorMax = new Vector2(0.5f, 0.5f);
        container.anchoredPosition = Vector2.zero;
        container.sizeDelta = Vector2.zero;
    }

    void OnDestroy()
    {
        if (glowRt != null) Destroy(glowRt.gameObject);   // sparkles are children of the logo and go with it
    }

    Sprite MakeGlow()
    {
        int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f, n / 2f)) / (n / 2f);
            float a = Mathf.Clamp01(1f - d);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    Sprite MakeStar()   // a 5x5 pixel plus sign
    {
        var tex = new Texture2D(5, 5, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 5; y++)
        for (int x = 0; x < 5; x++)
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0f));
        for (int i = 0; i < 5; i++)
        {
            tex.SetPixel(i, 2, Color.white);
            tex.SetPixel(2, i, Color.white);
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 5, 5), new Vector2(0.5f, 0.5f), 100f);
    }

    void Spawn()
    {
        var go = new GameObject("Sparkle", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(container, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(
            Random.Range(-area.x / 2f, area.x / 2f),
            Random.Range(-area.y / 2f, area.y / 2f));

        var img = go.GetComponent<Image>();
        img.sprite = star;
        img.raycastTarget = false;

        sparks.Add(new Spark
        {
            rt = rt,
            img = img,
            life = Random.Range(lifeRange.x, lifeRange.y),
            size = sparkSize * Random.Range(0.6f, 1.4f),
            rise = Random.Range(riseRange.x, riseRange.y),
            drift = Random.Range(-1f, 1f) * driftAmount,
            phase = Random.Range(0f, 6.28f),
            color = colors[Random.Range(0, colors.Length)]
        });
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float t = Time.unscaledTime;
        float n1 = Mathf.PerlinNoise(t * 3f, seed);

        // glow follows the logo and breathes
        glowRt.position = logoRt.TransformPoint(logoRt.rect.center);
        glowRt.sizeDelta = Vector2.Scale(area, glowMargin) * (1f + (n1 - 0.5f) * 0.12f);
        Color gc = glowColor;
        gc.a *= 0.8f + n1 * 0.4f;
        glowImg.color = gc;

        carry += perSecond * dt;
        while (carry >= 1f) { carry -= 1f; Spawn(); }

        for (int i = sparks.Count - 1; i >= 0; i--)
        {
            Spark s = sparks[i];
            s.age += dt;
            if (s.age >= s.life)
            {
                Destroy(s.rt.gameObject);
                sparks.RemoveAt(i);
                continue;
            }

            float k = s.age / s.life;
            float fade = Mathf.Sin(k * Mathf.PI);
            float twinkle = 0.65f + 0.35f * Mathf.Sin(t * 14f + s.phase);

            Vector2 p = s.rt.anchoredPosition;
            p.y += s.rise * dt;
            p.x += s.drift * dt * Mathf.Sin(t * 3f + s.phase);
            s.rt.anchoredPosition = p;
            s.rt.sizeDelta = Vector2.one * s.size * (0.6f + 0.4f * fade);

            Color c = s.color;
            c.a = fade * twinkle;
            s.img.color = c;
        }
    }
}